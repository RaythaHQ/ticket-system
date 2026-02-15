using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
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
    private readonly ICurrentOrganization _currentOrganization;

    public TeamReport(ITicketPermissionService permissionService, ICurrentOrganization currentOrganization)
    {
        _permissionService = permissionService;
        _currentOrganization = currentOrganization;
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

        // Convert dates to UTC properly:
        // - If the date already has a time component (came from a full ISO 8601 link), treat as UTC
        // - If date-only (midnight, from a date picker form), convert from org timezone to UTC
        var timeZone = _currentOrganization.TimeZone;
        var startDateUtc = ToUtc(StartDate, timeZone);
        var endDateUtc = ToUtc(EndDate, timeZone);

        // Store back the UTC values so links/forms use the correct dates
        StartDate = startDateUtc;
        EndDate = endDateUtc;

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

    public async Task<IActionResult> OnGetExportCsv(CancellationToken cancellationToken)
    {
        if (!_permissionService.CanAccessReports())
            return Forbid();

        StartDate ??= DateTime.UtcNow.AddDays(-30);
        EndDate ??= DateTime.UtcNow;

        var timeZoneExport = _currentOrganization.TimeZone;
        var startDateUtc = ToUtc(StartDate, timeZoneExport);
        var endDateUtc = ToUtc(EndDate, timeZoneExport);

        ShortGuid teamIdShortGuid;
        if (!ShortGuid.TryParse(TeamId, out teamIdShortGuid))
        {
            if (Guid.TryParse(TeamId, out var teamIdGuid))
            {
                teamIdShortGuid = new ShortGuid(teamIdGuid);
            }
            else
            {
                return BadRequest("Invalid team ID format.");
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

        var report = response.Result;
        var csv = new CsvWriterUtility();

        foreach (var member in report.MemberMetrics.OrderByDescending(m => m.ResolvedTickets))
        {
            csv.AddRow(new Dictionary<string, string>
            {
                ["Member"] = member.UserName,
                ["Assigned"] = member.AssignedTickets.ToString(),
                ["Resolved"] = member.ResolvedTickets.ToString(),
                ["Median Resolution Time (hours)"] = member.MedianResolutionTimeHours.HasValue
                    ? $"{member.MedianResolutionTimeHours:F1}"
                    : "",
            });
        }

        var bytes = csv.ExportToBytes();
        var fileName = $"{report.TeamName}-member-performance-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// Converts a DateTime to UTC properly.
    /// If the value already has Kind=Utc (from a full ISO 8601 string), keeps it as-is.
    /// If the value is Unspecified (from a date-only form input), treats it as the org timezone and converts.
    /// </summary>
    private static DateTime? ToUtc(DateTime? value, string timeZone)
    {
        if (!value.HasValue) return null;

        var dt = value.Value;
        if (dt.Kind == DateTimeKind.Utc) return dt;

        // Date-only input (midnight, no time component) — treat as local timezone
        if (dt.TimeOfDay == TimeSpan.Zero)
            return dt.TimeZoneToUtc(timeZone);

        // Otherwise just mark as UTC
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }
}
