using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.SchedulerAdmin.DTOs;

/// <summary>
/// Full detail DTO for a scheduler staff member (admin view).
/// </summary>
public record SchedulerStaffDto
{
    public ShortGuid Id { get; init; }
    public ShortGuid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool CanManageOthersCalendars { get; init; }
    public bool IsActive { get; init; }
    public string? DefaultMeetingLink { get; init; }
    public Dictionary<string, DaySchedule> Availability { get; init; } = new();
    public List<string> CoverageZones { get; init; } = new();
    public List<EligibleTypeInfo> EligibleAppointmentTypes { get; init; } = new();
    public DateTime CreationTime { get; init; }

    public record EligibleTypeInfo
    {
        public ShortGuid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public static SchedulerStaffDto MapFrom(SchedulerStaffMember staff)
    {
        return new SchedulerStaffDto
        {
            Id = staff.Id,
            UserId = staff.UserId,
            FullName = staff.User?.FirstName + " " + staff.User?.LastName,
            Email = staff.User?.EmailAddress ?? string.Empty,
            CanManageOthersCalendars = staff.CanManageOthersCalendars,
            IsActive = staff.IsActive,
            DefaultMeetingLink = staff.DefaultMeetingLink,
            Availability = staff.Availability,
            CoverageZones = staff.CoverageZones,
            EligibleAppointmentTypes = staff
                .EligibleAppointmentTypes?.Select(e => new EligibleTypeInfo
                {
                    Id = e.AppointmentTypeId,
                    Name = e.AppointmentType?.Name ?? string.Empty,
                })
                .ToList()
                ?? new List<EligibleTypeInfo>(),
            CreationTime = staff.CreationTime,
        };
    }
}

/// <summary>
/// Lightweight DTO for staff list views.
/// </summary>
public record SchedulerStaffListItemDto
{
    public ShortGuid Id { get; init; }
    public ShortGuid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool CanManageOthersCalendars { get; init; }
    public bool IsActive { get; init; }
    public int CoverageZonesCount { get; init; }
    public DateTime CreationTime { get; init; }

    public static SchedulerStaffListItemDto MapFrom(SchedulerStaffMember staff)
    {
        return new SchedulerStaffListItemDto
        {
            Id = staff.Id,
            UserId = staff.UserId,
            FullName = staff.User?.FirstName + " " + staff.User?.LastName,
            Email = staff.User?.EmailAddress ?? string.Empty,
            CanManageOthersCalendars = staff.CanManageOthersCalendars,
            IsActive = staff.IsActive,
            CoverageZonesCount = staff.CoverageZones.Count,
            CreationTime = staff.CreationTime,
        };
    }
}
