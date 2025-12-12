using App.Application.Common.Interfaces;
using App.Application.Tickets;
using App.Application.Tickets.Queries;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
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

        // Ensure dates are UTC for PostgreSQL
        var startDateUtc = StartDate.HasValue
            ? DateTime.SpecifyKind(StartDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var endDateUtc = EndDate.HasValue
            ? DateTime.SpecifyKind(EndDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        // Parse TeamId - handle both ShortGuid and Guid formats
        ShortGuid teamIdShortGuid;
        if (!ShortGuid.TryParse(TeamId, out teamIdShortGuid))
        {
            // If it's a Guid, convert it to ShortGuid
            if (Guid.TryParse(TeamId, out var teamIdGuid))
            {
                teamIdShortGuid = new ShortGuid(teamIdGuid);
            }
            else
            {
                SetErrorMessage("Invalid team ID format.");
                return RedirectToPage(RouteNames.Reports.Index);
            }
        }

        var response = await Mediator.Send(
            new GetTeamReport.Query
            {
                TeamId = teamIdShortGuid,
                StartDate = startDateUtc,
                EndDate = endDateUtc,
            },
            cancellationToken
        );

        Report = response.Result;
        ViewData["Title"] = $"Report: {Report.TeamName}";

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Reports",
                RouteName = RouteNames.Reports.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = Report.TeamName,
                RouteName = RouteNames.Reports.TeamReport,
                IsActive = true,
                RouteValues = new Dictionary<string, string> { { "teamId", TeamId } },
            }
        );

        return Page();
    }
}
