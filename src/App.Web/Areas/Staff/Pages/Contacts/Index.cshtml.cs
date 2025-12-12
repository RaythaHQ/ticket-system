using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Contacts;
using App.Application.Contacts.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;

namespace App.Web.Areas.Staff.Pages.Contacts;

/// <summary>
/// Page model for displaying a paginated list of contacts.
/// </summary>
public class Index : BaseStaffPageModel, IHasListView<Index.ContactListItemViewModel>
{
    /// <summary>
    /// Gets or sets the list view model containing paginated contact data.
    /// </summary>
    public ListViewModel<ContactListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<ContactListItemViewModel>(), 0);

    /// <summary>
    /// Handles GET requests to display the paginated list of contacts.
    /// </summary>
    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        ViewData["Title"] = "Contacts";
        ViewData["ActiveMenu"] = "Contacts";

        var input = new GetContacts.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await Mediator.Send(input, cancellationToken);

        var items = response.Result.Items.Select(p => new ContactListItemViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Email = p.Email ?? "-",
            PrimaryPhone = p.PrimaryPhone ?? "-",
            OrganizationAccount = p.OrganizationAccount ?? "-",
            TicketCount = p.TicketCount,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.CreationTime)
        });

        ListView = new ListViewModel<ContactListItemViewModel>(items, response.Result.TotalCount)
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Page();
    }

    /// <summary>
    /// View model for a single contact in the list.
    /// </summary>
    public record ContactListItemViewModel
    {
        public long Id { get; init; }

        [Display(Name = "Name")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; init; } = string.Empty;

        [Display(Name = "Phone")]
        public string PrimaryPhone { get; init; } = string.Empty;

        [Display(Name = "Organization")]
        public string OrganizationAccount { get; init; } = string.Empty;

        [Display(Name = "Tickets")]
        public int TicketCount { get; init; }

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;
    }
}

