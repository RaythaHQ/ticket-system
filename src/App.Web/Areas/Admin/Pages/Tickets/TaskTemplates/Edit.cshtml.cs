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
public class Edit : BaseAdminPageModel
{
    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public string ItemsJson { get; set; } = "[]";

    public TaskTemplateDetailDto? Template { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Task Template";
        ViewData["ActiveMenu"] = "TaskTemplates";
        ViewData["ExpandTasksMenu"] = true;

        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Task Templates", RouteName = RouteNames.TaskTemplates.Index },
            new BreadcrumbNode { Label = "Edit", RouteName = RouteNames.TaskTemplates.Edit, IsActive = true }
        );

        var response = await Mediator.Send(
            new GetTaskTemplateById.Query { Id = Id },
            cancellationToken);

        if (!response.Success)
        {
            SetErrorMessage("Template not found.");
            return RedirectToPage(RouteNames.TaskTemplates.Index);
        }

        Template = response.Result;
        Name = Template.Name;
        Description = Template.Description;

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Task Template";
        ViewData["ActiveMenu"] = "TaskTemplates";
        ViewData["ExpandTasksMenu"] = true;

        var items = System.Text.Json.JsonSerializer.Deserialize<List<ItemInput>>(
            ItemsJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        var command = new UpdateTaskTemplate.Command
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Items = items.Select((item, index) => new UpdateTaskTemplate.TemplateItemInput
            {
                Title = item.Title,
                SortOrder = index + 1,
                DependsOnIndex = item.DependsOnIndex,
            }).ToList(),
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Task template updated successfully.");
            return RedirectToPage(RouteNames.TaskTemplates.Index);
        }

        SetErrorMessage(response.Error);
        return Page();
    }

    public class ItemInput
    {
        public string Title { get; set; } = string.Empty;
        public int? DependsOnIndex { get; set; }
    }
}
