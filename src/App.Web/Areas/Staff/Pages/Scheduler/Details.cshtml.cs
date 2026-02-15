using System.ComponentModel.DataAnnotations;
using App.Application.Scheduler.Commands;
using App.Application.Scheduler.DTOs;
using App.Application.Scheduler.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Filters;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Scheduler;

[ServiceFilter(typeof(SchedulerStaffAccessFilter))]
public class DetailsModel : BaseStaffPageModel
{
    public AppointmentDto Appointment { get; set; } = null!;

    [BindProperty]
    public CancelFormViewModel CancelForm { get; set; } = new();

    [BindProperty]
    public RescheduleFormViewModel RescheduleForm { get; set; } = new();

    public async Task<IActionResult> OnGet(long id, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Appointment Details";
        ViewData["ActiveMenu"] = "AllAppointments";

        var response = await Mediator.Send(new GetAppointmentById.Query { Id = id }, cancellationToken);
        Appointment = response.Result;

        ViewData["Title"] = $"Appointment {Appointment.Code}";

        return Page();
    }

    /// <summary>
    /// Confirm a scheduled appointment.
    /// </summary>
    public async Task<IActionResult> OnPostConfirm(long id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new ChangeAppointmentStatus.Command
        {
            AppointmentId = id,
            NewStatus = AppointmentStatus.CONFIRMED
        }, cancellationToken);

        if (response.Success)
            SetSuccessMessage("Appointment confirmed.");
        else
            SetErrorMessage(response.GetErrors());

        return RedirectToPage(RouteNames.Scheduler.Details, new { id });
    }

    /// <summary>
    /// Start a confirmed appointment.
    /// </summary>
    public async Task<IActionResult> OnPostStart(long id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new ChangeAppointmentStatus.Command
        {
            AppointmentId = id,
            NewStatus = AppointmentStatus.IN_PROGRESS
        }, cancellationToken);

        if (response.Success)
            SetSuccessMessage("Appointment started.");
        else
            SetErrorMessage(response.GetErrors());

        return RedirectToPage(RouteNames.Scheduler.Details, new { id });
    }

    /// <summary>
    /// Complete an in-progress appointment.
    /// </summary>
    public async Task<IActionResult> OnPostComplete(long id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new ChangeAppointmentStatus.Command
        {
            AppointmentId = id,
            NewStatus = AppointmentStatus.COMPLETED
        }, cancellationToken);

        if (response.Success)
            SetSuccessMessage("Appointment marked as completed.");
        else
            SetErrorMessage(response.GetErrors());

        return RedirectToPage(RouteNames.Scheduler.Details, new { id });
    }

    /// <summary>
    /// Cancel an active appointment.
    /// </summary>
    public async Task<IActionResult> OnPostCancel(long id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CancelForm.CancellationReason))
        {
            SetErrorMessage("Cancellation reason is required.");
            return RedirectToPage(RouteNames.Scheduler.Details, new { id });
        }

        var response = await Mediator.Send(new CancelAppointment.Command
        {
            AppointmentId = id,
            CancellationReason = CancelForm.CancellationReason,
            CancellationNoticeOverrideReason = CancelForm.CancellationNoticeOverrideReason
        }, cancellationToken);

        if (response.Success)
            SetSuccessMessage("Appointment cancelled.");
        else
            SetErrorMessage(response.GetErrors());

        return RedirectToPage(RouteNames.Scheduler.Details, new { id });
    }

    /// <summary>
    /// Mark an active appointment as no-show.
    /// </summary>
    public async Task<IActionResult> OnPostNoShow(long id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new MarkAppointmentNoShow.Command
        {
            AppointmentId = id
        }, cancellationToken);

        if (response.Success)
            SetSuccessMessage("Appointment marked as no-show.");
        else
            SetErrorMessage(response.GetErrors());

        return RedirectToPage(RouteNames.Scheduler.Details, new { id });
    }

    /// <summary>
    /// Reschedule an active appointment.
    /// </summary>
    public async Task<IActionResult> OnPostReschedule(long id, CancellationToken cancellationToken)
    {
        if (RescheduleForm.NewScheduledStartTime == default)
        {
            SetErrorMessage("New scheduled time is required.");
            return RedirectToPage(RouteNames.Scheduler.Details, new { id });
        }

        var response = await Mediator.Send(new RescheduleAppointment.Command
        {
            AppointmentId = id,
            NewScheduledStartTime = RescheduleForm.NewScheduledStartTime,
            NewDurationMinutes = RescheduleForm.NewDurationMinutes,
            CancellationNoticeOverrideReason = RescheduleForm.CancellationNoticeOverrideReason
        }, cancellationToken);

        if (response.Success)
            SetSuccessMessage("Appointment rescheduled successfully.");
        else
            SetErrorMessage(response.GetErrors());

        return RedirectToPage(RouteNames.Scheduler.Details, new { id });
    }

    public record CancelFormViewModel
    {
        [Required]
        [Display(Name = "Cancellation Reason")]
        public string CancellationReason { get; set; } = string.Empty;

        [Display(Name = "Notice Override Reason")]
        public string? CancellationNoticeOverrideReason { get; set; }
    }

    public record RescheduleFormViewModel
    {
        [Required]
        [Display(Name = "New Date & Time")]
        public DateTime NewScheduledStartTime { get; set; }

        [Required]
        [Range(1, 480)]
        [Display(Name = "Duration (minutes)")]
        public int NewDurationMinutes { get; set; } = 30;

        [Display(Name = "Notice Override Reason")]
        public string? CancellationNoticeOverrideReason { get; set; }
    }
}
