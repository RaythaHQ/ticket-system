using App.Application.TicketViews;
using App.Application.TicketViews.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Views;

public class Index : BaseStaffPageModel
{
    public IEnumerable<TicketViewDto> SystemViews { get; set; } = Enumerable.Empty<TicketViewDto>();
    public IEnumerable<TicketViewDto> MyViews { get; set; } = Enumerable.Empty<TicketViewDto>();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Manage Views";
        ViewData["ActiveMenu"] = "Views";

        var userId = CurrentUser.UserId?.Guid;
        if (!userId.HasValue)
            return RedirectToPage(RouteNames.Error.Index);

        var response = await Mediator.Send(new GetTicketViews.Query(), cancellationToken);
        
        var allViews = response.Result;
        SystemViews = allViews.Where(v => v.IsSystemView);
        MyViews = allViews.Where(v => !v.IsSystemView && v.OwnerUserId == userId.Value);

        return Page();
    }

    public async Task<IActionResult> OnPostDelete(Guid viewId, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new App.Application.TicketViews.Commands.DeleteTicketView.Command
        {
            Id = viewId
        }, cancellationToken);

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
}

