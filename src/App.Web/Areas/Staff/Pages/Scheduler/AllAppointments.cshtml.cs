using System.ComponentModel.DataAnnotations;
using App.Application.Scheduler.Queries;
using App.Application.Common.Interfaces;
using App.Application.Scheduler.DTOs;
using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Shared.Models;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Filters;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Areas.Staff.Pages.Scheduler;

[ServiceFilter(typeof(SchedulerStaffAccessFilter))]
public class AllAppointmentsModel : BaseStaffPageModel, IHasListView<AllAppointmentsModel.AppointmentListViewModel>
{
    public ListViewModel<AppointmentListViewModel> ListView { get; set; } =
        new(Enumerable.Empty<AppointmentListViewModel>(), 0);

    public IEnumerable<SchedulerStaffListItemDto> StaffMembers { get; set; } =
        Enumerable.Empty<SchedulerStaffListItemDto>();

    public IEnumerable<AppointmentTypeListItemDto> AppointmentTypes { get; set; } =
        Enumerable.Empty<AppointmentTypeListItemDto>();

    public string? BuiltInView { get; set; }
    public string ViewTitle { get; set; } = "All Appointments";

    public async Task<IActionResult> OnGet(
        string? builtInView = null,
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
        BuiltInView = builtInView;

        // Set title and active menu based on built-in view
        switch (builtInView?.ToLower())
        {
            case "my-appointments":
                ViewTitle = "My Appointments";
                ViewData["ActiveMenu"] = "MyAppointments";
                break;
            case "today":
                ViewTitle = "Today's Appointments";
                ViewData["ActiveMenu"] = "TodayAppointments";
                break;
            case "upcoming":
                ViewTitle = "Upcoming Appointments";
                ViewData["ActiveMenu"] = "UpcomingAppointments";
                break;
            case "past":
                ViewTitle = "Past Appointments";
                ViewData["ActiveMenu"] = "PastAppointments";
                break;
            case "all":
            default:
                ViewTitle = "All Appointments";
                ViewData["ActiveMenu"] = "AllAppointments";
                break;
        }

        ViewData["Title"] = ViewTitle;

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

        // Build query with built-in view presets as base
        var query = new GetAppointments.Query
        {
            Search = search,
            StaffMemberId = !string.IsNullOrEmpty(staffMemberId) ? new ShortGuid(staffMemberId) : null,
            AppointmentTypeId = !string.IsNullOrEmpty(appointmentTypeId) ? new ShortGuid(appointmentTypeId) : null,
            Status = status,
            DateFrom = !string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df) ? df : null,
            DateTo = !string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt) ? dt.Date.AddDays(1) : null,
            PageNumber = pageNumber,
            PageSize = pageSize,
            // Apply built-in presets
            OnlyMine = builtInView?.ToLower() == "my-appointments",
            DatePreset = builtInView?.ToLower() switch
            {
                "today" => "today",
                "upcoming" => "upcoming",
                "past" => "past",
                _ => null
            },
            ExcludeTerminalStatuses = builtInView?.ToLower() is "my-appointments" or "upcoming",
            OrderBy = builtInView?.ToLower() switch
            {
                "upcoming" or "today" or "my-appointments" => $"ScheduledStartTime {SortOrder.ASCENDING}",
                _ => $"ScheduledStartTime {SortOrder.DESCENDING}"
            }
        };

        var response = await Mediator.Send(query, cancellationToken);

        // Safety fallback: if "all" view unexpectedly returns empty, query directly.
        // CQRS path above remains the primary implementation.
        if ((builtInView?.ToLower() is null or "all") && response.Result.TotalCount == 0)
        {
            var db = HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
            var fallbackQuery = db
                .Appointments.AsNoTracking()
                .Include(a => a.Contact)
                .Include(a => a.AssignedStaffMember)
                .ThenInclude(s => s.User)
                .Include(a => a.AppointmentType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(staffMemberId))
            {
                var staffGuid = new ShortGuid(staffMemberId).Guid;
                fallbackQuery = fallbackQuery.Where(a => a.AssignedStaffMemberId == staffGuid);
            }

            if (!string.IsNullOrWhiteSpace(appointmentTypeId))
            {
                var typeGuid = new ShortGuid(appointmentTypeId).Guid;
                fallbackQuery = fallbackQuery.Where(a => a.AppointmentTypeId == typeGuid);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                fallbackQuery = fallbackQuery.Where(a => a.Status == status.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var parsedFrom))
            {
                var fromUtc = DateTime.SpecifyKind(parsedFrom, DateTimeKind.Utc);
                fallbackQuery = fallbackQuery.Where(a => a.ScheduledStartTime >= fromUtc);
            }

            if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var parsedTo))
            {
                var toUtc = DateTime.SpecifyKind(parsedTo.Date.AddDays(1), DateTimeKind.Utc);
                fallbackQuery = fallbackQuery.Where(a => a.ScheduledStartTime < toUtc);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                long? searchId = null;
                if (searchLower.StartsWith("apt-") && long.TryParse(searchLower[4..], out var parsedId))
                    searchId = parsedId;
                else if (long.TryParse(search, out var numericId))
                    searchId = numericId;

                fallbackQuery = fallbackQuery.Where(a =>
                    (searchId.HasValue && a.Id == searchId.Value)
                    || (
                        a.Contact != null
                        && (
                            a.Contact.FirstName.ToLower().Contains(searchLower)
                            || (a.Contact.LastName != null && a.Contact.LastName.ToLower().Contains(searchLower))
                        )
                    )
                );
            }

            fallbackQuery = fallbackQuery.OrderByDescending(a => a.ScheduledStartTime);
            var fallbackTotal = await fallbackQuery.CountAsync(cancellationToken);
            var fallbackItems = await fallbackQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            response = new App.Application.Common.Models.QueryResponseDto<
                App.Application.Common.Models.ListResultDto<AppointmentListItemDto>
            >(
                new App.Application.Common.Models.ListResultDto<AppointmentListItemDto>(
                    fallbackItems.Select(AppointmentListItemDto.MapFrom),
                    fallbackTotal
                )
            );
        }

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
            PageSize = pageSize,
            BuiltInView = builtInView
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
