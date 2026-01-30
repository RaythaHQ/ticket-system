using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Wiki.Queries;

public class GetWikiArticleById
{
    public record Query : IRequest<IQueryResponseDto<WikiArticleDto?>>
    {
        public Guid Id { get; init; }
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
            var article = await _db.WikiArticles
                .Include(a => a.CreatorUser)
                .Include(a => a.LastModifierUser)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (article == null)
                return new QueryResponseDto<WikiArticleDto?>((WikiArticleDto?)null);

            return new QueryResponseDto<WikiArticleDto?>(WikiArticleDto.MapFrom(article));
        }
    }
}
