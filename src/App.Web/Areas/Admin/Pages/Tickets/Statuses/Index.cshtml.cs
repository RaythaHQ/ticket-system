using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.TicketConfig;
using App.Application.TicketConfig.Queries;
using App.Application.TicketConfig.Commands;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Statuses;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Index : BaseAdminPageModel
{
    public List<TicketStatusConfigDto> Statuses { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Ticket Statuses",
                RouteName = RouteNames.TicketStatuses.Index,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetTicketStatuses.Query { IncludeInactive = true }, cancellationToken);
        Statuses = response.Result.Items.ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostReorder([FromBody] ReorderRequest request, CancellationToken cancellationToken)
    {
        if (request.OrderedIds == null || !request.OrderedIds.Any())
        {
            return BadRequest("No IDs provided");
        }

        var orderedIds = request.OrderedIds.Select(id => (ShortGuid)id).ToList();
        var response = await Mediator.Send(new ReorderTicketStatuses.Command { OrderedIds = orderedIds }, cancellationToken);

        if (response.Success)
        {
            return new JsonResult(new { success = true });
        }

        return BadRequest(response.GetErrors());
    }

    public async Task<IActionResult> OnPostToggleActive([FromBody] ToggleActiveRequest request, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new ToggleTicketStatusActive.Command
        {
            Id = request.Id,
            IsActive = request.IsActive
        }, cancellationToken);

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

