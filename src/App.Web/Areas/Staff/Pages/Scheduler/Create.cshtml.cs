using System.ComponentModel.DataAnnotations;
using App.Application.Contacts.Queries;
using App.Application.Scheduler.Commands;
using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Filters;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Scheduler;

[ServiceFilter(typeof(SchedulerStaffAccessFilter))]
public class CreateModel : BaseStaffPageModel
{
    [BindProperty]
    public CreateAppointmentViewModel Form { get; set; } = new();

    /// <summary>
    /// Active appointment types with eligible staff info for the form dropdowns.
    /// </summary>
    public IEnumerable<AppointmentTypeDto> AppointmentTypes { get; set; } =
        Enumerable.Empty<AppointmentTypeDto>();

    /// <summary>
    /// Active scheduler staff members for the assignee dropdown.
    /// </summary>
    public IEnumerable<SchedulerStaffDto> StaffMembers { get; set; } =
        Enumerable.Empty<SchedulerStaffDto>();

    /// <summary>
    /// Default duration from org scheduler configuration.
    /// </summary>
    public int DefaultDurationMinutes { get; set; } = 30;

    public async Task<IActionResult> OnGet(
        long? contactId = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Create Appointment";
        ViewData["ActiveMenu"] = "AllAppointments";

        await LoadFormDataAsync(cancellationToken);

        // Pre-fill contact if provided via query string
        if (contactId.HasValue)
        {
            Form.ContactId = contactId.Value;
        }

        // Pre-fill default duration
        Form.DurationMinutes = DefaultDurationMinutes;

        return Page();
    }

    /// <summary>
    /// AJAX handler for contact search (same pattern as ticket creation).
    /// </summary>
    public async Task<IActionResult> OnGetSearchContact(
        string searchTerm,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new JsonResult(new { results = Array.Empty<object>() });
        }

        var response = await Mediator.Send(
            new SearchContacts.Query { SearchTerm = searchTerm, MaxResults = 10 },
            cancellationToken
        );

        var results = response.Result.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            email = c.Email,
            phone = c.PrimaryPhone,
            organizationAccount = c.OrganizationAccount,
            ticketCount = c.TicketCount,
        });

        return new JsonResult(new { results });
    }

    /// <summary>
    /// AJAX handler for getting a specific contact by ID (for pre-population).
    /// </summary>
    public async Task<IActionResult> OnGetContactById(long id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Mediator.Send(
                new GetContactById.Query { Id = id },
                cancellationToken
            );

            var contact = response.Result;
            return new JsonResult(
                new
                {
                    success = true,
                    contact = new
                    {
                        id = contact.Id,
                        name = contact.Name,
                        firstName = contact.FirstName,
                        lastName = contact.LastName,
                        email = contact.Email,
                        phoneNumbers = contact.PhoneNumbers,
                        primaryPhone = contact.PhoneNumbers.FirstOrDefault(),
                        address = contact.Address,
                        organizationAccount = contact.OrganizationAccount,
                        ticketCount = contact.TicketCount,
                    },
                }
            );
        }
        catch
        {
            return new JsonResult(new { success = false, message = "Contact not found" });
        }
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create Appointment";
        ViewData["ActiveMenu"] = "AllAppointments";

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(cancellationToken);
            return Page();
        }

        var command = new CreateAppointment.Command
        {
            ContactId = Form.ContactId,
            AppointmentTypeId = new ShortGuid(Form.AppointmentTypeId),
            AssignedStaffMemberId = new ShortGuid(Form.AssignedStaffMemberId),
            Mode = Form.Mode,
            MeetingLink = Form.MeetingLink,
            ScheduledStartTime = Form.ScheduledStartTime,
            DurationMinutes = Form.DurationMinutes,
            Notes = Form.Notes,
            CoverageZoneOverrideReason = Form.CoverageZoneOverrideReason,
            ContactFirstName = Form.ContactFirstName ?? string.Empty,
            ContactLastName = Form.ContactLastName,
            ContactEmail = Form.ContactEmail,
            ContactPhone = Form.ContactPhone,
            ContactAddress = Form.ContactAddress,
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage($"Appointment {FormatCode(response.Result)} created successfully.");
            return RedirectToPage(RouteNames.Scheduler.Details, new { id = response.Result });
        }

        SetErrorMessage(response.GetErrors());
        await LoadFormDataAsync(cancellationToken);
        return Page();
    }

    private async Task LoadFormDataAsync(CancellationToken cancellationToken)
    {
        // Load appointment types with full detail (including eligible staff)
        // NOTE: Must run sequentially — DbContext is not thread-safe
        var typesResponse = await Mediator.Send(new GetAppointmentTypes.Query
        {
            IncludeInactive = false,
            PageSize = 500
        }, cancellationToken);

        var typeDetails = new List<AppointmentTypeDto>();
        foreach (var t in typesResponse.Result.Items)
        {
            var detailResponse = await Mediator.Send(new GetAppointmentTypeById.Query
            {
                Id = t.Id
            }, cancellationToken);
            typeDetails.Add(detailResponse.Result);
        }
        AppointmentTypes = typeDetails;

        // Load active staff members
        var staffResponse = await Mediator.Send(new GetSchedulerStaff.Query
        {
            PageSize = 500
        }, cancellationToken);

        var staffDetails = new List<SchedulerStaffDto>();
        foreach (var s in staffResponse.Result.Items.Where(s => s.IsActive))
        {
            var detailResponse = await Mediator.Send(new GetSchedulerStaffById.Query
            {
                Id = s.Id
            }, cancellationToken);
            staffDetails.Add(detailResponse.Result);
        }
        StaffMembers = staffDetails;

        // Load org default duration
        var configResponse = await Mediator.Send(new GetSchedulerConfiguration.Query(), cancellationToken);
        DefaultDurationMinutes = configResponse.Result.DefaultDurationMinutes;
    }

    private static string FormatCode(long id) => $"APT-{id:D4}";

    public record CreateAppointmentViewModel
    {
        [Required]
        [Display(Name = "Contact")]
        public long ContactId { get; set; }

        [Required]
        [Display(Name = "Appointment Type")]
        public string AppointmentTypeId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Assigned Staff Member")]
        public string AssignedStaffMemberId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Mode")]
        public string Mode { get; set; } = string.Empty;

        [Display(Name = "Meeting Link")]
        [Url]
        public string? MeetingLink { get; set; }

        [Required]
        [Display(Name = "Scheduled Start Time")]
        public DateTime ScheduledStartTime { get; set; }

        [Required]
        [Range(1, 480)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 30;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Coverage Zone Override Reason")]
        public string? CoverageZoneOverrideReason { get; set; }

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
