using System.Threading.Channels;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Infrastructure.Logging;

/// <summary>
/// Writes audit log entries to Loki via Serilog.
/// Non-blocking with bounded channel to prevent memory blowup.
/// </summary>
public class LokiAuditLogWriter : IAuditLogWriter, IHostedService, IDisposable
{
    private readonly Channel<AuditLogEntry> _channel;
    private readonly ILogger<LokiAuditLogWriter> _logger;
    private readonly CancellationTokenSource _cts;
    private Task? _processingTask;

    public AuditLogMode Mode { get; }

    public LokiAuditLogWriter(
        IOptions<ObservabilityOptions> options,
        ILogger<LokiAuditLogWriter> logger)
    {
        _logger = logger;
        _cts = new CancellationTokenSource();

        Mode = options.Value.AuditLog.AdditionalSinks.Loki?.Mode ?? AuditLogMode.All;

        // Bounded channel with 10,000 entry capacity
        // DropOldest policy prevents memory blowup if Loki is unavailable
        _channel = Channel.CreateBounded<AuditLogEntry>(new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Non-blocking write - returns immediately after queuing.
    /// </summary>
    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        // TryWrite is non-blocking; if channel is full, oldest entries are dropped
        if (!_channel.Writer.TryWrite(entry))
        {
            _logger.LogWarning("Loki audit log channel is full, entry dropped: {Category}", entry.Category);
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _processingTask = ProcessEntriesAsync(_cts.Token);
        _logger.LogInformation("Loki audit log writer started with Mode={Mode}", Mode);
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
        try
        {
            await foreach (var entry in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // Write to Serilog with structured data
                    // Serilog's Loki sink will pick this up and send to Loki
                    _logger.LogInformation(
                        "AuditLog {@AuditEntry}",
                        new
                        {
                            entry.Id,
                            entry.Category,
                            entry.RequestType,
                            entry.RequestPayload,
                            entry.ResponsePayload,
                            entry.Success,
                            entry.DurationMs,
                            entry.UserEmail,
                            entry.IpAddress,
                            entry.EntityId,
                            entry.Timestamp
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write audit entry to Loki: {Category}", entry.Category);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loki audit log processing failed");
        }
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}

