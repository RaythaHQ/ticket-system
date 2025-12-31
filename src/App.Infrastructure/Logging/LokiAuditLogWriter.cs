using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Infrastructure.Logging;

/// <summary>
/// Writes audit log entries directly to Loki/VictoriaLogs via HTTP.
/// Independent of Serilog - uses its own connection from Loki config.
/// Non-blocking with bounded channel to prevent memory blowup.
/// </summary>
public class LokiAuditLogWriter : IAuditLogWriter, IHostedService, IDisposable
{
    private readonly Channel<AuditLogEntry> _channel;
    private readonly ILogger<LokiAuditLogWriter> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _lokiUrl;
    private readonly string _serviceName;
    private readonly CancellationTokenSource _cts;
    private Task? _processingTask;

    public AuditLogMode Mode { get; }

    public LokiAuditLogWriter(
        IOptions<ObservabilityOptions> options,
        ILogger<LokiAuditLogWriter> logger,
        IHttpClientFactory httpClientFactory
    )
    {
        _logger = logger;
        _cts = new CancellationTokenSource();

        var lokiOptions = options.Value.Loki;
        var auditSinkOptions = options.Value.AuditLog.AdditionalSinks.Loki;

        Mode = auditSinkOptions?.Mode ?? AuditLogMode.All;
        _serviceName = options.Value.OpenTelemetry.ServiceName;

        // Build Loki URL - append the Loki API path
        _lokiUrl = $"{lokiOptions.Url?.TrimEnd('/')}/loki/api/v1/push";

        // Create HTTP client with auth
        _httpClient = httpClientFactory.CreateClient("LokiAuditLog");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);

        if (!string.IsNullOrEmpty(lokiOptions.Username) && !string.IsNullOrEmpty(lokiOptions.Password))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{lokiOptions.Username}:{lokiOptions.Password}")
            );
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        // Bounded channel with 10,000 entry capacity
        // DropOldest policy prevents memory blowup if Loki is unavailable
        _channel = Channel.CreateBounded<AuditLogEntry>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            }
        );
    }

    /// <summary>
    /// Non-blocking write - returns immediately after queuing.
    /// </summary>
    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        // TryWrite is non-blocking; if channel is full, oldest entries are dropped
        if (!_channel.Writer.TryWrite(entry))
        {
            _logger.LogWarning(
                "Loki audit log channel is full, entry dropped: {Category}",
                entry.Category
            );
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _processingTask = ProcessEntriesAsync(_cts.Token);
        _logger.LogInformation(
            "Loki audit log writer started with Mode={Mode}, Url={Url}",
            Mode,
            _lokiUrl
        );
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.Complete();
        _cts.Cancel();

        if (_processingTask != null)
        {
            try
            {
                await _processingTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        _logger.LogInformation("Loki audit log writer stopped");
    }

    private async Task ProcessEntriesAsync(CancellationToken cancellationToken)
    {
        var batch = new List<AuditLogEntry>();
        var batchTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Try to read entries, batch them up
                while (batch.Count < 100 && _channel.Reader.TryRead(out var entry))
                {
                    batch.Add(entry);
                }

                // If we have entries, send them
                if (batch.Count > 0)
                {
                    await SendBatchAsync(batch, cancellationToken);
                    batch.Clear();
                }

                // Wait for more entries or timeout
                try
                {
                    if (await _channel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        continue;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            // Drain remaining entries on shutdown
            while (_channel.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
            }

            if (batch.Count > 0)
            {
                await SendBatchAsync(batch, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loki audit log processing failed");
        }
    }

    private async Task SendBatchAsync(List<AuditLogEntry> entries, CancellationToken cancellationToken)
    {
        try
        {
            var payload = BuildLokiPayload(entries);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_lokiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to send audit logs to Loki: {StatusCode} - {Body}",
                    response.StatusCode,
                    body
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send audit batch to Loki ({Count} entries)", entries.Count);
        }
    }

    private string BuildLokiPayload(List<AuditLogEntry> entries)
    {
        var values = entries
            .Select(e => new object[]
            {
                // Loki expects nanosecond timestamp as string
                (e.Timestamp.ToUniversalTime().Ticks - 621355968000000000L) * 100 + "",
                JsonSerializer.Serialize(new
                {
                    type = "AuditLog",
                    id = e.Id,
                    category = e.Category,
                    requestType = e.RequestType,
                    request = e.RequestPayload,
                    response = e.ResponsePayload,
                    success = e.Success,
                    durationMs = e.DurationMs,
                    user = e.UserEmail,
                    ip = e.IpAddress,
                    entityId = e.EntityId,
                    timestamp = e.Timestamp,
                }),
            })
            .ToArray();

        var payload = new
        {
            streams = new[]
            {
                new
                {
                    stream = new Dictionary<string, string>
                    {
                        ["app"] = _serviceName,
                        ["type"] = "audit",
                    },
                    values,
                },
            },
        };

        return JsonSerializer.Serialize(payload);
    }

    public void Dispose()
    {
        _cts.Dispose();
        _httpClient.Dispose();
    }
}
