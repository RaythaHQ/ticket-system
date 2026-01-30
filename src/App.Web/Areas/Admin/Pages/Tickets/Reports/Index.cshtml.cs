using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
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
    private readonly ICurrentOrganization _currentOrganization;

    public Index(ITicketPermissionService permissionService, ICurrentOrganization currentOrganization)
    {
        _permissionService = permissionService;
        _currentOrganization = currentOrganization;
    }

    public OrganizationReportDto Report { get; set; } = null!;
    public List<TeamSelectItem> Teams { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Preset { get; set; }

    // Expose current preset for UI highlighting
    public string? ActivePreset { get; set; }

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

        // Calculate timezone-aware date ranges (matching staff dashboard logic)
        var timeZone = _currentOrganization.TimeZone;
        var nowUtc = DateTime.UtcNow;
        var nowLocal = nowUtc.UtcToTimeZone(timeZone);

        // Handle preset shortcuts
        if (!string.IsNullOrEmpty(Preset))
        {
            ActivePreset = Preset;
            switch (Preset.ToLowerInvariant())
            {
                case "today":
                    var startOfTodayLocal = nowLocal.Date;
                    StartDate = startOfTodayLocal.TimeZoneToUtc(timeZone);
                    EndDate = nowUtc;
                    break;

                case "week":
                    var daysFromMonday = ((int)nowLocal.DayOfWeek - 1 + 7) % 7;
                    var startOfWeekLocal = nowLocal.Date.AddDays(-daysFromMonday);
                    StartDate = startOfWeekLocal.TimeZoneToUtc(timeZone);
                    EndDate = nowUtc;
                    break;

                case "month":
                    var startOfMonthLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1);
                    StartDate = startOfMonthLocal.TimeZoneToUtc(timeZone);
                    EndDate = nowUtc;
                    break;

                default:
                    // Unknown preset, fall through to default
                    break;
            }
        }

        // Default to last 30 days if no dates specified
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
