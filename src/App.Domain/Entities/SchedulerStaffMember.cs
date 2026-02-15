using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace App.Domain.Entities;

/// <summary>
/// Represents an active admin who has been added to the scheduler system.
/// Links a User to scheduler capabilities (calendar management, availability, coverage zones).
/// </summary>
public class SchedulerStaffMember : BaseAuditableEntity
{
    /// <summary>
    /// The user this staff member is linked to. One staff record per user (unique).
    /// </summary>
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Whether this staff member can create, edit, and cancel appointments on behalf of other staff members.
    /// </summary>
    public bool CanManageOthersCalendars { get; set; } = false;

    /// <summary>
    /// Soft disable without removing. When false, the staff member loses access to /staff/scheduler.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Default meeting link for virtual appointments. Used when no per-appointment link is provided.
    /// </summary>
    public string? DefaultMeetingLink { get; set; }

    /// <summary>
    /// JSON: per-day hours, e.g. {"monday":{"start":"09:00","end":"17:00"},...}
    /// Null means not yet configured (defaults to org-wide hours).
    /// </summary>
    public string? AvailabilityJson { get; set; }

    /// <summary>
    /// JSON array of zipcodes, e.g. ["10001","10002"].
    /// Null means use org-wide default coverage zones.
    /// </summary>
    public string? CoverageZonesJson { get; set; }

    [NotMapped]
    public Dictionary<string, DaySchedule> Availability
    {
        get => string.IsNullOrEmpty(AvailabilityJson)
            ? new Dictionary<string, DaySchedule>()
            : JsonSerializer.Deserialize<Dictionary<string, DaySchedule>>(AvailabilityJson)
                ?? new Dictionary<string, DaySchedule>();
        set => AvailabilityJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<string> CoverageZones
    {
        get => string.IsNullOrEmpty(CoverageZonesJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(CoverageZonesJson) ?? new List<string>();
        set => CoverageZonesJson = JsonSerializer.Serialize(value);
    }

    // Navigation collections
    public virtual ICollection<AppointmentTypeStaffEligibility> EligibleAppointmentTypes { get; set; } =
        new List<AppointmentTypeStaffEligibility>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<StaffBlockOutTime> BlockOutTimes { get; set; } = new List<StaffBlockOutTime>();
}

/// <summary>
/// Represents a time window for a single day (start and end times).
/// </summary>
public class DaySchedule
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
}
