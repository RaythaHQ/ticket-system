using System.Diagnostics;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Infrastructure.Logging;

/// <summary>
/// Writes audit log entries as OpenTelemetry log records.
/// Non-blocking - leverages OTEL SDK's internal batching and async export.
/// Includes trace context for correlation with distributed traces.
/// </summary>
public class OtelAuditLogWriter : IAuditLogWriter
{
    private readonly ILogger<OtelAuditLogWriter> _logger;

    public AuditLogMode Mode { get; }

    public OtelAuditLogWriter(
        IOptions<ObservabilityOptions> options,
        ILogger<OtelAuditLogWriter> logger)
    {
        _logger = logger;
        Mode = options.Value.AuditLog.AdditionalSinks.OpenTelemetry?.Mode ?? AuditLogMode.All;
    }

    /// <summary>
    /// Non-blocking write - OTEL SDK handles batching and async export.
    /// </summary>
    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        // Get current trace context for correlation
        var activity = Activity.Current;

        // Write as structured log - OTEL SDK will export this
        // The log will include trace_id and span_id automatically if there's an active trace
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["audit.id"] = entry.Id,
            ["audit.category"] = entry.Category,
            ["audit.request_type"] = entry.RequestType,
            ["audit.success"] = entry.Success,
            ["audit.duration_ms"] = entry.DurationMs,
            ["audit.user_email"] = entry.UserEmail,
            ["audit.ip_address"] = entry.IpAddress,
            ["audit.entity_id"] = entry.EntityId,
            ["audit.timestamp"] = entry.Timestamp,
            ["trace_id"] = activity?.TraceId.ToString(),
            ["span_id"] = activity?.SpanId.ToString()
        }))
        {
            if (entry.Success)
            {
                _logger.LogInformation(
                    "AuditLog: {Category} {RequestType} completed in {DurationMs}ms",
                    entry.Category,
                    entry.RequestType,
                    entry.DurationMs);
            }
            else
            {
                _logger.LogWarning(
                    "AuditLog: {Category} {RequestType} failed after {DurationMs}ms",
                    entry.Category,
                    entry.RequestType,
                    entry.DurationMs);
            }
        }

        return Task.CompletedTask;
    }
}

