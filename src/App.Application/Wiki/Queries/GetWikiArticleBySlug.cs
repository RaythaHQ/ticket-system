using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Wiki.Queries;

public class GetWikiArticleBySlug
{
    public record Query : IRequest<IQueryResponseDto<WikiArticleDto?>>
    {
        public string Slug { get; init; } = string.Empty;

        /// <summary>
        /// If true, only returns the article if it's published.
        /// Editors can set this to false to view draft articles.
        /// </summary>
        public bool PublishedOnly { get; init; } = true;

        /// <summary>
        /// If true, increments the view count.
        /// </summary>
        public bool IncrementViewCount { get; init; } = false;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WikiArticleDto?>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<WikiArticleDto?>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.WikiArticles
                .Include(a => a.CreatorUser)
                .Include(a => a.LastModifierUser)
                .AsQueryable();

            if (request.PublishedOnly)
                query = query.Where(a => a.IsPublished);

            var article = await query.FirstOrDefaultAsync(
                a => a.Slug == request.Slug.ToLower(),
                cancellationToken
            );

            if (article == null)
                return new QueryResponseDto<WikiArticleDto?>((WikiArticleDto?)null);

            // Increment view count if requested
            if (request.IncrementViewCount)
            {
                article.ViewCount++;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new QueryResponseDto<WikiArticleDto?>(WikiArticleDto.MapFrom(article));
        }
    }
}
