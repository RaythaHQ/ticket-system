using App.Application.Common.Interfaces;
using App.Application.Scheduler.Services;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

/// <summary>
/// Calculates available time slots by intersecting org hours with staff hours,
/// then subtracting existing appointments and buffer times.
/// </summary>
public class AvailabilityService : IAvailabilityService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentOrganization _currentOrganization;

    public AvailabilityService(IAppDbContext db, ICurrentOrganization currentOrganization)
    {
        _db = db;
        _currentOrganization = currentOrganization;
    }

    public async Task<List<AvailableSlot>> GetAvailableSlotsAsync(
        Guid staffMemberId,
        DateTime date,
        Guid appointmentTypeId,
        CancellationToken cancellationToken = default
    )
    {
        var staffMember = await _db
            .SchedulerStaffMembers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == staffMemberId && s.IsActive, cancellationToken);

        if (staffMember == null)
            return new List<AvailableSlot>();

        var orgConfig = await _db
            .SchedulerConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (orgConfig == null)
            return new List<AvailableSlot>();

        var appointmentType = await _db
            .AppointmentTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == appointmentTypeId && t.IsActive, cancellationToken);

        if (appointmentType == null)
            return new List<AvailableSlot>();

        // Determine effective settings (type-specific overrides > org defaults)
        var durationMinutes =
            appointmentType.DefaultDurationMinutes ?? orgConfig.DefaultDurationMinutes;
        var bufferMinutes = appointmentType.BufferTimeMinutes ?? orgConfig.DefaultBufferTimeMinutes;

        // Get the day of week for the requested date
        var dayOfWeek = date.DayOfWeek.ToString().ToLower();

        // Determine the working window: org hours ∩ staff hours
        var orgHours = orgConfig.AvailableHours;
        var staffHours = staffMember.Availability;

        if (!orgHours.TryGetValue(dayOfWeek, out var orgDay))
            return new List<AvailableSlot>(); // Org not open on this day

        // Staff hours (if set, intersect with org; if not set, use org)
        DaySchedule effectiveDay;
        if (staffHours.TryGetValue(dayOfWeek, out var staffDay))
        {
            // Intersect: take the later start and earlier end
            var effectiveStart =
                TimeSpan.Parse(orgDay.Start) > TimeSpan.Parse(staffDay.Start)
                    ? orgDay.Start
                    : staffDay.Start;
            var effectiveEnd =
                TimeSpan.Parse(orgDay.End) < TimeSpan.Parse(staffDay.End)
                    ? orgDay.End
                    : staffDay.End;
            effectiveDay = new DaySchedule { Start = effectiveStart, End = effectiveEnd };
        }
        else
        {
            effectiveDay = orgDay;
        }

        var startTime = TimeSpan.Parse(effectiveDay.Start);
        var endTime = TimeSpan.Parse(effectiveDay.End);

        if (startTime >= endTime)
            return new List<AvailableSlot>(); // Invalid range

        // Convert to UTC for comparison with existing appointments
        var tz = TimeZoneInfo.FindSystemTimeZoneById(_currentOrganization.TimeZone);
        var dayStartLocal = date.Date + startTime;
        var dayEndLocal = date.Date + endTime;
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, tz);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(dayEndLocal, tz);

        // Get existing active appointments for this staff member on this day
        var existingAppointments = await _db
            .Appointments.AsNoTracking()
            .Where(a =>
                a.AssignedStaffMemberId == staffMemberId
                && a.ScheduledStartTime >= dayStartUtc
                && a.ScheduledStartTime < dayEndUtc
                && (
                    a.Status == AppointmentStatus.SCHEDULED
                    || a.Status == AppointmentStatus.CONFIRMED
                    || a.Status == AppointmentStatus.IN_PROGRESS
                )
            )
            .OrderBy(a => a.ScheduledStartTime)
            .Select(a => new { a.ScheduledStartTime, a.DurationMinutes })
            .ToListAsync(cancellationToken);

        // Get block-out times for this staff member on this day
        var blockOutTimes = await _db
            .StaffBlockOutTimes.AsNoTracking()
            .Where(b =>
                b.StaffMemberId == staffMemberId
                && b.StartTimeUtc < dayEndUtc
                && b.EndTimeUtc > dayStartUtc
            )
            .Select(b => new { b.StartTimeUtc, b.EndTimeUtc })
            .ToListAsync(cancellationToken);

        // Build blocked time ranges (appointments with buffer + block-out times)
        var blockedRanges = existingAppointments
            .Select(a => (
                Start: a.ScheduledStartTime.AddMinutes(-bufferMinutes),
                End: a.ScheduledStartTime.AddMinutes(a.DurationMinutes + bufferMinutes)
            ))
            .Concat(blockOutTimes.Select(b => (Start: b.StartTimeUtc, End: b.EndTimeUtc)))
            .ToList();

        // Generate available slots
        var slots = new List<AvailableSlot>();
        var currentSlotStart = dayStartUtc;

        while (currentSlotStart.AddMinutes(durationMinutes) <= dayEndUtc)
        {
            var currentSlotEnd = currentSlotStart.AddMinutes(durationMinutes);

            // Check if this slot overlaps with any blocked range
            var isBlocked = blockedRanges.Any(blocked =>
                currentSlotStart < blocked.End && currentSlotEnd > blocked.Start
            );

            if (!isBlocked)
            {
                slots.Add(
                    new AvailableSlot
                    {
                        StartTimeUtc = currentSlotStart,
                        EndTimeUtc = currentSlotEnd,
                        DurationMinutes = durationMinutes,
                    }
                );
            }

            // Move to next potential slot (use buffer as step if within a blocked range)
            currentSlotStart = currentSlotStart.AddMinutes(
                isBlocked ? bufferMinutes : durationMinutes + bufferMinutes
            );
        }

        return slots;
    }

    public async Task<bool> IsSlotAvailableAsync(
        Guid staffMemberId,
        DateTime startTimeUtc,
        int durationMinutes,
        long? excludeAppointmentId = null,
        CancellationToken cancellationToken = default
    )
    {
        // Ensure UTC kind for PostgreSQL timestamptz compatibility
        startTimeUtc = DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc);
        var endTimeUtc = DateTime.SpecifyKind(startTimeUtc.AddMinutes(durationMinutes), DateTimeKind.Utc);

        var query = _db
            .Appointments.AsNoTracking()
            .Where(a =>
                a.AssignedStaffMemberId == staffMemberId
                && (
                    a.Status == AppointmentStatus.SCHEDULED
                    || a.Status == AppointmentStatus.CONFIRMED
                    || a.Status == AppointmentStatus.IN_PROGRESS
                )
                && a.ScheduledStartTime < endTimeUtc
                && a.ScheduledStartTime.AddMinutes(a.DurationMinutes) > startTimeUtc
            );

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        var hasConflictingAppointment = await query.AnyAsync(cancellationToken);
        if (hasConflictingAppointment)
            return false;

        // Also check block-out times
        var hasConflictingBlockOut = await _db
            .StaffBlockOutTimes.AsNoTracking()
            .AnyAsync(b =>
                b.StaffMemberId == staffMemberId
                && b.StartTimeUtc < endTimeUtc
                && b.EndTimeUtc > startTimeUtc,
                cancellationToken);

        return !hasConflictingBlockOut;
    }
}
