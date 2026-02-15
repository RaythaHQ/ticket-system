using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.DTOs;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Queries;

public class GetStaffSchedule
{
    public record Query : IRequest<IQueryResponseDto<StaffScheduleResourceDto>>, ILoggableQuery
    {
        /// <summary>
        /// Staff member IDs to include. If empty, includes all active staff.
        /// </summary>
        public List<ShortGuid> StaffMemberIds { get; init; } = new();

        /// <summary>
        /// Optional filter by appointment type.
        /// </summary>
        public ShortGuid? AppointmentTypeId { get; init; }

        /// <summary>
        /// The start date for the schedule view.
        /// </summary>
        public DateTime DateFrom { get; init; }

        /// <summary>
        /// The end date for the schedule view.
        /// </summary>
        public DateTime DateTo { get; init; }

        public string GetLogName() => "Scheduler.Queries.GetStaffSchedule";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<StaffScheduleResourceDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(IAppDbContext db, ISchedulerPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<StaffScheduleResourceDto>> Handle(
            Query request, CancellationToken cancellationToken)
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var dateFrom = DateTime.SpecifyKind(request.DateFrom, DateTimeKind.Utc);
            var dateTo = DateTime.SpecifyKind(request.DateTo, DateTimeKind.Utc);

            // Determine which staff members to show
            var staffQuery = _db.SchedulerStaffMembers.AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.IsActive);

            if (request.StaffMemberIds.Any())
            {
                var staffGuids = request.StaffMemberIds.Select(s => s.Guid).ToList();
                staffQuery = staffQuery.Where(s => staffGuids.Contains(s.Id));
            }

            var staffMembers = await staffQuery
                .OrderBy(s => s.User.FirstName)
                .ThenBy(s => s.User.LastName)
                .ToListAsync(cancellationToken);

            var staffIds = staffMembers.Select(s => s.Id).ToList();

            // Fetch all appointments in range for these staff
            var appointmentsQuery = _db.Appointments.AsNoTracking()
                .Include(a => a.Contact)
                .Include(a => a.AssignedStaffMember)
                    .ThenInclude(s => s.User)
                .Include(a => a.AppointmentType)
                .Where(a =>
                    staffIds.Contains(a.AssignedStaffMemberId)
                    && a.ScheduledStartTime < dateTo
                    && a.ScheduledStartTime.AddMinutes(a.DurationMinutes) > dateFrom
                    && a.Status != AppointmentStatus.CANCELLED
                );

            if (request.AppointmentTypeId.HasValue)
            {
                appointmentsQuery = appointmentsQuery
                    .Where(a => a.AppointmentTypeId == request.AppointmentTypeId.Value.Guid);
            }

            var appointments = await appointmentsQuery
                .OrderBy(a => a.ScheduledStartTime)
                .ToListAsync(cancellationToken);

            // Fetch all block-out times in range for these staff
            var blockOutTimes = await _db.StaffBlockOutTimes.AsNoTracking()
                .Include(b => b.StaffMember)
                    .ThenInclude(s => s.User)
                .Where(b =>
                    staffIds.Contains(b.StaffMemberId)
                    && b.StartTimeUtc < dateTo
                    && b.EndTimeUtc > dateFrom
                )
                .OrderBy(b => b.StartTimeUtc)
                .ToListAsync(cancellationToken);

            // Group into staff columns
            var columns = staffMembers.Select(staff => new StaffColumnDto
            {
                StaffMemberId = staff.Id,
                StaffMemberName = staff.User.FirstName + " " + staff.User.LastName,
                Appointments = appointments
                    .Where(a => a.AssignedStaffMemberId == staff.Id)
                    .Select(AppointmentListItemDto.MapFrom)
                    .ToList(),
                BlockOutTimes = blockOutTimes
                    .Where(b => b.StaffMemberId == staff.Id)
                    .Select(BlockOutTimeDto.MapFrom)
                    .ToList(),
            }).ToList();

            var result = new StaffScheduleResourceDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                StaffColumns = columns,
            };

            return new QueryResponseDto<StaffScheduleResourceDto>(result);
        }
    }
}
