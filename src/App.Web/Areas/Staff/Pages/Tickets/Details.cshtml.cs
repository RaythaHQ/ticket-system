using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for viewing ticket details.
/// </summary>
public class Details : BaseStaffPageModel
{
    public TicketDto Ticket { get; set; } = null!;
    public IEnumerable<TicketCommentDto> Comments { get; set; } = Enumerable.Empty<TicketCommentDto>();
    public IEnumerable<TicketChangeLogEntryDto> ChangeLog { get; set; } = Enumerable.Empty<TicketChangeLogEntryDto>();

    [BindProperty]
    public AddCommentViewModel CommentForm { get; set; } = new();

    public bool CanManageTickets { get; set; }

    public async Task<IActionResult> OnGet(long id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = $"Ticket #{id}";
        ViewData["ActiveMenu"] = "Tickets";

        var ticketResponse = await Mediator.Send(new GetTicketById.Query { Id = id }, cancellationToken);
        Ticket = ticketResponse.Result;

        var commentsResponse = await Mediator.Send(new GetTicketComments.Query { TicketId = id }, cancellationToken);
        Comments = commentsResponse.Result;

        var changeLogResponse = await Mediator.Send(new GetTicketChangeLog.Query { TicketId = id }, cancellationToken);
        ChangeLog = changeLogResponse.Result;

        CanManageTickets = TicketPermissionService.CanManageTickets();

        return Page();
    }

    public async Task<IActionResult> OnPostAddComment(long id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CommentForm.Body))
        {
            ModelState.AddModelError("CommentForm.Body", "Comment cannot be empty.");
            return await OnGet(id, cancellationToken);
        }

        var command = new AddTicketComment.Command
        {
            TicketId = id,
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

        return RedirectToPage(RouteNames.Tickets.Details, new { id });
    }

    public record AddCommentViewModel
    {
        [Required]
        public string Body { get; set; } = string.Empty;
    }
}

