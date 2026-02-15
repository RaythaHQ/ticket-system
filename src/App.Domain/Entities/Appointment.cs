using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.ValueObjects;

namespace App.Domain.Entities;

/// <summary>
/// Core scheduled appointment entity. A time-bound event linking a staff member to a Contact.
/// Uses numeric (long) ID for human-readable appointment codes (APT-0001).
/// </summary>
public class Appointment : BaseNumericFullAuditableEntity
{
    /// <summary>
    /// The contact (patient) for this appointment.
    /// </summary>
    public long ContactId { get; set; }
    public virtual Contact Contact { get; set; } = null!;

    /// <summary>
    /// The scheduler staff member conducting the appointment.
    /// </summary>
    public Guid AssignedStaffMemberId { get; set; }
    public virtual SchedulerStaffMember AssignedStaffMember { get; set; } = null!;

    /// <summary>
    /// The appointment type (e.g., "Initial Consultation", "Home Visit").
    /// </summary>
    public Guid AppointmentTypeId { get; set; }
    public virtual AppointmentType AppointmentType { get; set; } = null!;

    /// <summary>
    /// Per-appointment contact first name. Pre-populated from Contact record, can be overridden.
    /// </summary>
    public string ContactFirstName { get; set; } = string.Empty;

    /// <summary>
    /// Per-appointment contact last name. Pre-populated from Contact record, can be overridden.
    /// </summary>
    public string? ContactLastName { get; set; }

    /// <summary>
    /// Per-appointment contact email. Pre-populated from Contact record, can be overridden.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Per-appointment contact phone. Pre-populated from Contact record, can be overridden.
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Per-appointment contact address. Pre-populated from Contact record, can be overridden.
    /// </summary>
    public string? ContactAddress { get; set; }

    /// <summary>
    /// Resolved mode for this appointment: "virtual" or "in_person".
    /// Determined by the appointment type's mode setting (or chosen by staff for "either" types).
    /// </summary>
    public string Mode { get; set; } = null!;

    /// <summary>
    /// URL for virtual appointments. Required when Mode = "virtual".
    /// </summary>
    public string? MeetingLink { get; set; }

    /// <summary>
    /// When the appointment is scheduled to start. Stored as UTC.
    /// </summary>
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>
    /// Duration of the appointment in minutes.
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Current lifecycle status. Stored as AppointmentStatus DeveloperName.
    /// </summary>
    public string Status { get; set; } = AppointmentStatus.SCHEDULED;

    /// <summary>
    /// Free-text notes for this appointment.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Reason provided when the appointment was cancelled.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Reason provided when an in-person appointment was booked outside the coverage zone.
    /// </summary>
    public string? CoverageZoneOverrideReason { get; set; }

    /// <summary>
    /// Reason provided when cancellation/reschedule occurred within the minimum notice period.
    /// </summary>
    public string? CancellationNoticeOverrideReason { get; set; }

    /// <summary>
    /// When the reminder notification was sent. Null until sent. Prevents duplicate reminders.
    /// </summary>
    public DateTime? ReminderSentAt { get; set; }

    /// <summary>
    /// The staff member who created this appointment (may differ from AssignedStaffMember
    /// when someone with "Can Manage Others' Calendars" creates on behalf of another).
    /// </summary>
    public Guid CreatedByStaffId { get; set; }
    public virtual User CreatedByStaff { get; set; } = null!;

    /// <summary>
    /// Human-readable appointment code (e.g., "APT-0001234").
    /// </summary>
    [NotMapped]
    public string Code => $"APT-{Id:D4}";

    /// <summary>
    /// Parsed appointment status value object.
    /// </summary>
    [NotMapped]
    public AppointmentStatus StatusValue => AppointmentStatus.From(Status);

    /// <summary>
    /// Parsed appointment mode value object.
    /// </summary>
    [NotMapped]
    public AppointmentMode ModeValue => AppointmentMode.From(Mode);

    /// <summary>
    /// Computed end time based on start time and duration.
    /// </summary>
    [NotMapped]
    public DateTime ScheduledEndTime => ScheduledStartTime.AddMinutes(DurationMinutes);

    // Navigation collections
    public virtual ICollection<AppointmentHistory> History { get; set; } =
        new List<AppointmentHistory>();
}
