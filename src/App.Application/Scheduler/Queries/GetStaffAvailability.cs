using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.DTOs;
using App.Application.Scheduler.Services;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Queries;

public class GetStaffAvailability
{
    public record Query : IRequest<IQueryResponseDto<StaffAvailabilityDto>>, ILoggableQuery
    {
        public ShortGuid StaffMemberId { get; init; }
        public DateTime Date { get; init; }
        public ShortGuid AppointmentTypeId { get; init; }

        public string GetLogName() => "Scheduler.Queries.GetStaffAvailability";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<StaffAvailabilityDto>>
    {
        private readonly IAppDbContext _db;
        private readonly IAvailabilityService _availabilityService;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            IAvailabilityService availabilityService,
            ISchedulerPermissionService permissionService
        )
        {
            _db = db;
            _availabilityService = availabilityService;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<StaffAvailabilityDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            // Validate staff member exists
            var staffMember = await _db
                .SchedulerStaffMembers.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Id == request.StaffMemberId.Guid && s.IsActive,
                    cancellationToken
                );

            if (staffMember == null)
                throw new NotFoundException(
                    "SchedulerStaffMember",
                    request.StaffMemberId
                );

            // Validate appointment type exists
            var appointmentType = await _db
                .AppointmentTypes.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == request.AppointmentTypeId.Guid && t.IsActive,
                    cancellationToken
                );

            if (appointmentType == null)
                throw new NotFoundException(
                    "AppointmentType",
                    request.AppointmentTypeId
                );

            // Get available slots
            var availableSlots = await _availabilityService.GetAvailableSlotsAsync(
                request.StaffMemberId.Guid,
                request.Date.Date,
                request.AppointmentTypeId.Guid,
                cancellationToken
            );

            // Get booked slots for the date
            var dateStart = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
            var dateEnd = DateTime.SpecifyKind(dateStart.AddDays(1), DateTimeKind.Utc);

            var bookedAppointments = await _db
                .Appointments.AsNoTracking()
                .Where(a =>
                    a.AssignedStaffMemberId == request.StaffMemberId.Guid
                    && a.ScheduledStartTime < dateEnd
                    && a.ScheduledStartTime.AddMinutes(a.DurationMinutes) > dateStart
                    && a.Status != AppointmentStatus.CANCELLED
                )
                .OrderBy(a => a.ScheduledStartTime)
                .ToListAsync(cancellationToken);

            var bookedSlots = bookedAppointments
                .Select(a => new BookedSlot
                {
                    StartTimeUtc = a.ScheduledStartTime,
                    EndTimeUtc = a.ScheduledEndTime,
                    AppointmentCode = a.Code,
                })
                .ToList();

            var result = new StaffAvailabilityDto
            {
                AvailableSlots = availableSlots,
                BookedSlots = bookedSlots,
            };

            return new QueryResponseDto<StaffAvailabilityDto>(result);
        }
    }
}
