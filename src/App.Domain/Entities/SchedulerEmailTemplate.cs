namespace App.Domain.Entities;

/// <summary>
/// Scheduler-owned email/SMS template. Separate from the system-wide EmailTemplate entity.
/// Supports scheduler-specific merge variables including meeting links.
/// </summary>
public class SchedulerEmailTemplate : BaseAuditableEntity
{
    /// <summary>
    /// Template type: "confirmation", "reminder", or "post_meeting".
    /// </summary>
    public string TemplateType { get; set; } = null!;

    /// <summary>
    /// Notification channel: "email" or "sms".
    /// </summary>
    public string Channel { get; set; } = null!;

    /// <summary>
    /// Email subject line (Liquid template). Null for SMS channel.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Body content (Liquid template). Supports scheduler-specific merge variables.
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Whether this template is active. Email templates are active in v1; SMS templates are inactive.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Well-known template types
    public const string TYPE_CONFIRMATION = "confirmation";
    public const string TYPE_REMINDER = "reminder";
    public const string TYPE_POST_MEETING = "post_meeting";

    // Well-known channels
    public const string CHANNEL_EMAIL = "email";
    public const string CHANNEL_SMS = "sms";
}
