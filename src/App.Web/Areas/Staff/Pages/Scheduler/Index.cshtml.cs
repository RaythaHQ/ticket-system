using System.ComponentModel.DataAnnotations;
using App.Application.Scheduler.DTOs;
using App.Application.Scheduler.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Filters;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Scheduler;

[ServiceFilter(typeof(SchedulerStaffAccessFilter))]
public class IndexModel : BaseStaffPageModel
{
    public StaffScheduleDto Schedule { get; set; } = new();
    public DateTime SelectedDate { get; set; } = DateTime.UtcNow.Date;
    public string SelectedDateDisplay { get; set; } = string.Empty;
    public string PreviousDateParam { get; set; } = string.Empty;
    public string NextDateParam { get; set; } = string.Empty;

    public async Task<IActionResult> OnGet(
        string? date = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "My Schedule";
        ViewData["ActiveMenu"] = "MySchedule";

        // Parse date parameter or use today in org timezone
        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
        {
            SelectedDate = parsedDate.Date;
        }
        else
        {
            SelectedDate = DateTime.UtcNow.Date;
        }

        var response = await Mediator.Send(new GetMySchedule.Query
        {
            Date = SelectedDate,
            ViewType = "day"
        }, cancellationToken);

        Schedule = response.Result;

        // Set display values
        SelectedDateDisplay = SelectedDate.ToString("dddd, MMMM d, yyyy");
        PreviousDateParam = SelectedDate.AddDays(-1).ToString("yyyy-MM-dd");
        NextDateParam = SelectedDate.AddDays(1).ToString("yyyy-MM-dd");

        return Page();
    }
}
