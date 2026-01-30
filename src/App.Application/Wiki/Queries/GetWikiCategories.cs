using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Wiki.Queries;

public class GetWikiCategories
{
    public record Query : IRequest<IQueryResponseDto<List<WikiCategoryDto>>>
    {
        /// <summary>
        /// If true, only includes categories that have published articles.
        /// </summary>
        public bool PublishedOnly { get; init; } = true;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<List<WikiCategoryDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<List<WikiCategoryDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.WikiArticles.AsNoTracking();

            if (request.PublishedOnly)
                query = query.Where(a => a.IsPublished);

            var categories = await query
                .Where(a => a.Category != null)
                .GroupBy(a => a.Category)
                .Select(g => new WikiCategoryDto
                {
                    Name = g.Key!,
                    ArticleCount = g.Count(),
                })
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<List<WikiCategoryDto>>(categories);
        }
    }
}

public record WikiCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public int ArticleCount { get; init; }
}
