namespace App.Application.SchedulerAdmin.DTOs;

/// <summary>
/// DTO for scheduler reports dashboard data.
/// </summary>
public record SchedulerReportDto
{
    /// <summary>
    /// Count of appointments by status (e.g., "scheduled": 10, "completed": 25).
    /// </summary>
    public Dictionary<string, int> AppointmentsByStatus { get; init; } = new();

    /// <summary>
    /// Appointment volume over time (daily counts within the date range).
    /// </summary>
    public List<DailyVolumeItem> AppointmentVolumeByDate { get; init; } = new();

    /// <summary>
    /// Staff utilization data (appointment counts and rates per staff member).
    /// </summary>
    public List<StaffUtilizationItem> StaffUtilization { get; init; } = new();

    /// <summary>
    /// Percentage of appointments that resulted in no-show.
    /// </summary>
    public decimal NoShowRate { get; init; }

    /// <summary>
    /// Percentage of appointments that were cancelled.
    /// </summary>
    public decimal CancellationRate { get; init; }

    /// <summary>
    /// Average duration of completed appointments in minutes.
    /// </summary>
    public decimal AverageAppointmentDurationMinutes { get; init; }

    public record DailyVolumeItem
    {
        public DateTime Date { get; init; }
        public int Count { get; init; }
    }

    public record StaffUtilizationItem
    {
        public string StaffName { get; init; } = string.Empty;
        public int AppointmentCount { get; init; }
        public decimal UtilizationRate { get; init; }
    }
}
