using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Scheduler.Reports;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Index : BaseAdminPageModel
{
    private readonly ICurrentOrganization _currentOrganization;

    public Index(ICurrentOrganization currentOrganization)
    {
        _currentOrganization = currentOrganization;
    }

    public SchedulerReportDto Report { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Preset { get; set; }

    /// <summary>
    /// Expose current preset for UI button highlighting.
    /// </summary>
    public string? ActivePreset { get; set; }

    /// <summary>
    /// Formatted period label shown below the filter.
    /// </summary>
    public string PeriodLabel { get; set; } = string.Empty;

    // Convenience properties for the Razor view
    public int TotalAppointments => Report.AppointmentsByStatus.Values.Sum();
    public int ScheduledCount =>
        Report.AppointmentsByStatus.GetValueOrDefault(AppointmentStatus.SCHEDULED, 0)
        + Report.AppointmentsByStatus.GetValueOrDefault(AppointmentStatus.CONFIRMED, 0);
    public int CompletedCount =>
        Report.AppointmentsByStatus.GetValueOrDefault(AppointmentStatus.COMPLETED, 0);
    public int CancelledCount =>
        Report.AppointmentsByStatus.GetValueOrDefault(AppointmentStatus.CANCELLED, 0);
    public int NoShowCount =>
        Report.AppointmentsByStatus.GetValueOrDefault(AppointmentStatus.NO_SHOW, 0);

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Scheduler Reports",
                RouteName = RouteNames.Scheduler.Reports.Index,
                IsActive = true,
            }
        );

        // Calculate timezone-aware date ranges (matching Ticketing Reports pattern)
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

                case "quarter":
                    var quarterStartMonth = ((nowLocal.Month - 1) / 3) * 3 + 1;
                    var startOfQuarterLocal = new DateTime(nowLocal.Year, quarterStartMonth, 1);
                    StartDate = startOfQuarterLocal.TimeZoneToUtc(timeZone);
                    EndDate = nowUtc;
                    break;

                case "year":
                    var startOfYearLocal = new DateTime(nowLocal.Year, 1, 1);
                    StartDate = startOfYearLocal.TimeZoneToUtc(timeZone);
                    EndDate = nowUtc;
                    break;
            }
        }

        // Default to last 30 days if no dates specified
        StartDate ??= DateTime.UtcNow.AddDays(-30);
        EndDate ??= DateTime.UtcNow;

        // Ensure dates are UTC for PostgreSQL
        var startDateUtc = DateTime.SpecifyKind(StartDate.Value, DateTimeKind.Utc);
        var endDateUtc = DateTime.SpecifyKind(EndDate.Value, DateTimeKind.Utc);

        // Build the period label for display
        var startLocal = startDateUtc.UtcToTimeZone(timeZone);
        var endLocal = endDateUtc.UtcToTimeZone(timeZone);
        PeriodLabel = $"{startLocal:MMM dd, yyyy} — {endLocal:MMM dd, yyyy}";

        var response = await Mediator.Send(
            new GetSchedulerReports.Query
            {
                DateFrom = startDateUtc,
                DateTo = endDateUtc,
            },
            cancellationToken
        );

        if (response.Success)
        {
            Report = response.Result;
        }

        return Page();
    }
}
