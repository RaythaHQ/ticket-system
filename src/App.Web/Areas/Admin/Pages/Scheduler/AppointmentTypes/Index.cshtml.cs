using System.ComponentModel.DataAnnotations;
using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Scheduler.AppointmentTypes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.AppointmentTypeListItemViewModel>
{
    public ListViewModel<AppointmentTypeListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<AppointmentTypeListItemViewModel>(), 0);

    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"SortOrder {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Appointment Types",
                RouteName = RouteNames.Scheduler.AppointmentTypes.Index,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetAppointmentTypes.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
            IncludeInactive = IncludeInactive,
        }, cancellationToken);

        var items = response.Result.Items.Select(t => new AppointmentTypeListItemViewModel
        {
            Id = t.Id.ToString(),
            Name = t.Name,
            ModeLabel = t.ModeLabel,
            DefaultDurationMinutes = t.DefaultDurationMinutes?.ToString() ?? "Default",
            EligibleStaffCount = t.EligibleStaffCount,
            IsActive = t.IsActive,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(t.CreationTime),
        });

        ListView = new ListViewModel<AppointmentTypeListItemViewModel>(items, response.Result.TotalCount);

        return Page();
    }

    public record AppointmentTypeListItemViewModel
    {
        public string Id { get; init; } = string.Empty;

        [Display(Name = "Name")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Mode")]
        public string ModeLabel { get; init; } = string.Empty;

        [Display(Name = "Duration")]
        public string DefaultDurationMinutes { get; init; } = string.Empty;

        [Display(Name = "Eligible Staff")]
        public int EligibleStaffCount { get; init; }

        [Display(Name = "Active")]
        public bool IsActive { get; init; }

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;
    }
}
