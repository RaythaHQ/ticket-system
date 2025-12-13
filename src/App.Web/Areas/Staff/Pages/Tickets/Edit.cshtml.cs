using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Application.TicketConfig;
using App.Application.TicketConfig.Queries;
using App.Application.Contacts;
using App.Application.Contacts.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for editing a ticket with configurable statuses and priorities.
/// </summary>
public class Edit : BaseStaffPageModel
{
    public TicketDto Ticket { get; set; } = null!;

    [BindProperty]
    public EditTicketViewModel Form { get; set; } = new();

    public List<AssigneeSelectItem> AvailableAssignees { get; set; } = new();
    
    /// <summary>
    /// Available statuses from configuration, ordered by sort order.
    /// </summary>
    public IReadOnlyList<TicketStatusConfigDto> AvailableStatuses { get; set; } = new List<TicketStatusConfigDto>();
    
    /// <summary>
    /// Available priorities from configuration, ordered by sort order.
    /// </summary>
    public IReadOnlyList<TicketPriorityConfigDto> AvailablePriorities { get; set; } = new List<TicketPriorityConfigDto>();
    
    /// <summary>
    /// The current status configuration for this ticket.
    /// </summary>
    public TicketStatusConfigDto? CurrentStatus { get; set; }
    
    /// <summary>
    /// The current priority configuration for this ticket.
    /// </summary>
    public TicketPriorityConfigDto? CurrentPriority { get; set; }
    
    /// <summary>
    /// The selected contact for this ticket (if any).
    /// </summary>
    public ContactSummaryDto? SelectedContact { get; set; }

    public async Task<IActionResult> OnGet(long id, string? backToListUrl = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Edit Ticket";
        ViewData["ActiveMenu"] = "Tickets";
        ViewData["BackToListUrl"] = backToListUrl;

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
            Ticket = ticket;
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
        Ticket = ticket;
        await LoadSelectListsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostChangeStatus(long id, string status, CancellationToken cancellationToken)
    {
        var command = new ChangeTicketStatus.Command { Id = id, NewStatus = status };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            // Get the status label from config
            var statusesResponse = await Mediator.Send(new GetTicketStatuses.Query { IncludeInactive = false }, cancellationToken);
            var statusConfig = statusesResponse.Result.Items.FirstOrDefault(s => s.DeveloperName == status);
            SetSuccessMessage($"Status changed to {statusConfig?.Label ?? status}.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Edit, new { id });
    }

    public async Task<IActionResult> OnPostClose(long id, CancellationToken cancellationToken)
    {
        var command = new CloseTicket.Command { Id = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Ticket closed.");
            return RedirectToPage(RouteNames.Tickets.Details, new { id });
        }
        
        SetErrorMessage(response.GetErrors());
        return RedirectToPage(RouteNames.Tickets.Edit, new { id });
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

        return RedirectToPage(RouteNames.Tickets.Edit, new { id });
    }

    /// <summary>
    /// Handler for contact search/lookup.
    /// </summary>
    public async Task<IActionResult> OnGetSearchContact(string searchTerm, CancellationToken cancellationToken)
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
            ticketCount = c.TicketCount
        });

        return new JsonResult(new { results });
    }

    private async Task LoadSelectListsAsync(CancellationToken cancellationToken)
    {
        // Load assignees
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
        
        // Load statuses from configuration
        var statusesResponse = await Mediator.Send(new GetTicketStatuses.Query { IncludeInactive = false }, cancellationToken);
        AvailableStatuses = statusesResponse.Result.Items.ToList();
        CurrentStatus = AvailableStatuses.FirstOrDefault(s => s.DeveloperName == Form.Status);
        
        // Load priorities from configuration
        var prioritiesResponse = await Mediator.Send(new GetTicketPriorities.Query { IncludeInactive = false }, cancellationToken);
        AvailablePriorities = prioritiesResponse.Result.Items.ToList();
        CurrentPriority = AvailablePriorities.FirstOrDefault(p => p.DeveloperName == Form.Priority);
        
        // Load selected contact if there's a ContactId
        if (Form.ContactId.HasValue)
        {
            try
            {
                var contactResponse = await Mediator.Send(new GetContactById.Query { Id = Form.ContactId.Value }, cancellationToken);
                if (contactResponse.Result != null)
                {
                    SelectedContact = new ContactSummaryDto
                    {
                        Id = contactResponse.Result.Id,
                        Name = contactResponse.Result.Name,
                        Email = contactResponse.Result.Email,
                        Phone = contactResponse.Result.PhoneNumbers.FirstOrDefault()
                    };
                }
            }
            catch
            {
                // Contact not found, leave as null
            }
        }
    }

    public record EditTicketViewModel
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        [Required]
        public string Priority { get; set; } = string.Empty;

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
    
    public record ContactSummaryDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string? Phone { get; init; }
    }
}
