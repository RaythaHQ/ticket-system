using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using App.Domain.ValueObjects;

namespace App.Domain.Entities;

/// <summary>
/// Core entity representing a work item or issue to be tracked and resolved.
/// Uses numeric (long) ID for human-readable ticket numbers.
/// </summary>
public class Ticket : BaseNumericFullAuditableEntity
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = TicketStatus.OPEN;
    public string Priority { get; set; } = TicketPriority.NORMAL;
    public string Language { get; set; } = TicketLanguage.ENGLISH;
    public string? Category { get; set; }

    // Relationships
    public Guid? OwningTeamId { get; set; }
    public virtual Team? OwningTeam { get; set; }

    public Guid? AssigneeId { get; set; }
    public virtual User? Assignee { get; set; }

    /// <summary>
    /// When the current assignee was assigned to this ticket.
    /// Used for calculating personal close time metrics.
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    public Guid? CreatedByStaffId { get; set; }
    public virtual User? CreatedByStaff { get; set; }

    public long? ContactId { get; set; }
    public virtual Contact? Contact { get; set; }

    // Tags stored as JSON array
    public string? TagsJson { get; set; }

    [NotMapped]
    public List<string> Tags
    {
        get =>
            string.IsNullOrEmpty(TagsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        set => TagsJson = JsonSerializer.Serialize(value);
    }

    // Timestamps
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// The staff member who closed this ticket (for leaderboard tracking).
    /// </summary>
    public Guid? ClosedByStaffId { get; set; }
    public virtual User? ClosedByStaff { get; set; }

    // SLA
    public Guid? SlaRuleId { get; set; }
    public virtual SlaRule? SlaRule { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public DateTime? SlaBreachedAt { get; set; }
    public string? SlaStatus { get; set; }

    /// <summary>
    /// Number of times the SLA due date has been extended for this ticket.
    /// Used for enforcing extension limits for non-privileged users.
    /// </summary>
    public int SlaExtensionCount { get; set; } = 0;

    // Snooze fields
    /// <summary>
    /// When the ticket should automatically unsnooze. Null if not snoozed.
    /// Stored as UTC.
    /// </summary>
    public DateTime? SnoozedUntil { get; set; }

    /// <summary>
    /// When the snooze was initiated. Stored as UTC.
    /// </summary>
    public DateTime? SnoozedAt { get; set; }

    /// <summary>
    /// User who snoozed the ticket.
    /// </summary>
    public Guid? SnoozedById { get; set; }
    public virtual User? SnoozedBy { get; set; }

    /// <summary>
    /// Optional note explaining why the ticket was snoozed.
    /// </summary>
    public string? SnoozedReason { get; set; }

    /// <summary>
    /// When the ticket was last unsnoozed. Used for "recently unsnoozed" indicator.
    /// </summary>
    public DateTime? UnsnoozedAt { get; set; }

    // Collections
    public virtual ICollection<TicketChangeLogEntry> ChangeLogEntries { get; set; } =
        new List<TicketChangeLogEntry>();
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public virtual ICollection<TicketAttachment> Attachments { get; set; } =
        new List<TicketAttachment>();
    public virtual ICollection<TicketFollower> Followers { get; set; } = new List<TicketFollower>();
    public virtual ICollection<TicketTask> Tasks { get; set; } = new List<TicketTask>();

    /// <summary>
    /// Gets the ticket status as a value object.
    /// </summary>
    [NotMapped]
    public TicketStatus StatusValue => TicketStatus.From(Status);

    /// <summary>
    /// Gets the ticket priority as a value object.
    /// </summary>
    [NotMapped]
    public TicketPriority PriorityValue => TicketPriority.From(Priority);

    /// <summary>
    /// Gets the ticket language as a value object.
    /// </summary>
    [NotMapped]
    public TicketLanguage LanguageValue => TicketLanguage.From(Language);

    /// <summary>
    /// Gets the SLA status as a value object, if set.
    /// </summary>
    [NotMapped]
    public SlaStatus? SlaStatusValue =>
        string.IsNullOrEmpty(SlaStatus) ? null : ValueObjects.SlaStatus.From(SlaStatus);

    /// <summary>
    /// Returns true if the ticket is currently snoozed (has a future unsnooze time).
    /// </summary>
    [NotMapped]
    public bool IsSnoozed => SnoozedUntil != null && SnoozedUntil > DateTime.UtcNow;

    /// <summary>
    /// Returns true if the ticket was unsnoozed within the last 30 minutes.
    /// Used for visual indicator in UI.
    /// </summary>
    [NotMapped]
    public bool IsRecentlyUnsnoozed =>
        UnsnoozedAt != null && DateTime.UtcNow - UnsnoozedAt.Value < TimeSpan.FromMinutes(30);
}
