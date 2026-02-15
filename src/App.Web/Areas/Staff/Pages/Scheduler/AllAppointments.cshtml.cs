using System.ComponentModel.DataAnnotations;
using App.Application.Scheduler.DTOs;
using App.Application.Scheduler.Queries;
using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Shared.Models;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Filters;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Scheduler;

[ServiceFilter(typeof(SchedulerStaffAccessFilter))]
public class AllAppointmentsModel : BaseStaffPageModel
{
    public ListViewModel<AppointmentListViewModel> ListView { get; set; } =
        new(Enumerable.Empty<AppointmentListViewModel>(), 0);

    /// <summary>
    /// Staff members for filter dropdown.
    /// </summary>
    public IEnumerable<SchedulerStaffListItemDto> StaffMembers { get; set; } =
        Enumerable.Empty<SchedulerStaffListItemDto>();

    /// <summary>
    /// Appointment types for filter dropdown.
    /// </summary>
    public IEnumerable<AppointmentTypeListItemDto> AppointmentTypes { get; set; } =
        Enumerable.Empty<AppointmentTypeListItemDto>();

    public async Task<IActionResult> OnGet(
        string search = "",
        string? staffMemberId = null,
        string? appointmentTypeId = null,
        string? status = null,
        string? dateFrom = null,
        string? dateTo = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "All Appointments";
        ViewData["ActiveMenu"] = "AllAppointments";

        // Load filter dropdown data
        var staffResponse = await Mediator.Send(new GetSchedulerStaff.Query
        {
            PageSize = 500
        }, cancellationToken);
        StaffMembers = staffResponse.Result.Items;

        var typesResponse = await Mediator.Send(new GetAppointmentTypes.Query
        {
            IncludeInactive = false,
            PageSize = 500
        }, cancellationToken);
        AppointmentTypes = typesResponse.Result.Items;

        // Build query
        var query = new GetAppointments.Query
        {
            Search = search,
            StaffMemberId = !string.IsNullOrEmpty(staffMemberId) ? new ShortGuid(staffMemberId) : null,
            AppointmentTypeId = !string.IsNullOrEmpty(appointmentTypeId) ? new ShortGuid(appointmentTypeId) : null,
            Status = status,
            DateFrom = !string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df) ? df : null,
            DateTo = !string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt) ? dt.Date.AddDays(1) : null,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await Mediator.Send(query, cancellationToken);

        var items = response.Result.Items.Select(a => new AppointmentListViewModel
        {
            Id = a.Id,
            Code = a.Code,
            ContactName = a.ContactName,
            ContactId = a.ContactId,
            AssignedStaffName = a.AssignedStaffName,
            AppointmentTypeName = a.AppointmentTypeName,
            Mode = a.Mode,
            ModeLabel = a.ModeLabel,
            ScheduledStartTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(a.ScheduledStartTime),
            DurationMinutes = a.DurationMinutes,
            Status = a.Status,
            StatusLabel = a.StatusLabel
        });

        ListView = new ListViewModel<AppointmentListViewModel>(items, response.Result.TotalCount)
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Page();
    }

    public record AppointmentListViewModel
    {
        public long Id { get; init; }

        [Display(Name = "Code")]
        public string Code { get; init; } = string.Empty;

        [Display(Name = "Contact")]
        public string ContactName { get; init; } = string.Empty;

        public long ContactId { get; init; }

        [Display(Name = "Staff")]
        public string AssignedStaffName { get; init; } = string.Empty;

        [Display(Name = "Type")]
        public string AppointmentTypeName { get; init; } = string.Empty;

        public string Mode { get; init; } = string.Empty;

        [Display(Name = "Mode")]
        public string ModeLabel { get; init; } = string.Empty;

        [Display(Name = "Date & Time")]
        public string ScheduledStartTime { get; init; } = string.Empty;

        public int DurationMinutes { get; init; }

        public string Status { get; init; } = string.Empty;

        [Display(Name = "Status")]
        public string StatusLabel { get; init; } = string.Empty;
    }
}
