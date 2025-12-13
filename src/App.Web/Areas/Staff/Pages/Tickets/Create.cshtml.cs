using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Application.Contacts.Queries;
using App.Application.Contacts.Commands;
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

    // Handler for contact search/lookup
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

    // Handler for getting contact by ID
    public async Task<IActionResult> OnGetContactById(long id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Mediator.Send(
                new GetContactById.Query { Id = id },
                cancellationToken
            );

            var contact = response.Result;
            return new JsonResult(new
            {
                success = true,
                contact = new
                {
                    id = contact.Id,
                    name = contact.Name,
                    email = contact.Email,
                    phoneNumbers = contact.PhoneNumbers,
                    primaryPhone = contact.PhoneNumbers.FirstOrDefault(),
                    address = contact.Address,
                    organizationAccount = contact.OrganizationAccount,
                    ticketCount = contact.TicketCount
                }
            });
        }
        catch
        {
            return new JsonResult(new { success = false, message = "Contact not found" });
        }
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        // If we need to create a new contact, do that first
        if (Form.CreateNewContact && !string.IsNullOrWhiteSpace(Form.NewContactName))
        {
            var phoneNumbers = new List<string>();
            if (!string.IsNullOrWhiteSpace(Form.NewContactPhone))
            {
                phoneNumbers.Add(Form.NewContactPhone);
            }

            var createContactResponse = await Mediator.Send(
                new CreateContact.Command
                {
                    Name = Form.NewContactName,
                    Email = Form.NewContactEmail,
                    PhoneNumbers = phoneNumbers
                },
                cancellationToken
            );

            if (createContactResponse.Success)
            {
                Form.ContactId = createContactResponse.Result;
            }
            else
            {
                SetErrorMessage(createContactResponse.GetErrors());
                await LoadSelectListsAsync(cancellationToken);
                return Page();
            }
        }

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

        // New contact creation fields
        public bool CreateNewContact { get; set; }

        public bool SkipContact { get; set; }

        [MaxLength(500)]
        public string? NewContactName { get; set; }

        [EmailAddress]
        [MaxLength(500)]
        public string? NewContactEmail { get; set; }

        [MaxLength(50)]
        public string? NewContactPhone { get; set; }
    }

    public record AssigneeSelectItem
    {
        public string Value { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
        public ShortGuid? TeamId { get; init; }
        public ShortGuid? AssigneeId { get; init; }
    }
}

