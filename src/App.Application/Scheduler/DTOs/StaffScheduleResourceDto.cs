using CSharpVitamins;

namespace App.Application.Scheduler.DTOs;

/// <summary>
/// DTO for the multi-staff resource calendar view. Contains schedule data
/// for multiple staff members in a date range.
/// </summary>
public record StaffScheduleResourceDto
{
    public DateTime DateFrom { get; init; }
    public DateTime DateTo { get; init; }
    public List<StaffColumnDto> StaffColumns { get; init; } = new();
}

/// <summary>
/// One column in the resource calendar representing a single staff member's schedule.
/// </summary>
public record StaffColumnDto
{
    public ShortGuid StaffMemberId { get; init; }
    public string StaffMemberName { get; init; } = string.Empty;
    public List<AppointmentListItemDto> Appointments { get; init; } = new();
    public List<BlockOutTimeDto> BlockOutTimes { get; init; } = new();
}
