using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketViews.Queries;

public class GetUserFavoriteViews
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<FavoriteViewDto>>>;

    public record FavoriteViewDto
    {
        public ShortGuid ViewId { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public int DisplayOrder { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<FavoriteViewDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<FavoriteViewDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUser.UserId!.Value.Guid;

            var favorites = await _db.UserFavoriteViews
                .AsNoTracking()
                .Include(f => f.TicketView)
                .Where(f => f.UserId == userId)
                .OrderBy(f => f.DisplayOrder)
                .Select(f => new FavoriteViewDto
                {
                    ViewId = f.TicketViewId,
                    Name = f.TicketView.Name,
                    Description = f.TicketView.Description,
                    DisplayOrder = f.DisplayOrder
                })
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<IEnumerable<FavoriteViewDto>>(favorites);
        }
    }
}

