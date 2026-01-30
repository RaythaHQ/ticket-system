namespace App.Domain.Entities;

public class OrganizationSettings : BaseEntity
{
    public string? OrganizationName { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public bool SmtpOverrideSystem { get; set; } = false;
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpDefaultFromAddress { get; set; }
    public string? SmtpDefaultFromName { get; set; }

    /// <summary>
    /// When true, SLA due times are paused while a ticket is snoozed.
    /// The SLA due time is extended by the snooze duration when the ticket is unsnoozed.
    /// </summary>
    public bool PauseSlaOnSnooze { get; set; } = false;
}
