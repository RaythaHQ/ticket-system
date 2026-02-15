using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace App.Domain.Entities;

/// <summary>
/// Organization-wide scheduler settings. Singleton per organization (single row).
/// Governs scheduling hours, defaults, cancellation policy, reminders, and coverage zones.
/// </summary>
public class SchedulerConfiguration : BaseAuditableEntity
{
    /// <summary>
    /// JSON: per-day schedule defining org-wide available hours.
    /// e.g. {"monday":{"start":"09:00","end":"17:00"},...}
    /// </summary>
    public string AvailableHoursJson { get; set; } = null!;

    /// <summary>
    /// Default appointment duration in minutes. Used when an appointment type doesn't specify its own.
    /// </summary>
    public int DefaultDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Default buffer time between appointments in minutes. Used when a type doesn't specify its own.
    /// </summary>
    public int DefaultBufferTimeMinutes { get; set; } = 15;

    /// <summary>
    /// Default booking horizon (how far in advance appointments can be booked) in days.
    /// </summary>
    public int DefaultBookingHorizonDays { get; set; } = 30;

    /// <summary>
    /// Minimum cancellation/reschedule notice period in hours.
    /// Cancelling within this window triggers a soft warning requiring override reason.
    /// </summary>
    public int MinCancellationNoticeHours { get; set; } = 24;

    /// <summary>
    /// How far before an appointment (in minutes) to send the reminder notification.
    /// </summary>
    public int ReminderLeadTimeMinutes { get; set; } = 60;

    /// <summary>
    /// JSON array of zipcodes defining the org-wide service area for in-person appointments.
    /// Null = no zone restriction.
    /// </summary>
    public string? DefaultCoverageZonesJson { get; set; }

    [NotMapped]
    public Dictionary<string, DaySchedule> AvailableHours
    {
        get => string.IsNullOrEmpty(AvailableHoursJson)
            ? new Dictionary<string, DaySchedule>()
            : JsonSerializer.Deserialize<Dictionary<string, DaySchedule>>(AvailableHoursJson)
                ?? new Dictionary<string, DaySchedule>();
        set => AvailableHoursJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<string> DefaultCoverageZones
    {
        get => string.IsNullOrEmpty(DefaultCoverageZonesJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(DefaultCoverageZonesJson)
                ?? new List<string>();
        set => DefaultCoverageZonesJson = JsonSerializer.Serialize(value);
    }
}
