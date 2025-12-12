using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for editing a ticket. Requires CanManageTickets permission.
/// </summary>
public class Edit : BaseStaffPageModel
{
    public TicketDto Ticket { get; set; } = null!;

    [BindProperty]
    public EditTicketViewModel Form { get; set; } = new();

    public List<TeamSelectItem> AvailableTeams { get; set; } = new();
    public List<UserSelectItem> AvailableAssignees { get; set; } = new();

    public async Task<IActionResult> OnGet(long id, CancellationToken cancellationToken)
    {
        // Check permission
        if (!TicketPermissionService.CanManageTickets())
        {
            return Forbid();
        }

        ViewData["Title"] = "Edit Ticket";
        ViewData["ActiveMenu"] = "Tickets";

        var response = await Mediator.Send(new GetTicketById.Query { Id = id }, cancellationToken);
        Ticket = response.Result;

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
        if (!TicketPermissionService.CanManageTickets())
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
            return RedirectToPage("./Details", new { id = Form.Id });
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

        return RedirectToPage("./Details", new { id });
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

        return RedirectToPage("./Details", new { id });
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

        return RedirectToPage("./Details", new { id });
    }

    private async Task LoadSelectListsAsync(CancellationToken cancellationToken)
    {
        AvailableTeams = await Db.Teams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TeamSelectItem { Id = t.Id, Name = t.Name })
            .ToListAsync(cancellationToken);

        AvailableAssignees = await Db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new UserSelectItem { Id = u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(cancellationToken);
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

        public Guid? OwningTeamId { get; set; }

        public Guid? AssigneeId { get; set; }

        public long? ContactId { get; set; }
    }

    public record TeamSelectItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public record UserSelectItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}

