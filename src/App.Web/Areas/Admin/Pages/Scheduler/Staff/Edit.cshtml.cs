using System.ComponentModel.DataAnnotations;
using App.Application.SchedulerAdmin.Commands;
using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Scheduler.Staff;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    public SchedulerStaffDto StaffMember { get; set; } = null!;

    [BindProperty]
    public EditStaffViewModel Form { get; set; } = new();

    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new GetSchedulerStaffById.Query { Id = id },
            cancellationToken);

        StaffMember = response.Result;

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Scheduler Staff",
                RouteName = RouteNames.Scheduler.Staff.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = StaffMember.FullName,
                RouteName = RouteNames.Scheduler.Staff.Edit,
                IsActive = true,
                RouteValues = new Dictionary<string, string> { { "id", id } },
            }
        );

        Form = new EditStaffViewModel
        {
            Id = StaffMember.Id.ToString(),
            CanManageOthersCalendars = StaffMember.CanManageOthersCalendars,
            DefaultMeetingLink = StaffMember.DefaultMeetingLink,
            MondayStart = StaffMember.Availability.GetValueOrDefault("monday")?.Start ?? "",
            MondayEnd = StaffMember.Availability.GetValueOrDefault("monday")?.End ?? "",
            TuesdayStart = StaffMember.Availability.GetValueOrDefault("tuesday")?.Start ?? "",
            TuesdayEnd = StaffMember.Availability.GetValueOrDefault("tuesday")?.End ?? "",
            WednesdayStart = StaffMember.Availability.GetValueOrDefault("wednesday")?.Start ?? "",
            WednesdayEnd = StaffMember.Availability.GetValueOrDefault("wednesday")?.End ?? "",
            ThursdayStart = StaffMember.Availability.GetValueOrDefault("thursday")?.Start ?? "",
            ThursdayEnd = StaffMember.Availability.GetValueOrDefault("thursday")?.End ?? "",
            FridayStart = StaffMember.Availability.GetValueOrDefault("friday")?.Start ?? "",
            FridayEnd = StaffMember.Availability.GetValueOrDefault("friday")?.End ?? "",
            SaturdayStart = StaffMember.Availability.GetValueOrDefault("saturday")?.Start ?? "",
            SaturdayEnd = StaffMember.Availability.GetValueOrDefault("saturday")?.End ?? "",
            SundayStart = StaffMember.Availability.GetValueOrDefault("sunday")?.Start ?? "",
            SundayEnd = StaffMember.Availability.GetValueOrDefault("sunday")?.End ?? "",
            CoverageZones = string.Join("\n", StaffMember.CoverageZones),
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        // Update flags
        var flagsResponse = await Mediator.Send(new UpdateSchedulerStaffFlags.Command
        {
            SchedulerStaffMemberId = Form.Id,
            CanManageOthersCalendars = Form.CanManageOthersCalendars,
            DefaultMeetingLink = Form.DefaultMeetingLink,
        }, cancellationToken);

        if (!flagsResponse.Success)
        {
            SetErrorMessage(flagsResponse.GetErrors());
            return Page();
        }

        // Update availability
        var availability = new Dictionary<string, UpdateStaffAvailability.DayScheduleInput>();
        AddDayIfSet(availability, "monday", Form.MondayStart, Form.MondayEnd);
        AddDayIfSet(availability, "tuesday", Form.TuesdayStart, Form.TuesdayEnd);
        AddDayIfSet(availability, "wednesday", Form.WednesdayStart, Form.WednesdayEnd);
        AddDayIfSet(availability, "thursday", Form.ThursdayStart, Form.ThursdayEnd);
        AddDayIfSet(availability, "friday", Form.FridayStart, Form.FridayEnd);
        AddDayIfSet(availability, "saturday", Form.SaturdayStart, Form.SaturdayEnd);
        AddDayIfSet(availability, "sunday", Form.SundayStart, Form.SundayEnd);

        var availabilityResponse = await Mediator.Send(new UpdateStaffAvailability.Command
        {
            SchedulerStaffMemberId = Form.Id,
            Availability = availability,
        }, cancellationToken);

        if (!availabilityResponse.Success)
        {
            SetErrorMessage(availabilityResponse.GetErrors());
            return Page();
        }

        // Update coverage zones
        var zipcodes = (Form.CoverageZones ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(z => z.Trim())
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .ToList();

        var zonesResponse = await Mediator.Send(new UpdateStaffCoverageZones.Command
        {
            SchedulerStaffMemberId = Form.Id,
            Zipcodes = zipcodes,
        }, cancellationToken);

        if (!zonesResponse.Success)
        {
            SetErrorMessage(zonesResponse.GetErrors());
            return Page();
        }

        SetSuccessMessage("Staff member updated successfully.");
        return RedirectToPage(RouteNames.Scheduler.Staff.Index);
    }

    private static void AddDayIfSet(
        Dictionary<string, UpdateStaffAvailability.DayScheduleInput> dict,
        string day,
        string? start,
        string? end)
    {
        if (!string.IsNullOrWhiteSpace(start) && !string.IsNullOrWhiteSpace(end))
        {
            dict[day] = new UpdateStaffAvailability.DayScheduleInput
            {
                Start = start.Trim(),
                End = end.Trim(),
            };
        }
    }

    public record EditStaffViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Can manage other staff calendars")]
        public bool CanManageOthersCalendars { get; set; }

        [Display(Name = "Default Meeting Link")]
        [Url]
        public string? DefaultMeetingLink { get; set; }

        // Per-day availability
        public string? MondayStart { get; set; }
        public string? MondayEnd { get; set; }
        public string? TuesdayStart { get; set; }
        public string? TuesdayEnd { get; set; }
        public string? WednesdayStart { get; set; }
        public string? WednesdayEnd { get; set; }
        public string? ThursdayStart { get; set; }
        public string? ThursdayEnd { get; set; }
        public string? FridayStart { get; set; }
        public string? FridayEnd { get; set; }
        public string? SaturdayStart { get; set; }
        public string? SaturdayEnd { get; set; }
        public string? SundayStart { get; set; }
        public string? SundayEnd { get; set; }

        [Display(Name = "Coverage Zones (one zipcode per line)")]
        public string? CoverageZones { get; set; }
    }
}
