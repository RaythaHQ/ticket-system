namespace App.Application.Scheduler.Services;

/// <summary>
/// Calculates available time slots for staff members based on org hours,
/// personal availability, existing appointments, and buffer times.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Gets available time slots for a staff member on a specific date for a given appointment type.
    /// Availability = (org hours ∩ staff hours) − existing appointments − buffers.
    /// </summary>
    /// <param name="staffMemberId">The scheduler staff member's ID.</param>
    /// <param name="date">The date to check availability for (in org timezone).</param>
    /// <param name="appointmentTypeId">The appointment type ID (for type-specific duration/buffer).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available time slots.</returns>
    Task<List<AvailableSlot>> GetAvailableSlotsAsync(
        Guid staffMemberId,
        DateTime date,
        Guid appointmentTypeId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks whether a specific time slot is available for a staff member (no overlaps).
    /// </summary>
    Task<bool> IsSlotAvailableAsync(
        Guid staffMemberId,
        DateTime startTimeUtc,
        int durationMinutes,
        long? excludeAppointmentId = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Represents an available time slot.
/// </summary>
public record AvailableSlot
{
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public int DurationMinutes { get; init; }
}
