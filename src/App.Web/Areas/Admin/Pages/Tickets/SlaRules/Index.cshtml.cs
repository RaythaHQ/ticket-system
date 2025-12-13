using App.Application.SlaRules;
using App.Application.SlaRules.Commands;
using App.Application.SlaRules.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.SlaRules;

/// <summary>
/// Page model for displaying a list of SLA rules.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Index : BaseAdminPageModel
{
    public List<SlaRuleDto> SlaRules { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "SLA Rules",
                RouteName = RouteNames.SlaRules.Index,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(
            new GetSlaRules.Query
            {
                OrderBy = $"Priority {SortOrder.ASCENDING}",
                PageSize = 1000, // Get all for drag/drop
            },
            cancellationToken
        );

        SlaRules = response.Result.Items.ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostReorder(
        [FromBody] ReorderRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.OrderedIds == null || !request.OrderedIds.Any())
        {
            return BadRequest("No IDs provided");
        }

        var orderedIds = request.OrderedIds.Select(id => ((ShortGuid)id).Guid).ToList();
        var response = await Mediator.Send(
            new ReorderSlaRules.Command { OrderedRuleIds = orderedIds },
            cancellationToken
        );

        if (response.Success)
        {
            return new JsonResult(new { success = true });
        }

        return BadRequest(response.GetErrors());
    }

    public async Task<IActionResult> OnPostToggleActive(
        [FromBody] ToggleActiveRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await Mediator.Send(
            new ToggleSlaRuleActive.Command { Id = request.Id, IsActive = request.IsActive },
            cancellationToken
        );

        if (response.Success)
        {
            return new JsonResult(new { success = true });
        }

        return BadRequest(response.GetErrors());
    }

    public class ReorderRequest
    {
        public List<string> OrderedIds { get; set; } = new();
    }

    public class ToggleActiveRequest
    {
        public ShortGuid Id { get; set; }
        public bool IsActive { get; set; }
    }
}
