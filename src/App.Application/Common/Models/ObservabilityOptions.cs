namespace App.Application.Common.Models;

/// <summary>
/// Configuration options for the observability stack including logging,
/// OpenTelemetry, Sentry, and audit log destinations.
/// </summary>
public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public LoggingOptions Logging { get; set; } = new();
    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();
    public SentryConfigOptions Sentry { get; set; } = new();
    public AuditLogOptions AuditLog { get; set; } = new();
}

/// <summary>
/// Configuration for logging sinks (console, Loki, OpenTelemetry).
/// </summary>
public class LoggingOptions
{
    public bool EnableConsole { get; set; } = true;
    public bool EnableOpenTelemetry { get; set; } = false;
    public bool EnableLoki { get; set; } = false;
    public string? LokiUrl { get; set; }
}

/// <summary>
/// Configuration for OpenTelemetry tracing and metrics.
/// </summary>
public class OpenTelemetryOptions
{
    public bool Enabled { get; set; } = false;
    public string? OtlpEndpoint { get; set; }
    public string ServiceName { get; set; } = "app";

    /// <summary>
    /// Optional authorization header value for OTLP endpoint.
    /// For OpenObserve: "Basic {base64(email:password)}"
    /// For other backends: "Bearer {token}" or leave empty if no auth required.
    /// </summary>
    public string? Authorization { get; set; }

    /// <summary>
    /// Optional stream name for OpenObserve.
    /// Default is "default" if not specified.
    /// </summary>
    public string StreamName { get; set; } = "default";

    /// <summary>
    /// Optional organization name for OpenObserve.
    /// Default is "default" if not specified.
    /// </summary>
    public string Organization { get; set; } = "default";

    /// <summary>
    /// Use gRPC protocol instead of HTTP.
    /// Set to true for backends that prefer gRPC (e.g., OpenObserve on port 5081).
    /// </summary>
    public bool UseGrpc { get; set; } = false;
}

/// <summary>
/// Configuration for Sentry error tracking.
/// </summary>
public class SentryConfigOptions
{
    public string? Dsn { get; set; }
    public string Environment { get; set; } = "production";
}

/// <summary>
/// Configuration for audit logging destinations.
/// PostgreSQL is always enabled (source for in-app UI).
/// Additional sinks are optional with configurable modes.
/// </summary>
public class AuditLogOptions
{
    /// <summary>
    /// Additional audit log sinks beyond PostgreSQL.
    /// These support configurable modes (WritesOnly or All).
    /// </summary>
    public AdditionalAuditSinkOptions AdditionalSinks { get; set; } = new();
}

/// <summary>
/// Configuration for additional audit sinks (Loki, OpenTelemetry).
/// </summary>
public class AdditionalAuditSinkOptions
{
    public LokiAuditSinkOptions? Loki { get; set; }
    public OtelAuditSinkOptions? OpenTelemetry { get; set; }
}

/// <summary>
/// Configuration for Loki audit sink.
/// </summary>
public class LokiAuditSinkOptions
{
    public bool Enabled { get; set; } = false;
    public string? Url { get; set; }
    public AuditLogMode Mode { get; set; } = AuditLogMode.All;
}

/// <summary>
/// Configuration for OpenTelemetry audit sink.
/// </summary>
public class OtelAuditSinkOptions
{
    public bool Enabled { get; set; } = false;
    public AuditLogMode Mode { get; set; } = AuditLogMode.All;
}

/// <summary>
/// Determines which operations are logged by an audit sink.
/// </summary>
public enum AuditLogMode
{
    /// <summary>
    /// Only log commands (mutations/writes).
    /// </summary>
    WritesOnly,

    /// <summary>
    /// Log both commands and queries (reads + writes).
    /// Default for additional sinks for HIPAA compliance.
    /// </summary>
    All,
}
