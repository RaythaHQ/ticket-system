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
public class EditModel : BaseStaffPageModel
{
    public AppointmentDto Appointment { get; set; } = null!;

    [BindProperty]
    public EditAppointmentViewModel Form { get; set; } = new();

    public async Task<IActionResult> OnGet(long id, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Edit Appointment";
        ViewData["ActiveMenu"] = "AllAppointments";

        var response = await Mediator.Send(new GetAppointmentById.Query { Id = id }, cancellationToken);
        Appointment = response.Result;

        // Only allow editing non-terminal appointments
        var statusValue = AppointmentStatus.From(Appointment.Status);
        if (statusValue.IsTerminal)
        {
            SetErrorMessage("Cannot edit an appointment in a terminal status.");
            return RedirectToPage(RouteNames.Scheduler.Details, new { id });
        }

        Form = new EditAppointmentViewModel
        {
            AppointmentId = Appointment.Id,
            Notes = Appointment.Notes,
            MeetingLink = Appointment.MeetingLink,
            ContactFirstName = Appointment.AppointmentContactFirstName,
            ContactLastName = Appointment.AppointmentContactLastName,
            ContactEmail = Appointment.AppointmentContactEmail,
            ContactPhone = Appointment.AppointmentContactPhone,
            ContactAddress = Appointment.AppointmentContactAddress,
        };

        ViewData["Title"] = $"Edit {Appointment.Code}";

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Appointment";
        ViewData["ActiveMenu"] = "AllAppointments";

        if (!ModelState.IsValid)
        {
            // Reload appointment for display
            var loadResponse = await Mediator.Send(new GetAppointmentById.Query { Id = Form.AppointmentId }, cancellationToken);
            Appointment = loadResponse.Result;
            return Page();
        }

        var command = new UpdateAppointment.Command
        {
            AppointmentId = Form.AppointmentId,
            Notes = Form.Notes,
            MeetingLink = Form.MeetingLink,
            ContactFirstName = Form.ContactFirstName,
            ContactLastName = Form.ContactLastName,
            ContactEmail = Form.ContactEmail,
            ContactPhone = Form.ContactPhone,
            ContactAddress = Form.ContactAddress,
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Appointment updated successfully.");
            return RedirectToPage(RouteNames.Scheduler.Details, new { id = Form.AppointmentId });
        }

        SetErrorMessage(response.GetErrors());

        // Reload appointment for display
        var reloadResponse = await Mediator.Send(new GetAppointmentById.Query { Id = Form.AppointmentId }, cancellationToken);
        Appointment = reloadResponse.Result;
        return Page();
    }

    public record EditAppointmentViewModel
    {
        public long AppointmentId { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Meeting Link")]
        [Url]
        public string? MeetingLink { get; set; }

        // Per-appointment contact fields
        [Required]
        [MaxLength(250)]
        [Display(Name = "Contact First Name")]
        public string? ContactFirstName { get; set; }

        [MaxLength(250)]
        [Display(Name = "Contact Last Name")]
        public string? ContactLastName { get; set; }

        [EmailAddress]
        [MaxLength(500)]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Contact Address")]
        public string? ContactAddress { get; set; }
    }
}
