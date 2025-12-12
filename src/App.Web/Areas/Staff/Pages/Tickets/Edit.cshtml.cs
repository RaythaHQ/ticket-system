using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for editing a ticket. Requires CanManageTickets permission.
/// </summary>
public class Edit : BaseStaffPageModel
{
    public TicketDto Ticket { get; set; } = null!;

    [BindProperty]
    public EditTicketViewModel Form { get; set; } = new();

    public List<AssigneeSelectItem> AvailableAssignees { get; set; } = new();

    public async Task<IActionResult> OnGet(long id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Ticket";
        ViewData["ActiveMenu"] = "Tickets";

        var response = await Mediator.Send(new GetTicketById.Query { Id = id }, cancellationToken);
        Ticket = response.Result;

        // Check permission - user can edit if they have CanManageTickets, are assigned, or are in the team
        if (!await TicketPermissionService.CanEditTicketAsync(Ticket.AssigneeId?.Guid, Ticket.OwningTeamId?.Guid, cancellationToken))
        {
            return Forbid();
        }

        Form = new EditTicketViewModel
        {
            Id = Ticket.Id,
            Title = Ticket.Title,
            Description = Ticket.Description,
            Status = Ticket.Status,
            Priority = Ticket.Priority,
            Category = Ticket.Category,
            Tags = string.Join(", ", Ticket.Tags),
            OwningTeamId = Ticket.OwningTeamId,
            AssigneeId = Ticket.AssigneeId,
            ContactId = Ticket.ContactId
        };

        await LoadSelectListsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        // Re-fetch ticket to check permissions
        var ticketResponse = await Mediator.Send(new GetTicketById.Query { Id = Form.Id }, cancellationToken);
        var ticket = ticketResponse.Result;

        if (!await TicketPermissionService.CanEditTicketAsync(ticket.AssigneeId?.Guid, ticket.OwningTeamId?.Guid, cancellationToken))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(cancellationToken);
            return Page();
        }

        var command = new UpdateTicket.Command
        {
            Id = Form.Id,
            Title = Form.Title,
            Description = Form.Description,
            Priority = Form.Priority,
            Category = Form.Category,
            Tags = Form.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
            OwningTeamId = Form.OwningTeamId,
            AssigneeId = Form.AssigneeId,
            ContactId = Form.ContactId
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Ticket updated successfully.");
            return RedirectToPage(RouteNames.Tickets.Details, new { id = Form.Id });
        }

        SetErrorMessage(response.GetErrors());
        await LoadSelectListsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostChangeStatus(long id, string status, CancellationToken cancellationToken)
    {
        var command = new ChangeTicketStatus.Command { Id = id, NewStatus = status };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage($"Status changed to {TicketStatus.From(status).Label}.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id });
    }

    public async Task<IActionResult> OnPostClose(long id, CancellationToken cancellationToken)
    {
        var command = new CloseTicket.Command { Id = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Ticket closed.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id });
    }

    public async Task<IActionResult> OnPostReopen(long id, CancellationToken cancellationToken)
    {
        var command = new ReopenTicket.Command { Id = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Ticket reopened.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id });
    }

    private async Task LoadSelectListsAsync(CancellationToken cancellationToken)
    {
        var canManageTickets = TicketPermissionService.CanManageTickets();
        
        var assigneeOptionsResponse = await Mediator.Send(
            new GetAssigneeSelectOptions.Query
            {
                CanManageTickets = canManageTickets,
                CurrentUserId = CurrentUser.UserId?.Guid
            },
            cancellationToken
        );

        AvailableAssignees = assigneeOptionsResponse.Result.Select(a => new AssigneeSelectItem
        {
            Value = a.Value,
            DisplayText = a.DisplayText,
            TeamId = a.TeamId,
            AssigneeId = a.AssigneeId
        }).ToList();
    }

    public record EditTicketViewModel
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Status { get; set; } = TicketStatus.OPEN;

        [Required]
        public string Priority { get; set; } = TicketPriority.NORMAL;

        public string? Category { get; set; }

        public string? Tags { get; set; }

        public ShortGuid? OwningTeamId { get; set; }

        public ShortGuid? AssigneeId { get; set; }

        public long? ContactId { get; set; }
    }

    public record AssigneeSelectItem
    {
        public string Value { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
        public ShortGuid? TeamId { get; init; }
        public ShortGuid? AssigneeId { get; init; }
    }
}

