using App.Application.TicketViews.Queries;

namespace App.Application.TicketViews;

/// <summary>
/// Service for retrieving favorite views, optimized for injection into layout views.
/// </summary>
public interface IFavoriteViewsService
{
    /// <summary>
    /// Gets the current user's favorite views.
    /// </summary>
    Task<IEnumerable<GetUserFavoriteViews.FavoriteViewDto>> GetUserFavoriteViewsAsync(CancellationToken cancellationToken = default);
}

