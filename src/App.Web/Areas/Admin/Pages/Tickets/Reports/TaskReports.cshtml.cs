using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Application.TicketTasks.Queries;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Reports;

[Authorize(Policy = BuiltInSystemPermission.ACCESS_REPORTS_PERMISSION)]
public class TaskReports : BaseAdminPageModel
{
    private readonly ICurrentOrganization _currentOrganization;

    public TaskReports(ICurrentOrganization currentOrganization)
    {
        _currentOrganization = currentOrganization;
    }

    public TaskReportDto Report { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Preset { get; set; }

    public string? ActivePreset { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Task Reports";
        ViewData["ActiveMenu"] = "TaskReports";
        ViewData["ExpandTasksMenu"] = true;

        var timeZone = _currentOrganization.TimeZone;
        var nowUtc = DateTime.UtcNow;
        var nowLocal = nowUtc.UtcToTimeZone(timeZone);

        if (!string.IsNullOrEmpty(Preset))
        {
            ActivePreset = Preset;
            switch (Preset.ToLowerInvariant())
            {
                case "today":
                    StartDate = nowLocal.Date.TimeZoneToUtc(timeZone);
                    EndDate = nowUtc;
                    break;
                case "week":
                    var daysFromMonday = ((int)nowLocal.DayOfWeek - 1 + 7) % 7;
                    StartDate = nowLocal.Date.AddDays(-daysFromMonday).TimeZoneToUtc(timeZone);
                    EndDate = nowUtc;
                    break;
                case "month":
                    StartDate = new DateTime(nowLocal.Year, nowLocal.Month, 1).TimeZoneToUtc(timeZone);
                    EndDate = nowUtc;
                    break;
            }
        }

        StartDate ??= DateTime.UtcNow.AddDays(-30);
        EndDate ??= DateTime.UtcNow;

        var startDateUtc = StartDate.HasValue
            ? DateTime.SpecifyKind(StartDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var endDateUtc = EndDate.HasValue
            ? DateTime.SpecifyKind(EndDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var response = await Mediator.Send(
            new GetTaskReports.Query { StartDate = startDateUtc, EndDate = endDateUtc },
            cancellationToken);

        Report = response.Result;

        return Page();
    }
}
