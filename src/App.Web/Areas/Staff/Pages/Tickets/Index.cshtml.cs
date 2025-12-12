using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets;
using App.Application.Tickets.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for displaying a paginated list of tickets.
/// </summary>
public class Index : BaseStaffPageModel, IHasListView<Index.TicketListItemViewModel>
{
    /// <summary>
    /// Gets or sets the list view model containing paginated ticket data.
    /// </summary>
    public ListViewModel<TicketListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<TicketListItemViewModel>(), 0);

    /// <summary>
    /// Handles GET requests to display the paginated list of tickets.
    /// </summary>
    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        string? status = null,
        string? priority = null,
        bool? unassigned = null,
        CancellationToken cancellationToken = default
    )
    {
        ViewData["Title"] = "Tickets";
        ViewData["ActiveMenu"] = "Tickets";

        var input = new GetTickets.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            Priority = priority,
            Unassigned = unassigned
        };

        var response = await Mediator.Send(input, cancellationToken);

        var items = response.Result.Items.Select(p => new TicketListItemViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Status = p.Status,
            StatusLabel = p.StatusLabel,
            Priority = p.Priority,
            PriorityLabel = p.PriorityLabel,
            Category = p.Category ?? "-",
            AssigneeName = p.AssigneeName ?? "Unassigned",
            OwningTeamName = p.OwningTeamName ?? "-",
            ContactName = p.ContactName ?? "-",
            SlaDueAt = p.SlaDueAt?.ToString("MMM dd, HH:mm") ?? "-",
            SlaStatusLabel = p.SlaStatusLabel ?? "-",
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.CreationTime)
        });

        ListView = new ListViewModel<TicketListItemViewModel>(items, response.Result.TotalCount);

        return Page();
    }

    /// <summary>
    /// View model for a single ticket in the list.
    /// </summary>
    public record TicketListItemViewModel
    {
        public long Id { get; init; }

        [Display(Name = "Title")]
        public string Title { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        [Display(Name = "Status")]
        public string StatusLabel { get; init; } = string.Empty;

        public string Priority { get; init; } = string.Empty;

        [Display(Name = "Priority")]
        public string PriorityLabel { get; init; } = string.Empty;

        [Display(Name = "Category")]
        public string Category { get; init; } = string.Empty;

        [Display(Name = "Assignee")]
        public string AssigneeName { get; init; } = string.Empty;

        [Display(Name = "Team")]
        public string OwningTeamName { get; init; } = string.Empty;

        [Display(Name = "Contact")]
        public string ContactName { get; init; } = string.Empty;

        [Display(Name = "SLA Due")]
        public string SlaDueAt { get; init; } = string.Empty;

        [Display(Name = "SLA Status")]
        public string SlaStatusLabel { get; init; } = string.Empty;

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;
    }
}

