using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.SchedulerAdmin.Commands;
using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Areas.Admin.Pages.Scheduler.Staff;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.StaffListItemViewModel>
{
    private readonly IAppDbContext _db;

    public Index(IAppDbContext db)
    {
        _db = db;
    }

    public ListViewModel<StaffListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<StaffListItemViewModel>(), 0);

    public List<AdminOption> AvailableAdmins { get; set; } = new();

    [BindProperty]
    public string? SelectedUserId { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Scheduler Staff",
                RouteName = RouteNames.Scheduler.Staff.Index,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetSchedulerStaff.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        var items = response.Result.Items.Select(s => new StaffListItemViewModel
        {
            Id = s.Id.ToString(),
            FullName = s.FullName,
            Email = s.Email,
            CanManageOthersCalendars = s.CanManageOthersCalendars ? "Yes" : "No",
            CoverageZonesCount = s.CoverageZonesCount,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(s.CreationTime),
        });

        ListView = new ListViewModel<StaffListItemViewModel>(items, response.Result.TotalCount);

        // Load active admins who are NOT already scheduler staff
        var existingStaffUserIds = await _db.SchedulerStaffMembers
            .Select(s => s.UserId)
            .ToListAsync(cancellationToken);

        AvailableAdmins = await _db.Users
            .Where(u => u.IsAdmin && u.IsActive && !existingStaffUserIds.Contains(u.Id))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new AdminOption
            {
                UserId = u.Id.ToString(),
                FullName = u.FirstName + " " + u.LastName,
                Email = u.EmailAddress,
            })
            .ToListAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAddStaff(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(SelectedUserId))
        {
            SetErrorMessage("Please select a user to add.");
            return RedirectToPage(RouteNames.Scheduler.Staff.Index);
        }

        var response = await Mediator.Send(new AddSchedulerStaff.Command
        {
            UserId = SelectedUserId,
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Staff member added successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Scheduler.Staff.Index);
    }

    public async Task<IActionResult> OnPostRemoveStaff(string staffId, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new RemoveSchedulerStaff.Command
        {
            SchedulerStaffMemberId = staffId,
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Staff member removed successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Scheduler.Staff.Index);
    }

    public record StaffListItemViewModel
    {
        public string Id { get; init; } = string.Empty;

        [Display(Name = "Name")]
        public string FullName { get; init; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; init; } = string.Empty;

        [Display(Name = "Can Manage Others")]
        public string CanManageOthersCalendars { get; init; } = string.Empty;

        [Display(Name = "Coverage Zones")]
        public int CoverageZonesCount { get; init; }

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;
    }

    public record AdminOption
    {
        public string UserId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
