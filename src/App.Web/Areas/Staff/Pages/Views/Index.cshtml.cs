using App.Application.TicketViews;
using App.Application.TicketViews.Commands;
using App.Application.TicketViews.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Views;

public class Index : BaseStaffPageModel
{
    public IEnumerable<ViewWithFavorite> SystemViews { get; set; } = Enumerable.Empty<ViewWithFavorite>();
    public IEnumerable<ViewWithFavorite> MyViews { get; set; } = Enumerable.Empty<ViewWithFavorite>();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Manage Views";
        ViewData["ActiveMenu"] = "Views";

        var userId = CurrentUser.UserId?.Guid;
        if (!userId.HasValue)
            return RedirectToPage(RouteNames.Error.Index);

        var response = await Mediator.Send(new GetTicketViews.Query(), cancellationToken);
        var favoritesResponse = await Mediator.Send(new GetUserFavoriteViews.Query(), cancellationToken);
        
        var favoriteViewIds = new HashSet<Guid>(favoritesResponse.Result.Select(f => f.ViewId.Guid));

        var allViews = response.Result;
        SystemViews = allViews
            .Where(v => v.IsSystemView)
            .Select(v => new ViewWithFavorite(v, favoriteViewIds.Contains(v.Id.Guid)));
        MyViews = allViews
            .Where(v => !v.IsSystemView && v.OwnerStaffId?.Guid == userId.Value)
            .Select(v => new ViewWithFavorite(v, favoriteViewIds.Contains(v.Id.Guid)));

        return Page();
    }

    public async Task<IActionResult> OnPostDelete(Guid viewId, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new App.Application.TicketViews.Commands.DeleteTicketView.Command { Id = viewId },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage("View deleted successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage();
    }
    
    public async Task<IActionResult> OnPostToggleFavorite(string viewId, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new ToggleFavoriteView.Command { ViewId = new ShortGuid(viewId) },
            cancellationToken
        );

        if (response.Success)
        {
            var isFavorited = response.Result;
            SetSuccessMessage(isFavorited ? "View added to favorites." : "View removed from favorites.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage();
    }
    
    public record ViewWithFavorite(TicketViewDto View, bool IsFavorited);
}
