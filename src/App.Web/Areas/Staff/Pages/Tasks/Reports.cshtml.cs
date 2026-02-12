using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Application.TicketTasks.Queries;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Tasks;

public class Reports : BaseStaffPageModel
{
    private readonly ICurrentOrganization _currentOrganization;

    public Reports(ICurrentOrganization currentOrganization)
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
        ViewData["ActiveMenu"] = "Tasks";

        // Calculate timezone-aware date ranges
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
            }
        }

        // Default to last 30 days
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
