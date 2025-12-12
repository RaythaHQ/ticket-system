using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Contacts;
using App.Application.Contacts.Commands;
using App.Application.Contacts.Queries;
using App.Application.Tickets;
using App.Web.Areas.Staff.Pages.Shared.Models;

namespace App.Web.Areas.Staff.Pages.Contacts;

/// <summary>
/// Page model for viewing contact details.
/// </summary>
public class Details : BaseStaffPageModel
{
    public ContactDto Contact { get; set; } = null!;
    public IEnumerable<TicketListItemDto> Tickets { get; set; } = Enumerable.Empty<TicketListItemDto>();
    public IEnumerable<ContactCommentDto> Comments { get; set; } = Enumerable.Empty<ContactCommentDto>();
    public IEnumerable<ContactChangeLogEntryDto> ChangeLog { get; set; } = Enumerable.Empty<ContactChangeLogEntryDto>();

    [BindProperty]
    public AddCommentViewModel CommentForm { get; set; } = new();

    public async Task<IActionResult> OnGet(long id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = $"Contact #{id}";
        ViewData["ActiveMenu"] = "Contacts";

        var contactResponse = await Mediator.Send(new GetContactById.Query { Id = id }, cancellationToken);
        Contact = contactResponse.Result;

        var ticketsResponse = await Mediator.Send(new GetContactTickets.Query { ContactId = id }, cancellationToken);
        Tickets = ticketsResponse.Result;

        var commentsResponse = await Mediator.Send(new GetContactComments.Query { ContactId = id }, cancellationToken);
        Comments = commentsResponse.Result;

        var changeLogResponse = await Mediator.Send(new GetContactChangeLog.Query { ContactId = id }, cancellationToken);
        ChangeLog = changeLogResponse.Result;

        return Page();
    }

    public async Task<IActionResult> OnPostAddComment(long id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CommentForm.Body))
        {
            ModelState.AddModelError("CommentForm.Body", "Comment cannot be empty.");
            return await OnGet(id, cancellationToken);
        }

        var command = new AddContactComment.Command
        {
            ContactId = id,
            Body = CommentForm.Body
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Comment added successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage("./Details", new { id });
    }

    public record AddCommentViewModel
    {
        [Required]
        public string Body { get; set; } = string.Empty;
    }
}

