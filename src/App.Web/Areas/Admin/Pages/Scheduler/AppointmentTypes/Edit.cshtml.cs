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

namespace App.Web.Areas.Admin.Pages.Scheduler.AppointmentTypes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    private readonly IAppDbContext _db;

    public Edit(IAppDbContext db)
    {
        _db = db;
    }

    public AppointmentTypeDto AppointmentType { get; set; } = null!;

    [BindProperty]
    public EditAppointmentTypeViewModel Form { get; set; } = new();

    public List<StaffOption> AllStaff { get; set; } = new();
    public List<ModeOption> Modes { get; set; } = new();

    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new GetAppointmentTypeById.Query { Id = id },
            cancellationToken);

        if (!response.Success)
        {
            SetErrorMessage(response.GetErrors());
            return RedirectToPage(RouteNames.Scheduler.AppointmentTypes.Index);
        }

        AppointmentType = response.Result;

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Appointment Types",
                RouteName = RouteNames.Scheduler.AppointmentTypes.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = AppointmentType.Name,
                RouteName = RouteNames.Scheduler.AppointmentTypes.Edit,
                IsActive = true,
                RouteValues = new Dictionary<string, string> { { "id", id } },
            }
        );

        Form = new EditAppointmentTypeViewModel
        {
            Id = AppointmentType.Id.ToString(),
            Name = AppointmentType.Name,
            Mode = AppointmentType.Mode,
            DefaultDurationMinutes = AppointmentType.DefaultDurationMinutes,
            BufferTimeMinutes = AppointmentType.BufferTimeMinutes,
            BookingHorizonDays = AppointmentType.BookingHorizonDays,
            IsActive = AppointmentType.IsActive,
            EligibleStaffIds = AppointmentType.EligibleStaff
                .Select(s => s.StaffMemberId.ToString())
                .ToList(),
        };

        await LoadFormData(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        // Update the appointment type
        var updateResponse = await Mediator.Send(new UpdateAppointmentType.Command
        {
            Id = Form.Id,
            Name = Form.Name,
            Mode = Form.Mode,
            DefaultDurationMinutes = Form.DefaultDurationMinutes,
            BufferTimeMinutes = Form.BufferTimeMinutes,
            BookingHorizonDays = Form.BookingHorizonDays,
            IsActive = Form.IsActive,
        }, cancellationToken);

        if (!updateResponse.Success)
        {
            SetErrorMessage(updateResponse.GetErrors());
            await LoadFormData(cancellationToken);
            return Page();
        }

        // Update eligible staff
        var eligibleStaffIds = (Form.EligibleStaffIds ?? new List<string>())
            .Select(id => (ShortGuid)id)
            .ToList();

        var eligibilityResponse = await Mediator.Send(new UpdateAppointmentTypeEligibility.Command
        {
            AppointmentTypeId = Form.Id,
            EligibleStaffMemberIds = eligibleStaffIds,
        }, cancellationToken);

        if (!eligibilityResponse.Success)
        {
            SetErrorMessage(eligibilityResponse.GetErrors());
            await LoadFormData(cancellationToken);
            return Page();
        }

        SetSuccessMessage("Appointment type updated successfully.");
        return RedirectToPage(RouteNames.Scheduler.AppointmentTypes.Index);
    }

    private async Task LoadFormData(CancellationToken cancellationToken)
    {
        Modes = AppointmentMode.SupportedTypes
            .Select(m => new ModeOption { Value = m.DeveloperName, Label = m.Label })
            .ToList();

        var staffEntities = await _db.SchedulerStaffMembers
            .Where(s => s.IsActive)
            .Include(s => s.User)
            .OrderBy(s => s.User.FirstName)
            .ThenBy(s => s.User.LastName)
            .Select(s => new { s.Id, FullName = s.User.FirstName + " " + s.User.LastName })
            .ToListAsync(cancellationToken);

        AllStaff = staffEntities.Select(s => new StaffOption
        {
            Id = ((ShortGuid)s.Id).ToString(),
            FullName = s.FullName,
        }).ToList();
    }

    public record EditAppointmentTypeViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Mode")]
        public string Mode { get; set; } = AppointmentMode.VIRTUAL;

        [Display(Name = "Default Duration (minutes)")]
        [Range(1, 480)]
        public int? DefaultDurationMinutes { get; set; }

        [Display(Name = "Buffer Time (minutes)")]
        [Range(0, 120)]
        public int? BufferTimeMinutes { get; set; }

        [Display(Name = "Booking Horizon (days)")]
        [Range(1, 365)]
        public int? BookingHorizonDays { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Eligible Staff")]
        public List<string>? EligibleStaffIds { get; set; } = new();
    }

    public record StaffOption
    {
        public string Id { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
    }

    public record ModeOption
    {
        public string Value { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
    }
}
