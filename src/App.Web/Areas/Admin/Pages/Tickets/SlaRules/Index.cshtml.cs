using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.SlaRules;
using App.Application.SlaRules.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.SlaRules;

/// <summary>
/// Page model for displaying a list of SLA rules.
/// </summary>
public class Index : BaseAdminPageModel, IHasListView<Index.SlaRuleListItemViewModel>
{
    private readonly ITicketPermissionService _permissionService;

    public Index(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public ListViewModel<SlaRuleListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<SlaRuleListItemViewModel>(), 0);

    public bool CanManageTickets { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"Priority {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "SLA Rules",
                RouteName = RouteNames.SlaRules.Index,
                IsActive = true,
            }
        );

        CanManageTickets = _permissionService.CanManageTickets();

        var input = new GetSlaRules.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input, cancellationToken);

        var items = response.Result.Items.Select(r => new SlaRuleListItemViewModel
        {
            Id = r.Id.ToString(),
            Name = r.Name,
            Description = r.Description ?? "-",
            TargetResolutionTime = r.ResolutionTimeLabel,
            BusinessHours = r.BusinessHoursEnabled ? "Yes" : "No",
            Priority = r.Priority,
            IsActive = r.IsActive,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                r.CreationTime
            ),
        });

        ListView = new ListViewModel<SlaRuleListItemViewModel>(items, response.Result.TotalCount);

        return Page();
    }

    public record SlaRuleListItemViewModel
    {
        public string Id { get; init; } = string.Empty;

        [Display(Name = "Name")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Target Resolution")]
        public string TargetResolutionTime { get; init; } = string.Empty;

        [Display(Name = "Business Hours")]
        public string BusinessHours { get; init; } = string.Empty;

        [Display(Name = "Priority")]
        public int Priority { get; init; }

        public bool IsActive { get; init; }

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;
    }
}
