using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Contacts;
using App.Application.Contacts.Commands;
using App.Application.Contacts.Queries;
using App.Application.Scheduler.DTOs;
using App.Application.Scheduler.Queries;
using App.Application.Tickets;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Contacts;

/// <summary>
/// Page model for viewing contact details.
/// </summary>
public class Details : BaseStaffPageModel
{
    public ContactDto Contact { get; set; } = null!;
    public IEnumerable<TicketListItemDto> Tickets { get; set; } =
        Enumerable.Empty<TicketListItemDto>();
    public IEnumerable<ContactCommentDto> Comments { get; set; } =
        Enumerable.Empty<ContactCommentDto>();
    public IEnumerable<ContactChangeLogEntryDto> ChangeLog { get; set; } =
        Enumerable.Empty<ContactChangeLogEntryDto>();
    public ListResultDto<AppointmentListItemDto>? Appointments { get; set; }
    public bool IsSchedulerStaff { get; set; }

    [BindProperty]
    public AddCommentViewModel CommentForm { get; set; } = new();

    /// <summary>
    /// Back to list URL - preserved across navigation and form submissions.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? BackToListUrl { get; set; }

    public async Task<IActionResult> OnGet(
        long id,
        string? backToListUrl = null,
        CancellationToken cancellationToken = default
    )
    {
        ViewData["Title"] = $"Contact #{id}";
        ViewData["ActiveMenu"] = "Contacts";

        var contactResponse = await Mediator.Send(
            new GetContactById.Query { Id = id },
            cancellationToken
        );
        Contact = contactResponse.Result;

        var ticketsResponse = await Mediator.Send(
            new GetContactTickets.Query { ContactId = id },
            cancellationToken
        );
        Tickets = ticketsResponse.Result;

        var commentsResponse = await Mediator.Send(
            new GetContactComments.Query { ContactId = id },
            cancellationToken
        );
        Comments = commentsResponse.Result;

        var changeLogResponse = await Mediator.Send(
            new GetContactChangeLog.Query { ContactId = id },
            cancellationToken
        );
        ChangeLog = changeLogResponse.Result;

        // Load scheduler appointments for this contact
        var appointmentsResponse = await Mediator.Send(
            new GetContactAppointments.Query { ContactId = id },
            cancellationToken
        );
        Appointments = appointmentsResponse.Result;

        // Check if current user is scheduler staff
        var schedulerPermissionService =
            HttpContext.RequestServices.GetRequiredService<ISchedulerPermissionService>();
        IsSchedulerStaff = await schedulerPermissionService.IsSchedulerStaffAsync(cancellationToken);

        // Store back URL (use parameter if provided, otherwise use property)
        BackToListUrl = backToListUrl ?? BackToListUrl;
        ViewData["BackToListUrl"] = BackToListUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAddComment(long id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CommentForm.Body))
        {
            ModelState.AddModelError("CommentForm.Body", "Comment cannot be empty.");
            return await OnGet(id, BackToListUrl, cancellationToken);
        }

        var command = new AddContactComment.Command { ContactId = id, Body = CommentForm.Body };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Comment added successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Contacts.Details, new { id, backToListUrl = BackToListUrl });
    }

    public record AddCommentViewModel
    {
        [Required]
        public string Body { get; set; } = string.Empty;
    }
}
