namespace App.Application.Common.Models;

/// <summary>
/// Configuration options for the observability stack including logging,
/// OpenTelemetry, Loki, Sentry, and audit log destinations.
/// </summary>
public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>
    /// Controls which logging sinks are enabled.
    /// </summary>
    public LoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// OpenTelemetry connection details (OTLP endpoint, auth).
    /// Used when Logging.EnableOpenTelemetry is true.
    /// </summary>
    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();

    /// <summary>
    /// Loki/VictoriaLogs connection details.
    /// Used when Logging.EnableLoki is true.
    /// </summary>
    public LokiOptions Loki { get; set; } = new();

    /// <summary>
    /// Sentry error tracking connection details.
    /// Enabled when Dsn is set.
    /// </summary>
    public SentryOptions Sentry { get; set; } = new();

    /// <summary>
    /// Audit log configuration.
    /// </summary>
    public AuditLogOptions AuditLog { get; set; } = new();
}

/// <summary>
/// Controls which logging sinks are enabled.
/// </summary>
public class LoggingOptions
{
    public bool EnableConsole { get; set; } = true;
    public bool EnableOpenTelemetry { get; set; } = false;
    public bool EnableLoki { get; set; } = false;
}

/// <summary>
/// OpenTelemetry connection details for traces, metrics, and logs.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// OTLP endpoint URL (e.g., https://openobserve.example.com/api/default).
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Service name for resource attribution.
    /// </summary>
    public string ServiceName { get; set; } = "app";

    /// <summary>
    /// Authorization header value for OTLP endpoint.
    /// For OpenObserve: "Basic {base64(email:password)}"
    /// For other backends: "Bearer {token}" or leave empty.
    /// </summary>
    public string? Authorization { get; set; }

    /// <summary>
    /// Stream name for OpenObserve.
    /// </summary>
    public string StreamName { get; set; } = "default";

    /// <summary>
    /// Organization name for OpenObserve.
    /// </summary>
    public string Organization { get; set; } = "default";

    /// <summary>
    /// Use gRPC protocol instead of HTTP/protobuf.
    /// </summary>
    public bool UseGrpc { get; set; } = false;
}

/// <summary>
/// Loki/VictoriaLogs connection details.
/// </summary>
public class LokiOptions
{
    /// <summary>
    /// Loki push endpoint URL.
    /// For VictoriaLogs: http://host:9428/insert/loki/api/v1/push
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Optional username for basic auth.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional password for basic auth.
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Sentry error tracking connection details.
/// Enabled automatically when Dsn is set.
/// </summary>
public class SentryOptions
{
    /// <summary>
    /// Sentry DSN (Data Source Name).
    /// </summary>
    public string? Dsn { get; set; }

    /// <summary>
    /// Environment tag for filtering errors.
    /// </summary>
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
/// Uses the main Serilog Loki sink (configured in Logging section).
/// </summary>
public class LokiAuditSinkOptions
{
    /// <summary>
    /// Enable writing audit logs to Loki via Serilog.
    /// Requires Logging.EnableLoki to also be true.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Which operations to log: WritesOnly (commands) or All (commands + queries).
    /// </summary>
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
