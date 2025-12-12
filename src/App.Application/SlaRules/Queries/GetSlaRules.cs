using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SlaRules.Queries;

public class GetSlaRules
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<SlaRuleDto>>>
    {
        public override string OrderBy { get; init; } = $"Priority {SortOrder.ASCENDING}";
        public bool? ActiveOnly { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<SlaRuleDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<SlaRuleDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.SlaRules.AsNoTracking().AsQueryable();

            if (request.ActiveOnly == true)
                query = query.Where(r => r.IsActive);

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(r =>
                    r.Name.ToLower().Contains(searchQuery)
                    || (r.Description != null && r.Description.ToLower().Contains(searchQuery))
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(r => SlaRuleDto.MapFrom(r))
                .ToArray();

            return new QueryResponseDto<ListResultDto<SlaRuleDto>>(
                new ListResultDto<SlaRuleDto>(items, total)
            );
        }
    }
}

