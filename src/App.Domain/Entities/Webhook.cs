using App.Domain.ValueObjects;

namespace App.Domain.Entities;

/// <summary>
/// Represents a webhook configuration that fires HTTP requests on ticket events.
/// </summary>
public class Webhook : BaseAuditableEntity
{
    /// <summary>
    /// Display name for the webhook (e.g., "Slack Integration", "CRM Sync").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Target URL to POST webhook payloads to.
    /// </summary>
    public string Url { get; set; } = null!;

    /// <summary>
    /// The event trigger type (stored as WebhookTriggerType.DeveloperName).
    /// </summary>
    public string TriggerType { get; set; } = null!;

    /// <summary>
    /// Whether this webhook is currently active and will fire on events.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional description of what this webhook does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property for webhook delivery logs.
    /// </summary>
    public virtual ICollection<WebhookLog> Logs { get; set; } = new List<WebhookLog>();

    /// <summary>
    /// Gets the trigger type as a value object.
    /// </summary>
    public WebhookTriggerType TriggerTypeValue => WebhookTriggerType.From(TriggerType);

    public override string ToString() => Name;
}
