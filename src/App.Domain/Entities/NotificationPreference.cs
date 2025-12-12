using App.Domain.ValueObjects;

namespace App.Domain.Entities;

/// <summary>
/// User-level settings for notification delivery.
/// </summary>
public class NotificationPreference : BaseAuditableEntity
{
    public Guid StaffAdminId { get; set; }
    public virtual User StaffAdmin { get; set; } = null!;

    /// <summary>
    /// The type of notification event this preference applies to.
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// When true, email notifications are enabled for this event.
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// When true, webhook notifications are enabled for this event.
    /// </summary>
    public bool WebhookEnabled { get; set; }

    /// <summary>
    /// Optional webhook URL for this specific event type.
    /// </summary>
    public string? WebhookUrl { get; set; }
}

