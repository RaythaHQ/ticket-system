using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Teams.Queries;

public class GetTeams
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<TeamDto>>>
    {
        public override string OrderBy { get; init; } = $"Name {SortOrder.ASCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<TeamDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<TeamDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.Teams
                .AsNoTracking()
                .Include(t => t.Memberships)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(searchQuery)
                    || (t.Description != null && t.Description.ToLower().Contains(searchQuery))
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(t => TeamDto.MapFrom(t))
                .ToArray();

            return new QueryResponseDto<ListResultDto<TeamDto>>(
                new ListResultDto<TeamDto>(items, total)
            );
        }
    }
}

