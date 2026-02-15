using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.SchedulerAdmin.Commands;
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
public class Create : BaseAdminPageModel
{
    private readonly IAppDbContext _db;

    public Create(IAppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public CreateAppointmentTypeViewModel Form { get; set; } = new();

    public List<StaffOption> AllStaff { get; set; } = new();
    public List<ModeOption> Modes { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Appointment Types",
                RouteName = RouteNames.Scheduler.AppointmentTypes.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create",
                RouteName = RouteNames.Scheduler.AppointmentTypes.Create,
                IsActive = true,
            }
        );

        await LoadFormData(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        var eligibleStaffIds = (Form.EligibleStaffIds ?? new List<string>())
            .Select(id => (ShortGuid)id)
            .ToList();

        var response = await Mediator.Send(new CreateAppointmentType.Command
        {
            Name = Form.Name,
            Mode = Form.Mode,
            DefaultDurationMinutes = Form.DefaultDurationMinutes,
            BufferTimeMinutes = Form.BufferTimeMinutes,
            BookingHorizonDays = Form.BookingHorizonDays,
            EligibleStaffIds = eligibleStaffIds,
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Appointment type created successfully.");
            return RedirectToPage(RouteNames.Scheduler.AppointmentTypes.Index);
        }

        SetErrorMessage(response.GetErrors());
        await LoadFormData(cancellationToken);
        return Page();
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

    public record CreateAppointmentTypeViewModel
    {
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
