using App.Application.Common.Interfaces;
using App.Application.Tickets;
using App.Application.Tickets.Queries;
using App.Web.Areas.Admin.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Reports;

public class TeamReport : BaseAdminPageModel
{
    private readonly ITicketPermissionService _permissionService;

    public TeamReport(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public TeamReportDto Report { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string TeamId { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (!_permissionService.CanAccessReports())
            return Forbid();

        ViewData["Title"] = "Team Report";
        ViewData["ActiveMenu"] = "Reports";
        ViewData["ExpandTicketingMenu"] = true;

        StartDate ??= DateTime.UtcNow.AddDays(-30);
        EndDate ??= DateTime.UtcNow;

        var response = await Mediator.Send(new GetTeamReport.Query
        {
            TeamId = new ShortGuid(TeamId),
            StartDate = StartDate,
            EndDate = EndDate
        }, cancellationToken);

        Report = response.Result;
        ViewData["Title"] = $"Report: {Report.TeamName}";

        return Page();
    }
}

