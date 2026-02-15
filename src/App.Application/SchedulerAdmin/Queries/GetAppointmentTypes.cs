using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Application.SchedulerAdmin.DTOs;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Queries;

public class GetAppointmentTypes
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<AppointmentTypeListItemDto>>>
    {
        public bool IncludeInactive { get; init; }
        public override string OrderBy { get; init; } = $"SortOrder {SortOrder.ASCENDING}";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<AppointmentTypeListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<AppointmentTypeListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var query = _db.AppointmentTypes
                .AsNoTracking()
                .Include(t => t.EligibleStaff)
                .AsQueryable();

            if (!request.IncludeInactive)
            {
                query = query.Where(t => t.IsActive);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(searchQuery));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(t => AppointmentTypeListItemDto.MapFrom(t))
                .ToArray();

            return new QueryResponseDto<ListResultDto<AppointmentTypeListItemDto>>(
                new ListResultDto<AppointmentTypeListItemDto>(items, total));
        }
    }
}
