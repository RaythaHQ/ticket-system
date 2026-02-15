using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;

namespace App.Application.SchedulerAdmin.DTOs;

/// <summary>
/// Full detail DTO for an appointment type (admin view).
/// </summary>
public record AppointmentTypeDto
{
    public ShortGuid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public string ModeLabel { get; init; } = string.Empty;
    public int? DefaultDurationMinutes { get; init; }
    public int? BufferTimeMinutes { get; init; }
    public int? BookingHorizonDays { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public List<EligibleStaffInfo> EligibleStaff { get; init; } = new();
    public DateTime CreationTime { get; init; }

    public record EligibleStaffInfo
    {
        public ShortGuid StaffMemberId { get; init; }
        public string FullName { get; init; } = string.Empty;
    }

    public static AppointmentTypeDto MapFrom(AppointmentType type)
    {
        return new AppointmentTypeDto
        {
            Id = type.Id,
            Name = type.Name,
            Mode = type.Mode,
            ModeLabel = AppointmentMode.From(type.Mode).Label,
            DefaultDurationMinutes = type.DefaultDurationMinutes,
            BufferTimeMinutes = type.BufferTimeMinutes,
            BookingHorizonDays = type.BookingHorizonDays,
            IsActive = type.IsActive,
            SortOrder = type.SortOrder,
            EligibleStaff = type
                .EligibleStaff?.Select(e => new EligibleStaffInfo
                {
                    StaffMemberId = e.SchedulerStaffMemberId,
                    FullName = e.SchedulerStaffMember?.User != null
                        ? e.SchedulerStaffMember.User.FirstName
                            + " "
                            + e.SchedulerStaffMember.User.LastName
                        : string.Empty,
                })
                .ToList()
                ?? new List<EligibleStaffInfo>(),
            CreationTime = type.CreationTime,
        };
    }
}

/// <summary>
/// Lightweight DTO for appointment type list views.
/// </summary>
public record AppointmentTypeListItemDto
{
    public ShortGuid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public string ModeLabel { get; init; } = string.Empty;
    public int? DefaultDurationMinutes { get; init; }
    public bool IsActive { get; init; }
    public int EligibleStaffCount { get; init; }
    public DateTime CreationTime { get; init; }

    public static AppointmentTypeListItemDto MapFrom(AppointmentType type)
    {
        return new AppointmentTypeListItemDto
        {
            Id = type.Id,
            Name = type.Name,
            Mode = type.Mode,
            ModeLabel = AppointmentMode.From(type.Mode).Label,
            DefaultDurationMinutes = type.DefaultDurationMinutes,
            IsActive = type.IsActive,
            EligibleStaffCount = type.EligibleStaff?.Count ?? 0,
            CreationTime = type.CreationTime,
        };
    }
}
