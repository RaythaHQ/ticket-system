using System.ComponentModel.DataAnnotations;
using App.Application.Contacts;
using App.Application.Contacts.Queries;
using App.Application.Tickets;
using App.Application.Tickets.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Dashboard;

public class Index : BaseStaffPageModel
{
    public UserDashboardMetricsDto Metrics { get; set; } = null!;
    public ShortGuid? UserId { get; set; }

    [BindProperty]
    public QuickLookupTicketIdViewModel QuickLookupTicketId { get; set; } = new();

    [BindProperty]
    public QuickLookupContactIdViewModel QuickLookupContactId { get; set; } = new();

    [BindProperty]
    public QuickSearchTicketsViewModel QuickSearchTickets { get; set; } = new();

    [BindProperty]
    public QuickSearchContactsViewModel QuickSearchContacts { get; set; } = new();

    public List<AssigneeSelectItem> AvailableAssignees { get; set; } = new();
    public List<AssigneeSelectItem> AvailableCreatedByUsers { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";
        ViewData["ActiveMenu"] = "Dashboard";

        UserId = CurrentUser.UserId;
        if (!UserId.HasValue)
            return RedirectToPage(RouteNames.Error.Index);

        var response = await Mediator.Send(
            new GetUserDashboardMetrics.Query { UserId = UserId.Value },
            cancellationToken
        );
        Metrics = response.Result;

        await LoadSelectListsAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostQuickLookupTicketId(CancellationToken cancellationToken)
    {
        if (!QuickLookupTicketId.TicketId.HasValue)
        {
            SetErrorMessage("Please enter a Ticket ID.");
            await LoadSelectListsAsync(cancellationToken);
            return await OnGet(cancellationToken);
        }

        try
        {
            var response = await Mediator.Send(
                new GetTicketById.Query { Id = QuickLookupTicketId.TicketId.Value },
                cancellationToken
            );

            return RedirectToPage(
                RouteNames.Tickets.Details,
                new { id = QuickLookupTicketId.TicketId.Value }
            );
        }
        catch (App.Application.Common.Exceptions.NotFoundException)
        {
            SetErrorMessage($"Ticket #{QuickLookupTicketId.TicketId.Value} not found.");
            await LoadSelectListsAsync(cancellationToken);
            return await OnGet(cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostQuickLookupContactId(CancellationToken cancellationToken)
    {
        if (!QuickLookupContactId.ContactId.HasValue)
        {
            SetErrorMessage("Please enter a Contact ID.");
            await LoadSelectListsAsync(cancellationToken);
            return await OnGet(cancellationToken);
        }

        try
        {
            var response = await Mediator.Send(
                new GetContactById.Query { Id = QuickLookupContactId.ContactId.Value },
                cancellationToken
            );

            return RedirectToPage(
                RouteNames.Contacts.Details,
                new { id = QuickLookupContactId.ContactId.Value }
            );
        }
        catch (App.Application.Common.Exceptions.NotFoundException)
        {
            SetErrorMessage($"Contact #{QuickLookupContactId.ContactId.Value} not found.");
            await LoadSelectListsAsync(cancellationToken);
            return await OnGet(cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostQuickSearchTickets(CancellationToken cancellationToken)
    {
        if (!HasAnyTicketSearchCriteria())
        {
            SetErrorMessage("Please provide at least one search criterion.");
            return await OnGet(cancellationToken);
        }

        // Build query parameters for redirect to Tickets Index
        var queryParams = new Dictionary<string, string?> { { "builtInView", "all" } };

        // Add search term (combine TicketTitle with other text searches)
        var searchTerms = new List<string>();
        if (!string.IsNullOrWhiteSpace(QuickSearchTickets.TicketTitle))
        {
            searchTerms.Add(QuickSearchTickets.TicketTitle);
        }

        // For ContactPhone, we need to find matching contact IDs first
        if (!string.IsNullOrWhiteSpace(QuickSearchTickets.ContactPhone))
        {
            var contactSearchResponse = await Mediator.Send(
                new App.Application.Contacts.Queries.QuickSearchContacts.Query
                {
                    Phone = QuickSearchTickets.ContactPhone,
                },
                cancellationToken
            );

            var matchingContactIds = contactSearchResponse.Result.Select(c => c.Id).ToList();
            if (matchingContactIds.Any())
            {
                // Use the phone-matched contact ID (only if explicit ContactId not provided)
                if (!QuickSearchTickets.ContactId.HasValue)
                {
                    // If multiple matches, use the first one
                    queryParams["contactId"] = matchingContactIds.First().ToString();
                }
            }
            else
            {
                // No contacts found with that phone number
                SetWarningMessage(
                    $"No contacts found with phone number containing '{QuickSearchTickets.ContactPhone}'."
                );
                return await OnGet(cancellationToken);
            }
        }

        // Add explicit ContactId if provided (overrides phone-matched contact)
        if (QuickSearchTickets.ContactId.HasValue)
        {
            queryParams["contactId"] = QuickSearchTickets.ContactId.Value.ToString();
        }

        // Add AssigneeId if provided
        if (QuickSearchTickets.AssigneeId.HasValue)
        {
            queryParams["assigneeId"] = QuickSearchTickets.AssigneeId.Value.ToString();
        }

        // Add CreatedById if provided
        if (QuickSearchTickets.CreatedById.HasValue)
        {
            queryParams["createdById"] = QuickSearchTickets.CreatedById.Value.ToString();
        }

        // Combine search terms
        if (searchTerms.Any())
        {
            queryParams["search"] = string.Join(" ", searchTerms);
        }

        return RedirectToPage(RouteNames.Tickets.Index, queryParams);
    }

    public async Task<IActionResult> OnPostQuickSearchContacts(CancellationToken cancellationToken)
    {
        if (!HasAnyContactSearchCriteria())
        {
            SetErrorMessage("Please provide at least one search criterion.");
            return await OnGet(cancellationToken);
        }

        // Build search terms for Contacts Index
        var searchTerms = new List<string>();

        if (!string.IsNullOrWhiteSpace(QuickSearchContacts.FirstName))
        {
            searchTerms.Add(QuickSearchContacts.FirstName);
        }

        if (!string.IsNullOrWhiteSpace(QuickSearchContacts.LastName))
        {
            searchTerms.Add(QuickSearchContacts.LastName);
        }

        // For phone, we can include it in the search - GetContacts query supports phone search
        if (!string.IsNullOrWhiteSpace(QuickSearchContacts.Phone))
        {
            searchTerms.Add(QuickSearchContacts.Phone);
        }

        // Build query parameters for redirect to Contacts Index
        var queryParams = new Dictionary<string, string?>();

        if (searchTerms.Any())
        {
            queryParams["search"] = string.Join(" ", searchTerms);
        }

        return RedirectToPage(RouteNames.Contacts.Index, queryParams);
    }

    private bool HasAnyTicketSearchCriteria()
    {
        return QuickSearchTickets.ContactId.HasValue
            || !string.IsNullOrWhiteSpace(QuickSearchTickets.ContactPhone)
            || !string.IsNullOrWhiteSpace(QuickSearchTickets.TicketTitle)
            || QuickSearchTickets.AssigneeId.HasValue
            || QuickSearchTickets.CreatedById.HasValue;
    }

    private bool HasAnyContactSearchCriteria()
    {
        return !string.IsNullOrWhiteSpace(QuickSearchContacts.FirstName)
            || !string.IsNullOrWhiteSpace(QuickSearchContacts.LastName)
            || !string.IsNullOrWhiteSpace(QuickSearchContacts.Phone);
    }

    private async Task LoadSelectListsAsync(CancellationToken cancellationToken)
    {
        var canManageTickets = TicketPermissionService.CanManageTickets();

        var assigneeOptionsResponse = await Mediator.Send(
            new GetAssigneeSelectOptions.Query
            {
                CanManageTickets = canManageTickets,
                CurrentUserId = CurrentUser.UserId?.Guid,
            },
            cancellationToken
        );

        AvailableAssignees = assigneeOptionsResponse
            .Result.Where(a => a.AssigneeId.HasValue) // Only individual users, not teams
            .Select(a => new AssigneeSelectItem
            {
                Value = a.Value,
                DisplayText = a.DisplayText,
                TeamId = a.TeamId,
                AssigneeId = a.AssigneeId,
            })
            .ToList();

        // Get all users for Created By dropdown - only admins, non-suspended
        var usersResponse = await Mediator.Send(
            new App.Application.Users.Queries.GetUsers.Query
            {
                PageSize = 1000,
                OrderBy = "FirstName ASC",
            },
            cancellationToken
        );

        AvailableCreatedByUsers = usersResponse
            .Result.Items.Where(u => u.IsActive && u.IsAdmin)
            .Select(u => new AssigneeSelectItem
            {
                Value = u.Id.ToString(),
                DisplayText = $"{u.FirstName} {u.LastName}",
                TeamId = null,
                AssigneeId = u.Id,
            })
            .ToList();
    }

    public record QuickLookupTicketIdViewModel
    {
        public long? TicketId { get; set; }
    }

    public record QuickLookupContactIdViewModel
    {
        public long? ContactId { get; set; }
    }

    public record QuickSearchTicketsViewModel
    {
        public long? ContactId { get; set; }
        public string? ContactPhone { get; set; }
        public string? TicketTitle { get; set; }
        public ShortGuid? AssigneeId { get; set; }
        public ShortGuid? CreatedById { get; set; }
    }

    public record QuickSearchContactsViewModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
    }

    public record AssigneeSelectItem
    {
        public string Value { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
        public ShortGuid? TeamId { get; init; }
        public ShortGuid? AssigneeId { get; init; }
    }
}
