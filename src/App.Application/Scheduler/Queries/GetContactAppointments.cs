using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Queries;

public class GetContactAppointments
{
    public record Query : IRequest<IQueryResponseDto<ListResultDto<AppointmentListItemDto>>>
    {
        public long ContactId { get; init; }
        public int PageSize { get; init; } = 50;
        public int PageNumber { get; init; } = 1;
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<AppointmentListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<
            IQueryResponseDto<ListResultDto<AppointmentListItemDto>>
        > Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db
                .Appointments.AsNoTracking()
                .Include(a => a.Contact)
                .Include(a => a.AssignedStaffMember)
                    .ThenInclude(s => s.User)
                .Include(a => a.AppointmentType)
                .Where(a => a.ContactId == request.ContactId)
                .OrderByDescending(a => a.ScheduledStartTime);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(AppointmentListItemDto.MapFrom);

            return new QueryResponseDto<ListResultDto<AppointmentListItemDto>>(
                new ListResultDto<AppointmentListItemDto>(dtos, totalCount)
            );
        }
    }
}
