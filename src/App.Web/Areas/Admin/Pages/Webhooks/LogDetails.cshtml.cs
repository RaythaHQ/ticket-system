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
public class LogDetails : BaseAdminPageModel
{
    public LogViewModel Log { get; set; } = null!;

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetWebhookLogById.Query { Id = id });

        if (response.Result == null)
        {
            SetErrorMessage("Webhook log not found.");
            return RedirectToPage(RouteNames.Webhooks.Logs);
        }

        var log = response.Result;

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
                Label = "Logs",
                RouteName = RouteNames.Webhooks.Logs,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Details",
                RouteName = RouteNames.Webhooks.LogDetails,
                IsActive = true,
            }
        );

        Log = new LogViewModel
        {
            Id = log.Id,
            WebhookId = log.WebhookId,
            WebhookName = log.WebhookName,
            TicketId = log.TicketId,
            TriggerType = log.TriggerType,
            TriggerTypeLabel = log.TriggerTypeLabel,
            PayloadJson = FormatJson(log.PayloadJson),
            AttemptCount = log.AttemptCount,
            Success = log.Success,
            HttpStatusCode = log.HttpStatusCode,
            ErrorMessage = log.ErrorMessage,
            ResponseBody = log.ResponseBody,
            CreatedAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                log.CreatedAt
            ),
            CompletedAt = log.CompletedAt.HasValue
                ? CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    log.CompletedAt.Value
                )
                : null,
            Duration = log.Duration?.TotalMilliseconds.ToString("F0") + "ms",
        };

        return Page();
    }

    public async Task<IActionResult> OnPostRetry(string id)
    {
        var response = await Mediator.Send(new RetryWebhookDelivery.Command { LogId = id });

        if (response.Success)
        {
            SetSuccessMessage(
                "Webhook retry has been queued. Check the logs for the new delivery attempt."
            );
        }
        else
        {
            SetErrorMessage("Failed to queue retry.", response.GetErrors());
        }

        return RedirectToPage(RouteNames.Webhooks.Logs);
    }

    private static string FormatJson(string json)
    {
        try
        {
            var obj = System.Text.Json.JsonSerializer.Deserialize<object>(json);
            return System.Text.Json.JsonSerializer.Serialize(
                obj,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch
        {
            return json;
        }
    }

    public record LogViewModel
    {
        public ShortGuid Id { get; init; }
        public ShortGuid WebhookId { get; init; }
        public string WebhookName { get; init; } = null!;
        public long? TicketId { get; init; }
        public string TriggerType { get; init; } = null!;
        public string TriggerTypeLabel { get; init; } = null!;
        public string PayloadJson { get; init; } = null!;
        public int AttemptCount { get; init; }
        public bool Success { get; init; }
        public int? HttpStatusCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? ResponseBody { get; init; }
        public string CreatedAt { get; init; } = null!;
        public string? CompletedAt { get; init; }
        public string? Duration { get; init; }
    }
}
