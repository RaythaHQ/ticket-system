using App.Application.TicketConfig.Commands;
using App.Application.TicketConfig.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.TaskTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Index : BaseAdminPageModel
{
    public List<TaskTemplateListItemDto> Templates { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Task Templates";
        ViewData["ActiveMenu"] = "TaskTemplates";
        ViewData["ExpandTasksMenu"] = true;

        SetBreadcrumbs(new BreadcrumbNode
        {
            Label = "Task Templates",
            RouteName = RouteNames.TaskTemplates.Index,
            IsActive = true,
        });

        var response = await Mediator.Send(new GetTaskTemplates.Query(), cancellationToken);
        Templates = response.Result.Items.ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostToggleActive(
        [FromBody] ToggleActiveRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new ToggleTaskTemplateActive.Command
        {
            Id = request.Id,
            IsActive = request.IsActive
        }, cancellationToken);

        if (response.Success)
            return new JsonResult(new { success = true });

        return BadRequest(response.GetErrors());
    }

    public async Task<IActionResult> OnPostDelete(
        [FromBody] DeleteRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new DeleteTaskTemplate.Command
        {
            Id = request.Id
        }, cancellationToken);

        if (response.Success)
            return new JsonResult(new { success = true });

        return BadRequest(response.GetErrors());
    }

    public class ToggleActiveRequest
    {
        public ShortGuid Id { get; set; }
        public bool IsActive { get; set; }
    }

    public class DeleteRequest
    {
        public ShortGuid Id { get; set; }
    }
}
