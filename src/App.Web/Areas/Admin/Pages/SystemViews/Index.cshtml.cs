using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.TicketViews;
using App.Application.TicketViews.Queries;
using App.Application.TicketViews.Commands;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;

namespace App.Web.Areas.Admin.Pages.SystemViews;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_VIEWS_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.SystemViewListItemViewModel>
{
    public ListViewModel<SystemViewListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<SystemViewListItemViewModel>(), 0);

    public async Task<IActionResult> OnGet(
        string search = "",
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "System Views";
        ViewData["ActiveMenu"] = "SystemViews";
        ViewData["ExpandTicketingMenu"] = true;

        var response = await Mediator.Send(new GetTicketViews.Query
        {
            IncludeSystem = true
        }, cancellationToken);

        var systemViews = response.Result
            .Where(v => v.IsSystem)
            .Select(v => new SystemViewListItemViewModel
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description ?? "-",
                FilterCount = v.Conditions?.Filters?.Count() ?? 0,
                ColumnCount = v.VisibleColumns?.Count ?? 0,
                IsDefault = v.IsDefault,
                CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(v.CreationTime)
            });

        if (!string.IsNullOrWhiteSpace(search))
        {
            systemViews = systemViews.Where(v => 
                v.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                v.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var items = systemViews.ToList();
        ListView = new ListViewModel<SystemViewListItemViewModel>(items, items.Count);

        return Page();
    }

    public async Task<IActionResult> OnPostDelete(ShortGuid viewId, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new DeleteTicketView.Command { Id = viewId }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("System view deleted successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage("./Index");
    }

    public record SystemViewListItemViewModel
    {
        public ShortGuid Id { get; init; }

        [Display(Name = "Name")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Filters")]
        public int FilterCount { get; init; }

        [Display(Name = "Columns")]
        public int ColumnCount { get; init; }

        public bool IsDefault { get; init; }

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;
    }
}

