using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using App.Application.Webhooks.Commands;
using App.Application.Webhooks.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Web.Areas.Admin.Pages.Webhooks;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; } = new();

    public SelectList TriggerTypes { get; set; } = null!;

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetWebhookById.Query { Id = id });

        if (response.Result == null)
        {
            SetErrorMessage("Webhook not found.");
            return RedirectToPage(RouteNames.Webhooks.Index);
        }

        var webhook = response.Result;

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Settings",
                RouteName = RouteNames.Configuration.Index,
                IsActive = false,
                Icon = SidebarIcons.Settings,
            },
            new BreadcrumbNode
            {
                Label = "Webhooks",
                RouteName = RouteNames.Webhooks.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = webhook.Name,
                RouteName = RouteNames.Webhooks.Edit,
                IsActive = true,
            }
        );

        Form = new FormModel
        {
            Id = webhook.Id,
            Name = webhook.Name,
            Url = webhook.Url,
            TriggerType = webhook.TriggerType,
            Description = webhook.Description,
            IsActive = webhook.IsActive,
        };

        PopulateTriggerTypes();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new UpdateWebhook.Command
        {
            Id = Form.Id,
            Name = Form.Name,
            Url = Form.Url,
            TriggerType = Form.TriggerType,
            Description = Form.Description,
            IsActive = Form.IsActive,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Webhook was updated successfully.");
            return RedirectToPage(RouteNames.Webhooks.Edit, new { id = Form.Id });
        }

        SetErrorMessage("There was an error updating the webhook.", response.GetErrors());
        PopulateTriggerTypes();
        return Page();
    }

    public async Task<IActionResult> OnPostTest()
    {
        var response = await Mediator.Send(new TestWebhook.Command { Id = Form.Id });

        if (response.Success)
        {
            SetSuccessMessage(
                "Test webhook has been queued for delivery. Check the logs to see the result."
            );
        }
        else
        {
            SetErrorMessage("Failed to queue test webhook.", response.GetErrors());
        }

        return RedirectToPage(RouteNames.Webhooks.Edit, new { id = Form.Id });
    }

    private void PopulateTriggerTypes()
    {
        var types = new OrderedDictionary();
        foreach (var t in WebhookTriggerType.SupportedTypes)
        {
            types.Add(t.DeveloperName, t.Label);
        }
        TriggerTypes = new SelectList(types, "Key", "Value");
    }

    public record FormModel
    {
        public ShortGuid Id { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "URL")]
        public string Url { get; set; } = string.Empty;

        [Display(Name = "Trigger")]
        public string TriggerType { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }
}
