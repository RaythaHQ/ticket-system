namespace App.Domain.Entities;

/// <summary>
/// Represents a log entry for a webhook delivery attempt.
/// </summary>
public class WebhookLog : BaseEntity
{
    /// <summary>
    /// The webhook this log entry belongs to.
    /// </summary>
    public Guid WebhookId { get; set; }
    public virtual Webhook Webhook { get; set; } = null!;

    /// <summary>
    /// Reference to the ticket that triggered this webhook.
    /// </summary>
    public long? TicketId { get; set; }

    /// <summary>
    /// The trigger type at the time of delivery (for historical reference).
    /// </summary>
    public string TriggerType { get; set; } = null!;

    /// <summary>
    /// The JSON payload that was sent (or attempted to send).
    /// </summary>
    public string PayloadJson { get; set; } = null!;

    /// <summary>
    /// Number of delivery attempts made (1-3 for automatic retries).
    /// </summary>
    public int AttemptCount { get; set; } = 1;

    /// <summary>
    /// Whether the webhook was successfully delivered.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP status code received from the target URL.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Error message if the delivery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// First 1KB of the response body (for debugging).
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// When this log entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the delivery completed (success or final failure).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total duration of all delivery attempts.
    /// </summary>
    public TimeSpan? Duration { get; set; }
}
