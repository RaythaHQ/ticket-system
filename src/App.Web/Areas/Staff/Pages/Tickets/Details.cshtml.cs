using System.ComponentModel.DataAnnotations;
using App.Application.Contacts;
using App.Application.Contacts.Queries;
using App.Application.Tickets;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for viewing ticket details.
/// </summary>
public class Details : BaseStaffPageModel
{
    public TicketDto Ticket { get; set; } = null!;
    public ContactDto? Contact { get; set; }
    public IEnumerable<TicketCommentDto> Comments { get; set; } =
        Enumerable.Empty<TicketCommentDto>();
    public IEnumerable<TicketChangeLogEntryDto> ChangeLog { get; set; } =
        Enumerable.Empty<TicketChangeLogEntryDto>();

    [BindProperty]
    public AddCommentViewModel CommentForm { get; set; } = new();

    public bool CanEditTicket { get; set; }
    public bool CanDeleteTicket { get; set; }
    public bool CanManageTickets { get; set; }

    // Following feature
    public bool IsFollowing { get; set; }
    public IEnumerable<GetTicketFollowers.TicketFollowerDto> Followers { get; set; } =
        Enumerable.Empty<GetTicketFollowers.TicketFollowerDto>();

    public async Task<IActionResult> OnGet(
        long id,
        string? backToListUrl = null,
        CancellationToken cancellationToken = default
    )
    {
        ViewData["Title"] = $"Ticket #{id}";
        ViewData["ActiveMenu"] = "Tickets";

        var ticketResponse = await Mediator.Send(
            new GetTicketById.Query { Id = id },
            cancellationToken
        );
        Ticket = ticketResponse.Result;

        var commentsResponse = await Mediator.Send(
            new GetTicketComments.Query { TicketId = id },
            cancellationToken
        );
        Comments = commentsResponse.Result;

        var changeLogResponse = await Mediator.Send(
            new GetTicketChangeLog.Query { TicketId = id },
            cancellationToken
        );
        ChangeLog = changeLogResponse.Result;

        // Load following status and followers list
        var isFollowingResponse = await Mediator.Send(
            new IsFollowingTicket.Query { TicketId = id },
            cancellationToken
        );
        IsFollowing = isFollowingResponse.Result;

        var followersResponse = await Mediator.Send(
            new GetTicketFollowers.Query { TicketId = id },
            cancellationToken
        );
        Followers = followersResponse.Result;

        // Load full contact details if contact is assigned
        if (Ticket.ContactId.HasValue)
        {
            var contactResponse = await Mediator.Send(
                new GetContactById.Query { Id = Ticket.ContactId.Value },
                cancellationToken
            );
            Contact = contactResponse.Result;
        }

        // Check if user can edit this specific ticket (has permission, is assigned, or is in the team)
        CanEditTicket = await TicketPermissionService.CanEditTicketAsync(
            Ticket.AssigneeId?.Guid,
            Ticket.OwningTeamId?.Guid,
            cancellationToken
        );

        // Check if user can delete tickets (requires Can Manage Tickets permission)
        CanDeleteTicket = TicketPermissionService.CanManageTickets();
        CanManageTickets = TicketPermissionService.CanManageTickets();

        // Store back URL for the view
        ViewData["BackToListUrl"] = backToListUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostDelete(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteTicket.Command { Id = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Ticket deleted successfully.");
            return RedirectToPage(RouteNames.Tickets.Index);
        }
        else
        {
            SetErrorMessage(response.GetErrors());
            return await OnGet(id, null, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostAddComment(long id, CancellationToken cancellationToken)
    {
        var backToListUrl = Request.Query["backToListUrl"].ToString();

        if (string.IsNullOrWhiteSpace(CommentForm.Body))
        {
            ModelState.AddModelError("CommentForm.Body", "Comment cannot be empty.");
            return await OnGet(id, backToListUrl, cancellationToken);
        }

        var command = new AddTicketComment.Command { TicketId = id, Body = CommentForm.Body };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Comment added successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        if (!string.IsNullOrEmpty(backToListUrl))
        {
            return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl });
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id });
    }

    public async Task<IActionResult> OnPostRefreshSla(
        long id,
        bool restartFromNow,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new RefreshTicketSla.Command { Id = id, RestartFromNow = restartFromNow };

            var response = await Mediator.Send(command, cancellationToken);

            if (response.Success)
            {
                var message = restartFromNow
                    ? "SLA restarted from current time."
                    : "SLA rules re-evaluated.";
                SetSuccessMessage(message);
            }
            else
            {
                SetErrorMessage(response.GetErrors());
            }
        }
        catch (Application.Common.Exceptions.BusinessException ex)
        {
            SetErrorMessage(ex.Message);
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id });
    }

    public async Task<IActionResult> OnPostFollow(long id, CancellationToken cancellationToken)
    {
        var command = new FollowTicket.Command { TicketId = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("You are now following this ticket.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id });
    }

    public async Task<IActionResult> OnPostUnfollow(long id, CancellationToken cancellationToken)
    {
        var command = new UnfollowTicket.Command { TicketId = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("You are no longer following this ticket.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id });
    }

    /// <summary>
    /// API endpoint for searching mentionable teams and users for @ autocomplete.
    /// </summary>
    public async Task<IActionResult> OnGetSearchMentionables(
        string? query,
        CancellationToken cancellationToken
    )
    {
        var response = await Mediator.Send(
            new SearchMentionables.Query { SearchTerm = query ?? string.Empty, MaxResults = 10 },
            cancellationToken
        );

        var result = response.Result;
        return new JsonResult(
            new
            {
                teams = result.Teams.Select(t => new
                {
                    id = t.Id.ToString(),
                    name = t.Name,
                    memberCount = t.MemberCount,
                    type = "team",
                }),
                users = result.Users.Select(u => new
                {
                    id = u.Id.ToString(),
                    name = u.Name,
                    email = u.Email,
                    type = "user",
                }),
            }
        );
    }

    public record AddCommentViewModel
    {
        [Required]
        public string Body { get; set; } = string.Empty;
    }
}
