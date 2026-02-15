namespace App.Domain.Entities;

/// <summary>
/// Represents a time block when a scheduler staff member is unavailable.
/// Used for lunch breaks, PTO, meetings, and other non-appointment unavailability.
/// Integrates with AvailabilityService to prevent booking during blocked periods.
/// </summary>
public class StaffBlockOutTime : BaseAuditableEntity
{
    /// <summary>
    /// The scheduler staff member this block-out applies to.
    /// </summary>
    public Guid StaffMemberId { get; set; }
    public virtual SchedulerStaffMember StaffMember { get; set; } = null!;

    /// <summary>
    /// Short descriptive title (e.g., "Lunch", "PTO", "Team Meeting").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Start of the block-out period. Stored as UTC.
    /// </summary>
    public DateTime StartTimeUtc { get; set; }

    /// <summary>
    /// End of the block-out period. Stored as UTC.
    /// </summary>
    public DateTime EndTimeUtc { get; set; }

    /// <summary>
    /// Whether this block-out covers the entire day(s).
    /// When true, StartTimeUtc and EndTimeUtc represent date boundaries.
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Optional longer description or reason for the block-out.
    /// </summary>
    public string? Reason { get; set; }
}
