using App.Application.Common.Interfaces;
using App.Application.TicketViews;
using App.Application.TicketViews.Queries;
using CSharpVitamins;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

public class FavoriteViewsService : IFavoriteViewsService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public FavoriteViewsService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<GetUserFavoriteViews.FavoriteViewDto>> GetUserFavoriteViewsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUser.UserIdAsGuid;
        if (!userId.HasValue)
        {
            return Enumerable.Empty<GetUserFavoriteViews.FavoriteViewDto>();
        }

        var favorites = await _db
            .UserFavoriteViews.AsNoTracking()
            .Include(f => f.TicketView)
            .Where(f => f.UserId == userId.Value)
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new GetUserFavoriteViews.FavoriteViewDto
            {
                ViewId = f.TicketViewId,
                Name = f.TicketView.Name,
                Description = f.TicketView.Description,
                DisplayOrder = f.DisplayOrder,
            })
            .ToListAsync(cancellationToken);

        return favorites;
    }
}
