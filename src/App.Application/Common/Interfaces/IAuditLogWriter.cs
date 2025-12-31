using App.Application.Common.Models;

namespace App.Application.Common.Interfaces;

/// <summary>
/// Interface for writing audit log entries to various destinations.
/// Implementations can write to PostgreSQL, Loki, OpenTelemetry, etc.
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>
    /// The mode this writer operates in. Used by behaviors to determine
    /// whether to send query (read) entries to this writer.
    /// </summary>
    AuditLogMode Mode { get; }

    /// <summary>
    /// Writes an audit log entry to this sink.
    /// </summary>
    /// <param name="entry">The audit log entry to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a single audit log entry with all metadata for structured logging.
/// </summary>
public record AuditLogEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Category/name of the request (e.g., "Tickets.Commands.CreateTicket").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Type of request: "Command" or "Query".
    /// </summary>
    public string RequestType { get; init; } = string.Empty;

    /// <summary>
    /// Serialized JSON of the request payload.
    /// </summary>
    public string RequestPayload { get; init; } = string.Empty;

    /// <summary>
    /// Optional serialized JSON of the response payload.
    /// Only populated for queries with LogQueryResult enabled.
    /// </summary>
    public string? ResponsePayload { get; init; }

    /// <summary>
    /// Whether the request completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Duration of the request in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Email address of the user who made the request.
    /// </summary>
    public string? UserEmail { get; init; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Optional entity ID for entity-specific requests.
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Timestamp when the request was made.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

