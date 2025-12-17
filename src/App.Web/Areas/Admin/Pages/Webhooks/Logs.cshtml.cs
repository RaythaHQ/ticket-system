using System.ComponentModel.DataAnnotations;
using App.Application.Common.Utils;
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
public class Logs : BaseAdminPageModel, IHasListView<Logs.LogListItemViewModel>
{
    public ListViewModel<LogListItemViewModel> ListView { get; set; } = null!;
    public SelectList WebhookOptions { get; set; } = null!;
    public SelectList TriggerTypeOptions { get; set; } = null!;
    public SelectList StatusOptions { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string? WebhookId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TriggerType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public async Task<IActionResult> OnGet(int pageNumber = 1, int pageSize = 50)
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
                Label = "Delivery Logs",
                RouteName = RouteNames.Webhooks.Logs,
                IsActive = true,
            }
        );

        await PopulateFilters();

        bool? successFilter = Status switch
        {
            "success" => true,
            "failed" => false,
            _ => null,
        };

        var input = new GetWebhookLogs.Query
        {
            WebhookId = string.IsNullOrEmpty(WebhookId) ? null : (ShortGuid)WebhookId,
            TriggerType = TriggerType,
            Success = successFilter,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new LogListItemViewModel
        {
            Id = p.Id,
            WebhookName = p.WebhookName,
            TicketId = p.TicketId,
            TriggerTypeLabel = p.TriggerTypeLabel,
            Success = p.Success,
            HttpStatusCode = p.HttpStatusCode,
            AttemptCount = p.AttemptCount,
            CreatedAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreatedAt
            ),
            Duration = p.Duration?.TotalMilliseconds.ToString("F0") + "ms",
        });

        ListView = new ListViewModel<LogListItemViewModel>(items, response.Result.TotalCount);

        return Page();
    }

    private async Task PopulateFilters()
    {
        // Webhook dropdown
        var webhooksResponse = await Mediator.Send(new GetWebhooksForDropdown.Query());
        var webhookItems = new List<SelectListItem> { new SelectListItem("All Webhooks", "") };
        webhookItems.AddRange(
            webhooksResponse.Result.Select(w => new SelectListItem(w.Label, w.Value))
        );
        WebhookOptions = new SelectList(webhookItems, "Value", "Text", WebhookId);

        // Trigger type dropdown
        var triggerItems = new List<SelectListItem> { new SelectListItem("All Triggers", "") };
        triggerItems.AddRange(
            WebhookTriggerType.SupportedTypes.Select(t => new SelectListItem(
                t.Label,
                t.DeveloperName
            ))
        );
        TriggerTypeOptions = new SelectList(triggerItems, "Value", "Text", TriggerType);

        // Status dropdown
        StatusOptions = new SelectList(
            new[]
            {
                new SelectListItem("All Statuses", ""),
                new SelectListItem("Success", "success"),
                new SelectListItem("Failed", "failed"),
            },
            "Value",
            "Text",
            Status
        );
    }

    public record LogListItemViewModel
    {
        public string Id { get; init; } = null!;

        [Display(Name = "Webhook")]
        public string WebhookName { get; init; } = null!;

        [Display(Name = "Ticket")]
        public long? TicketId { get; init; }

        [Display(Name = "Trigger")]
        public string TriggerTypeLabel { get; init; } = null!;

        [Display(Name = "Status")]
        public bool Success { get; init; }

        [Display(Name = "HTTP Status")]
        public int? HttpStatusCode { get; init; }

        [Display(Name = "Attempts")]
        public int AttemptCount { get; init; }

        [Display(Name = "Created")]
        public string CreatedAt { get; init; } = null!;

        [Display(Name = "Duration")]
        public string? Duration { get; init; }
    }
}
