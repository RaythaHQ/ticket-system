using App.Application.Scheduler.DTOs;
using App.Application.Scheduler.Queries;
using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Filters;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Scheduler;

[ServiceFilter(typeof(SchedulerStaffAccessFilter))]
public class StaffScheduleModel : BaseStaffPageModel
{
    public StaffScheduleResourceDto Schedule { get; set; } = new();
    public IEnumerable<SchedulerStaffListItemDto> AllStaffMembers { get; set; } = Enumerable.Empty<SchedulerStaffListItemDto>();
    public IEnumerable<AppointmentTypeListItemDto> AllAppointmentTypes { get; set; } = Enumerable.Empty<AppointmentTypeListItemDto>();

    public DateTime SelectedDate { get; set; }
    public string ViewType { get; set; } = "day";
    public string SelectedDateDisplay { get; set; } = string.Empty;
    public string PreviousDateParam { get; set; } = string.Empty;
    public string NextDateParam { get; set; } = string.Empty;
    public string TodayParam { get; set; } = string.Empty;
    public List<string> SelectedStaffIds { get; set; } = new();
    public string? SelectedAppointmentTypeId { get; set; }

    public int StartHour { get; set; } = 7;
    public int EndHour { get; set; } = 19;

    public List<DateTime> DayColumns { get; set; } = new();

    public async Task<IActionResult> OnGet(
        string? date = null,
        string? viewType = null,
        string? staffIds = null,
        string? appointmentTypeId = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Staff Schedule";
        ViewData["ActiveMenu"] = "StaffSchedule";

        ViewType = viewType ?? "day";

        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            SelectedDate = parsedDate.Date;
        else
            SelectedDate = DateTime.UtcNow.Date;

        SelectedAppointmentTypeId = appointmentTypeId;

        // Parse selected staff IDs
        if (!string.IsNullOrEmpty(staffIds))
        {
            SelectedStaffIds = staffIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        // Load filter dropdown data
        var staffResponse = await Mediator.Send(new GetSchedulerStaff.Query { PageSize = 500 }, cancellationToken);
        AllStaffMembers = staffResponse.Result.Items;

        var typesResponse = await Mediator.Send(new GetAppointmentTypes.Query { IncludeInactive = false, PageSize = 500 }, cancellationToken);
        AllAppointmentTypes = typesResponse.Result.Items;

        // Determine date range
        DateTime dateFrom, dateTo;
        if (ViewType == "week")
        {
            var dayOfWeek = SelectedDate.DayOfWeek;
            var daysToMonday = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
            dateFrom = SelectedDate.AddDays(-daysToMonday);
            dateTo = dateFrom.AddDays(7);
            DayColumns = Enumerable.Range(0, 7).Select(i => dateFrom.AddDays(i)).ToList();
        }
        else
        {
            dateFrom = SelectedDate;
            dateTo = SelectedDate.AddDays(1);
            DayColumns = new List<DateTime> { SelectedDate };
        }

        // Build staff IDs for query
        var queryStaffIds = SelectedStaffIds
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => new ShortGuid(s))
            .ToList();

        ShortGuid? typeFilter = null;
        if (!string.IsNullOrEmpty(SelectedAppointmentTypeId))
            typeFilter = new ShortGuid(SelectedAppointmentTypeId);

        var scheduleResponse = await Mediator.Send(new GetStaffSchedule.Query
        {
            StaffMemberIds = queryStaffIds,
            AppointmentTypeId = typeFilter,
            DateFrom = dateFrom,
            DateTo = dateTo
        }, cancellationToken);

        Schedule = scheduleResponse.Result;

        // Date navigation
        var navDays = ViewType == "week" ? 7 : 1;
        PreviousDateParam = SelectedDate.AddDays(-navDays).ToString("yyyy-MM-dd");
        NextDateParam = SelectedDate.AddDays(navDays).ToString("yyyy-MM-dd");
        TodayParam = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        if (ViewType == "week")
        {
            SelectedDateDisplay = $"{dateFrom:MMM d} – {dateTo.AddDays(-1):MMM d, yyyy}";
        }
        else
        {
            SelectedDateDisplay = SelectedDate.ToString("dddd, MMMM d, yyyy");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateBlockOut(
        string staffMemberId,
        string title,
        DateTime startTime,
        DateTime endTime,
        bool isAllDay,
        string? reason,
        string? returnDate,
        string? returnViewType,
        string? returnStaffIds,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new App.Application.Scheduler.Commands.CreateBlockOutTime.Command
        {
            StaffMemberId = new ShortGuid(staffMemberId),
            Title = title,
            StartTimeUtc = DateTime.SpecifyKind(startTime, DateTimeKind.Utc),
            EndTimeUtc = DateTime.SpecifyKind(endTime, DateTimeKind.Utc),
            IsAllDay = isAllDay,
            Reason = reason
        }, cancellationToken);

        SetSuccessMessage("Block-out time created.");
        return RedirectToPage(new { date = returnDate, viewType = returnViewType, staffIds = returnStaffIds });
    }

    public async Task<IActionResult> OnPostDeleteBlockOut(
        string blockOutId,
        string? returnDate,
        string? returnViewType,
        string? returnStaffIds,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new App.Application.Scheduler.Commands.DeleteBlockOutTime.Command
        {
            Id = new ShortGuid(blockOutId)
        }, cancellationToken);

        SetSuccessMessage("Block-out time deleted.");
        return RedirectToPage(new { date = returnDate, viewType = returnViewType, staffIds = returnStaffIds });
    }
}
