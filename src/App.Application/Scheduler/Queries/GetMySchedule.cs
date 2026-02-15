using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.DTOs;
using App.Application.Scheduler.Services;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Queries;

public class GetMySchedule
{
    public record Query : IRequest<IQueryResponseDto<StaffScheduleDto>>, ILoggableQuery
    {
        /// <summary>
        /// The date to get the schedule for.
        /// </summary>
        public DateTime Date { get; init; }

        /// <summary>
        /// View type: "day" or "week".
        /// </summary>
        public string ViewType { get; init; } = "day";

        /// <summary>
        /// Optional staff member ID for manage-others-calendars use case.
        /// If null, uses the current user's staff member ID.
        /// </summary>
        public ShortGuid? StaffMemberId { get; init; }

        public string GetLogName() => "Scheduler.Queries.GetMySchedule";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<StaffScheduleDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IAvailabilityService _availabilityService;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            IAvailabilityService availabilityService,
            ISchedulerPermissionService permissionService
        )
        {
            _db = db;
            _currentUser = currentUser;
            _availabilityService = availabilityService;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<StaffScheduleDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            Guid staffMemberGuid;

            if (request.StaffMemberId.HasValue)
            {
                // Viewing another staff member's schedule — requires CanManageOthersCalendars
                var currentStaffId =
                    await _permissionService.GetCurrentStaffMemberIdAsync(cancellationToken);
                if (!currentStaffId.HasValue)
                    throw new ForbiddenAccessException(
                        "Current user is not a scheduler staff member."
                    );

                var currentStaff = await _db
                    .SchedulerStaffMembers.AsNoTracking()
                    .FirstOrDefaultAsync(
                        s => s.Id == currentStaffId.Value,
                        cancellationToken
                    );

                if (
                    currentStaff == null
                    || (
                        !currentStaff.CanManageOthersCalendars
                        && currentStaff.Id != request.StaffMemberId.Value.Guid
                    )
                )
                {
                    throw new ForbiddenAccessException(
                        "You do not have permission to view this staff member's schedule."
                    );
                }

                staffMemberGuid = request.StaffMemberId.Value.Guid;
            }
            else
            {
                // Viewing own schedule
                var currentStaffId =
                    await _permissionService.GetCurrentStaffMemberIdAsync(cancellationToken);
                if (!currentStaffId.HasValue)
                    throw new NotFoundException(
                        "No scheduler staff member record found for the current user."
                    );

                staffMemberGuid = currentStaffId.Value;
            }

            // Determine date range
            var viewType = request.ViewType?.ToLower() ?? "day";
            DateTime dateFrom;
            DateTime dateTo;

            if (viewType == "week")
            {
                // Start of week (Monday)
                var dayOfWeek = request.Date.DayOfWeek;
                var daysToMonday =
                    dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
                dateFrom = DateTime.SpecifyKind(request.Date.Date.AddDays(-daysToMonday), DateTimeKind.Utc);
                dateTo = DateTime.SpecifyKind(dateFrom.AddDays(7), DateTimeKind.Utc);
            }
            else
            {
                dateFrom = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
                dateTo = DateTime.SpecifyKind(dateFrom.AddDays(1), DateTimeKind.Utc);
            }

            // Get appointments for the date range
            var appointments = await _db
                .Appointments.AsNoTracking()
                .Include(a => a.Contact)
                .Include(a => a.AssignedStaffMember)
                    .ThenInclude(s => s.User)
                .Include(a => a.AppointmentType)
                .Where(a =>
                    a.AssignedStaffMemberId == staffMemberGuid
                    && a.ScheduledStartTime < dateTo
                    && a.ScheduledStartTime.AddMinutes(a.DurationMinutes) > dateFrom
                    && a.Status != Domain.ValueObjects.AppointmentStatus.CANCELLED
                )
                .OrderBy(a => a.ScheduledStartTime)
                .ToListAsync(cancellationToken);

            var appointmentDtos = appointments
                .Select(AppointmentListItemDto.MapFrom)
                .ToList();

            // Get available slots for each day in the range
            var availableSlots = new List<AvailableSlot>();
            for (var date = dateFrom; date < dateTo; date = date.AddDays(1))
            {
                // Use a default appointment type for general availability
                // We pass Guid.Empty to get general availability without type-specific duration
                var slots = await _availabilityService.GetAvailableSlotsAsync(
                    staffMemberGuid,
                    date,
                    Guid.Empty,
                    cancellationToken
                );
                availableSlots.AddRange(slots);
            }

            var result = new StaffScheduleDto
            {
                Appointments = appointmentDtos,
                AvailableSlots = availableSlots,
                DateFrom = dateFrom,
                DateTo = dateTo,
            };

            return new QueryResponseDto<StaffScheduleDto>(result);
        }
    }
}
