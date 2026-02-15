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

namespace App.Web.Areas.Admin.Pages.Scheduler.Configuration;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Index : BaseAdminPageModel
{
    [BindProperty]
    public ConfigurationViewModel Form { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Scheduler Configuration",
                RouteName = RouteNames.Scheduler.Configuration.Index,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetSchedulerConfiguration.Query(), cancellationToken);
        var config = response.Result;

        Form = new ConfigurationViewModel
        {
            MondayStart = config.AvailableHours.GetValueOrDefault("monday")?.Start ?? "",
            MondayEnd = config.AvailableHours.GetValueOrDefault("monday")?.End ?? "",
            TuesdayStart = config.AvailableHours.GetValueOrDefault("tuesday")?.Start ?? "",
            TuesdayEnd = config.AvailableHours.GetValueOrDefault("tuesday")?.End ?? "",
            WednesdayStart = config.AvailableHours.GetValueOrDefault("wednesday")?.Start ?? "",
            WednesdayEnd = config.AvailableHours.GetValueOrDefault("wednesday")?.End ?? "",
            ThursdayStart = config.AvailableHours.GetValueOrDefault("thursday")?.Start ?? "",
            ThursdayEnd = config.AvailableHours.GetValueOrDefault("thursday")?.End ?? "",
            FridayStart = config.AvailableHours.GetValueOrDefault("friday")?.Start ?? "",
            FridayEnd = config.AvailableHours.GetValueOrDefault("friday")?.End ?? "",
            SaturdayStart = config.AvailableHours.GetValueOrDefault("saturday")?.Start ?? "",
            SaturdayEnd = config.AvailableHours.GetValueOrDefault("saturday")?.End ?? "",
            SundayStart = config.AvailableHours.GetValueOrDefault("sunday")?.Start ?? "",
            SundayEnd = config.AvailableHours.GetValueOrDefault("sunday")?.End ?? "",
            DefaultDurationMinutes = config.DefaultDurationMinutes,
            DefaultBufferTimeMinutes = config.DefaultBufferTimeMinutes,
            DefaultBookingHorizonDays = config.DefaultBookingHorizonDays,
            MinCancellationNoticeHours = config.MinCancellationNoticeHours,
            ReminderLeadTimeMinutes = config.ReminderLeadTimeMinutes,
            DefaultCoverageZones = string.Join("\n", config.DefaultCoverageZones),
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        var availableHours = new Dictionary<string, UpdateSchedulerConfiguration.DayScheduleInput>();
        AddDayIfSet(availableHours, "monday", Form.MondayStart, Form.MondayEnd);
        AddDayIfSet(availableHours, "tuesday", Form.TuesdayStart, Form.TuesdayEnd);
        AddDayIfSet(availableHours, "wednesday", Form.WednesdayStart, Form.WednesdayEnd);
        AddDayIfSet(availableHours, "thursday", Form.ThursdayStart, Form.ThursdayEnd);
        AddDayIfSet(availableHours, "friday", Form.FridayStart, Form.FridayEnd);
        AddDayIfSet(availableHours, "saturday", Form.SaturdayStart, Form.SaturdayEnd);
        AddDayIfSet(availableHours, "sunday", Form.SundayStart, Form.SundayEnd);

        var coverageZones = (Form.DefaultCoverageZones ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(z => z.Trim())
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .ToList();

        var response = await Mediator.Send(new UpdateSchedulerConfiguration.Command
        {
            AvailableHours = availableHours,
            DefaultDurationMinutes = Form.DefaultDurationMinutes,
            DefaultBufferTimeMinutes = Form.DefaultBufferTimeMinutes,
            DefaultBookingHorizonDays = Form.DefaultBookingHorizonDays,
            MinCancellationNoticeHours = Form.MinCancellationNoticeHours,
            ReminderLeadTimeMinutes = Form.ReminderLeadTimeMinutes,
            DefaultCoverageZones = coverageZones,
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Scheduler configuration updated successfully.");
            return RedirectToPage(RouteNames.Scheduler.Configuration.Index);
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    private static void AddDayIfSet(
        Dictionary<string, UpdateSchedulerConfiguration.DayScheduleInput> dict,
        string day,
        string? start,
        string? end)
    {
        if (!string.IsNullOrWhiteSpace(start) && !string.IsNullOrWhiteSpace(end))
        {
            dict[day] = new UpdateSchedulerConfiguration.DayScheduleInput
            {
                Start = start.Trim(),
                End = end.Trim(),
            };
        }
    }

    public record ConfigurationViewModel
    {
        // Per-day available hours
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

        [Required]
        [Display(Name = "Default Duration (minutes)")]
        [Range(1, 480)]
        public int DefaultDurationMinutes { get; set; } = 30;

        [Required]
        [Display(Name = "Buffer Time (minutes)")]
        [Range(0, 120)]
        public int DefaultBufferTimeMinutes { get; set; } = 15;

        [Required]
        [Display(Name = "Booking Horizon (days)")]
        [Range(1, 365)]
        public int DefaultBookingHorizonDays { get; set; } = 30;

        [Required]
        [Display(Name = "Min Cancellation Notice (hours)")]
        [Range(0, 168)]
        public int MinCancellationNoticeHours { get; set; } = 24;

        [Required]
        [Display(Name = "Reminder Lead Time (minutes)")]
        [Range(1, 10080)]
        public int ReminderLeadTimeMinutes { get; set; } = 60;

        [Display(Name = "Default Coverage Zones (one zipcode per line)")]
        public string? DefaultCoverageZones { get; set; }
    }
}
