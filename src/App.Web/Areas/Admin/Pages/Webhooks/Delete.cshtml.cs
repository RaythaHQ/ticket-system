using App.Application.Webhooks.Commands;
using App.Application.Webhooks.Queries;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Webhooks;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public WebhookViewModel Webhook { get; set; } = null!;

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
                Label = "Delete",
                RouteName = RouteNames.Webhooks.Delete,
                IsActive = true,
            }
        );

        Webhook = new WebhookViewModel
        {
            Id = webhook.Id,
            Name = webhook.Name,
            Url = webhook.Url,
            TriggerTypeLabel = webhook.TriggerTypeLabel,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var response = await Mediator.Send(new DeleteWebhook.Command { Id = id });

        if (response.Success)
        {
            SetSuccessMessage("Webhook was deleted successfully.");
            return RedirectToPage(RouteNames.Webhooks.Index);
        }

        SetErrorMessage("There was an error deleting the webhook.", response.GetErrors());
        return RedirectToPage(RouteNames.Webhooks.Index);
    }

    public record WebhookViewModel
    {
        public ShortGuid Id { get; init; }
        public string Name { get; init; } = null!;
        public string Url { get; init; } = null!;
        public string TriggerTypeLabel { get; init; } = null!;
    }
}
