using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Application.SchedulerAdmin.DTOs;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Queries;

public class GetSchedulerStaff
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<SchedulerStaffListItemDto>>>,
            ILoggableQuery
    {
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.DESCENDING}";

        public string GetLogName() => "SchedulerAdmin.Queries.GetSchedulerStaff";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<SchedulerStaffListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<
            IQueryResponseDto<ListResultDto<SchedulerStaffListItemDto>>
        > Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.SchedulerStaffMembers
                .AsNoTracking()
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(s =>
                    s.User.FirstName.ToLower().Contains(searchQuery)
                    || s.User.LastName.ToLower().Contains(searchQuery)
                    || s.User.EmailAddress.ToLower().Contains(searchQuery)
                    || (s.User.FirstName + " " + s.User.LastName)
                        .ToLower()
                        .Contains(searchQuery)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(s => SchedulerStaffListItemDto.MapFrom(s))
                .ToArray();

            return new QueryResponseDto<ListResultDto<SchedulerStaffListItemDto>>(
                new ListResultDto<SchedulerStaffListItemDto>(items, total)
            );
        }
    }
}
