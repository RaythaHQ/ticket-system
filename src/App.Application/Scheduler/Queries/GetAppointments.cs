using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Application.Scheduler.DTOs;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Queries;

public class GetAppointments
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<AppointmentListItemDto>>>,
            ILoggableQuery
    {
        public override string OrderBy { get; init; } =
            $"ScheduledStartTime {SortOrder.DESCENDING}";

        public string GetLogName() => "Scheduler.Queries.GetAppointments";

        /// <summary>
        /// Search by appointment code or contact name.
        /// </summary>
        public override string Search { get; init; } = string.Empty;

        /// <summary>
        /// Optional filter by assigned staff member.
        /// </summary>
        public ShortGuid? StaffMemberId { get; init; }

        /// <summary>
        /// Optional filter by appointment type.
        /// </summary>
        public ShortGuid? AppointmentTypeId { get; init; }

        /// <summary>
        /// Optional filter by status developer name.
        /// </summary>
        public string? Status { get; init; }

        /// <summary>
        /// Optional filter: appointments starting on or after this date.
        /// </summary>
        public DateTime? DateFrom { get; init; }

        /// <summary>
        /// Optional filter: appointments starting before this date.
        /// </summary>
        public DateTime? DateTo { get; init; }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<AppointmentListItemDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(IAppDbContext db, ISchedulerPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<
            IQueryResponseDto<ListResultDto<AppointmentListItemDto>>
        > Handle(Query request, CancellationToken cancellationToken)
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var query = _db
                .Appointments.AsNoTracking()
                .Include(a => a.Contact)
                .Include(a => a.AssignedStaffMember)
                    .ThenInclude(s => s.User)
                .Include(a => a.AppointmentType)
                .AsQueryable();

            // Apply filters
            if (request.StaffMemberId.HasValue)
            {
                query = query.Where(a =>
                    a.AssignedStaffMemberId == request.StaffMemberId.Value.Guid
                );
            }

            if (request.AppointmentTypeId.HasValue)
            {
                query = query.Where(a =>
                    a.AppointmentTypeId == request.AppointmentTypeId.Value.Guid
                );
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                var statusLower = request.Status.ToLower();
                query = query.Where(a => a.Status == statusLower);
            }

            if (request.DateFrom.HasValue)
            {
                var dateFromUtc = DateTime.SpecifyKind(request.DateFrom.Value, DateTimeKind.Utc);
                query = query.Where(a =>
                    a.ScheduledStartTime >= dateFromUtc
                );
            }

            if (request.DateTo.HasValue)
            {
                var dateToUtc = DateTime.SpecifyKind(request.DateTo.Value, DateTimeKind.Utc);
                query = query.Where(a =>
                    a.ScheduledStartTime < dateToUtc
                );
            }

            // Apply search (code or contact name)
            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchLower = request.Search.ToLower();

                // Try to parse as appointment ID from code format "APT-XXXX"
                long? searchId = null;
                if (
                    searchLower.StartsWith("apt-")
                    && long.TryParse(searchLower[4..], out var parsedId)
                )
                {
                    searchId = parsedId;
                }
                else if (long.TryParse(request.Search, out var numericId))
                {
                    searchId = numericId;
                }

                query = query.Where(a =>
                    (searchId.HasValue && a.Id == searchId.Value)
                    || (
                        a.Contact != null
                        && (
                            a.Contact.FirstName.ToLower().Contains(searchLower)
                            || (
                                a.Contact.LastName != null
                                && a.Contact.LastName.ToLower().Contains(searchLower)
                            )
                        )
                    )
                );
            }

            var total = await query.CountAsync(cancellationToken);

            var items = query
                .ApplyPaginationInput(request)
                .Select(a => AppointmentListItemDto.MapFrom(a))
                .ToArray();

            return new QueryResponseDto<ListResultDto<AppointmentListItemDto>>(
                new ListResultDto<AppointmentListItemDto>(items, total)
            );
        }
    }
}
