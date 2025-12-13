using App.Application.Common.Interfaces;
using App.Application.Teams.Queries;
using App.Application.Tickets;
using App.Application.Tickets.Queries;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Reports;

public class Index : BaseAdminPageModel
{
    private readonly ITicketPermissionService _permissionService;

    public Index(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public OrganizationReportDto Report { get; set; } = null!;
    public List<TeamSelectItem> Teams { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (!_permissionService.CanAccessReports())
            return Forbid();

        ViewData["Title"] = "Reports";
        ViewData["ActiveMenu"] = "Reports";
        ViewData["ExpandTicketingMenu"] = true;

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Reports",
                RouteName = RouteNames.Reports.Index,
                IsActive = true,
            }
        );

        StartDate ??= DateTime.UtcNow.AddDays(-30);
        EndDate ??= DateTime.UtcNow;

        // Ensure dates are UTC for PostgreSQL
        var startDateUtc = StartDate.HasValue
            ? DateTime.SpecifyKind(StartDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var endDateUtc = EndDate.HasValue
            ? DateTime.SpecifyKind(EndDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var response = await Mediator.Send(
            new GetOrganizationReport.Query { StartDate = startDateUtc, EndDate = endDateUtc },
            cancellationToken
        );

        Report = response.Result;

        var teamsResponse = await Mediator.Send(new GetTeams.Query(), cancellationToken);
        Teams = teamsResponse
            .Result.Items.Select(t => new TeamSelectItem { Id = t.Id, Name = t.Name })
            .ToList();

        return Page();
    }

    public record TeamSelectItem
    {
        public ShortGuid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
