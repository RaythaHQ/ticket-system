using App.Application.Common.Interfaces;
using App.Application.Tickets;
using App.Application.Tickets.Queries;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Dashboard;

public class Index : BaseStaffPageModel
{
    public UserDashboardMetricsDto Metrics { get; set; } = null!;
    public Guid? UserId { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";
        ViewData["ActiveMenu"] = "Dashboard";

        UserId = CurrentUser.UserId?.Guid;
        if (!UserId.HasValue)
            return RedirectToPage("/Error");

        var response = await Mediator.Send(new GetUserDashboardMetrics.Query { UserId = UserId.Value }, cancellationToken);
        Metrics = response.Result;

        return Page();
    }
}

