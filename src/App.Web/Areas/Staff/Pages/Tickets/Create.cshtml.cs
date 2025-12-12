using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for creating a new ticket.
/// </summary>
public class Create : BaseStaffPageModel
{
    [BindProperty]
    public CreateTicketViewModel Form { get; set; } = new();

    public List<AssigneeSelectItem> AvailableAssignees { get; set; } = new();

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
            return RedirectToPage(RouteNames.Tickets.Details, new { id = response.Result });
        }

        SetErrorMessage(response.GetErrors());
        await LoadSelectListsAsync(cancellationToken);
        return Page();
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

