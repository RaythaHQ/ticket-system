namespace App.Domain.Entities;

/// <summary>
/// Immutable audit trail entry for appointment changes.
/// Records status changes, reschedules, cancellations, overrides, and edits.
/// </summary>
public class AppointmentHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The appointment this history entry belongs to.
    /// </summary>
    public long AppointmentId { get; set; }
    public virtual Appointment Appointment { get; set; } = null!;

    /// <summary>
    /// Type of change: "created", "status_changed", "rescheduled", "cancelled", "edited",
    /// "coverage_override", "cancellation_notice_override".
    /// </summary>
    public string ChangeType { get; set; } = null!;

    /// <summary>
    /// Previous value (e.g., old status, old datetime).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value (e.g., new status, new datetime).
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Reason for override (coverage zone, cancellation notice).
    /// </summary>
    public string? OverrideReason { get; set; }

    /// <summary>
    /// The user who made this change.
    /// </summary>
    public Guid ChangedByUserId { get; set; }
    public virtual User ChangedByUser { get; set; } = null!;

    /// <summary>
    /// When this change occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
