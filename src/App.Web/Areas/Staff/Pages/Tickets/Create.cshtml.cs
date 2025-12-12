using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets.Commands;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for creating a new ticket.
/// </summary>
public class Create : BaseStaffPageModel
{
    [BindProperty]
    public CreateTicketViewModel Form { get; set; } = new();

    public List<TeamSelectItem> AvailableTeams { get; set; } = new();
    public List<UserSelectItem> AvailableAssignees { get; set; } = new();

    public async Task<IActionResult> OnGet(long? contactId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create Ticket";
        ViewData["ActiveMenu"] = "Tickets";

        // Pre-populate contact if provided
        if (contactId.HasValue)
        {
            Form.ContactId = contactId.Value;
        }

        await LoadSelectListsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(cancellationToken);
            return Page();
        }

        var command = new CreateTicket.Command
        {
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
            SetSuccessMessage($"Ticket #{response.Result} created successfully.");
            return RedirectToPage("./Details", new { id = response.Result });
        }

        SetErrorMessage(response.GetErrors());
        await LoadSelectListsAsync(cancellationToken);
        return Page();
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

    public record CreateTicketViewModel
    {
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

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

