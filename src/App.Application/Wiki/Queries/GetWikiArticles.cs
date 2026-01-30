using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Wiki.Queries;

public class GetWikiArticles
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WikiArticleListItemDto>>>
    {
        public override string OrderBy { get; init; } =
            $"IsPinned {SortOrder.DESCENDING}, SortOrder {SortOrder.ASCENDING}, Title {SortOrder.ASCENDING}";

        /// <summary>
        /// Filter by category. If null, returns all categories.
        /// </summary>
        public string? Category { get; init; }

        /// <summary>
        /// If true, only returns published articles (for regular staff).
        /// If false or null, returns all articles (for editors).
        /// </summary>
        public bool? PublishedOnly { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WikiArticleListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WikiArticleListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.WikiArticles
                .AsNoTracking()
                .Include(a => a.CreatorUser)
                .AsQueryable();

            // Filter by published status
            if (request.PublishedOnly == true)
                query = query.Where(a => a.IsPublished);

            // Filter by category
            if (!string.IsNullOrEmpty(request.Category))
                query = query.Where(a => a.Category == request.Category);

            // Search
            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(searchQuery)
                    || (a.Excerpt != null && a.Excerpt.ToLower().Contains(searchQuery))
                    || a.Content.ToLower().Contains(searchQuery)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(a => WikiArticleListItemDto.MapFrom(a))
                .ToArray();

            return new QueryResponseDto<ListResultDto<WikiArticleListItemDto>>(
                new ListResultDto<WikiArticleListItemDto>(items, total)
            );
        }
    }
}
