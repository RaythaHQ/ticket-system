using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using App.Application.Webhooks.Commands;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Web.Areas.Admin.Pages.Webhooks;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; } = new();

    public SelectList TriggerTypes { get; set; } = null!;

    public async Task<IActionResult> OnGet()
    {
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
                Label = "Create webhook",
                RouteName = RouteNames.Webhooks.Create,
                IsActive = true,
            }
        );

        PopulateTriggerTypes();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateWebhook.Command
        {
            Name = Form.Name,
            Url = Form.Url,
            TriggerType = Form.TriggerType,
            Description = Form.Description,
            IsActive = Form.IsActive,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Webhook was created successfully.");
            return RedirectToPage(RouteNames.Webhooks.Edit, new { id = response.Result });
        }

        SetErrorMessage("There was an error creating the webhook.", response.GetErrors());
        PopulateTriggerTypes();
        return Page();
    }

    private void PopulateTriggerTypes()
    {
        var types = new OrderedDictionary { { "", "-- SELECT --" } };
        foreach (var t in WebhookTriggerType.SupportedTypes)
        {
            types.Add(t.DeveloperName, t.Label);
        }
        TriggerTypes = new SelectList(types, "Key", "Value");
    }

    public record FormModel
    {
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "URL")]
        public string Url { get; set; } = string.Empty;

        [Display(Name = "Trigger")]
        public string TriggerType { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
