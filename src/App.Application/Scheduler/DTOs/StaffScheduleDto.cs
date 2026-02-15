using App.Application.Scheduler.Services;

namespace App.Application.Scheduler.DTOs;

/// <summary>
/// DTO representing a staff member's schedule for a day/week.
/// </summary>
public record StaffScheduleDto
{
    /// <summary>
    /// Appointments for this staff member in the requested time range.
    /// </summary>
    public List<AppointmentListItemDto> Appointments { get; init; } = new();

    /// <summary>
    /// Available time slots in the requested time range.
    /// </summary>
    public List<AvailableSlot> AvailableSlots { get; init; } = new();

    /// <summary>
    /// Block-out times for this staff member in the requested time range.
    /// </summary>
    public List<BlockOutTimeDto> BlockOutTimes { get; init; } = new();

    /// <summary>
    /// The date range that was queried.
    /// </summary>
    public DateTime DateFrom { get; init; }
    public DateTime DateTo { get; init; }
}

/// <summary>
/// DTO for staff availability query results.
/// </summary>
public record StaffAvailabilityDto
{
    public List<AvailableSlot> AvailableSlots { get; init; } = new();
    public List<BookedSlot> BookedSlots { get; init; } = new();
}

/// <summary>
/// Represents a booked time slot.
/// </summary>
public record BookedSlot
{
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public string AppointmentCode { get; init; } = string.Empty;
}
