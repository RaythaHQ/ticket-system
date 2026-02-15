using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.SchedulerAdmin.DTOs;

/// <summary>
/// DTO for org-wide scheduler configuration.
/// </summary>
public record SchedulerConfigurationDto
{
    public ShortGuid Id { get; init; }
    public Dictionary<string, DaySchedule> AvailableHours { get; init; } = new();
    public int DefaultDurationMinutes { get; init; }
    public int DefaultBufferTimeMinutes { get; init; }
    public int DefaultBookingHorizonDays { get; init; }
    public int MinCancellationNoticeHours { get; init; }
    public int ReminderLeadTimeMinutes { get; init; }
    public List<string> DefaultCoverageZones { get; init; } = new();

    public static SchedulerConfigurationDto MapFrom(SchedulerConfiguration config)
    {
        return new SchedulerConfigurationDto
        {
            Id = config.Id,
            AvailableHours = config.AvailableHours,
            DefaultDurationMinutes = config.DefaultDurationMinutes,
            DefaultBufferTimeMinutes = config.DefaultBufferTimeMinutes,
            DefaultBookingHorizonDays = config.DefaultBookingHorizonDays,
            MinCancellationNoticeHours = config.MinCancellationNoticeHours,
            ReminderLeadTimeMinutes = config.ReminderLeadTimeMinutes,
            DefaultCoverageZones = config.DefaultCoverageZones,
        };
    }
}
