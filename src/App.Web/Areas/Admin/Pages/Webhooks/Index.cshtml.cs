using System.ComponentModel.DataAnnotations;
using App.Application.Common.Utils;
using App.Application.Webhooks.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Webhooks;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.WebhookListItemViewModel>
{
    public ListViewModel<WebhookListItemViewModel> ListView { get; set; } = null!;

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"Name {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
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
                IsActive = true,
            }
        );

        var input = new GetWebhooks.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new WebhookListItemViewModel
        {
            Id = p.Id,
            Name = p.Name,
            TriggerTypeLabel = p.TriggerTypeLabel,
            Url = TruncateUrl(p.Url),
            IsActive = p.IsActive,
            LastModificationTime =
                CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    p.LastModificationTime
                ),
        });

        ListView = new ListViewModel<WebhookListItemViewModel>(items, response.Result.TotalCount);

        return Page();
    }

    private static string TruncateUrl(string url)
    {
        const int maxLength = 50;
        if (string.IsNullOrEmpty(url) || url.Length <= maxLength)
            return url;
        return url[..maxLength] + "...";
    }

    public record WebhookListItemViewModel
    {
        public string Id { get; init; } = null!;

        [Display(Name = "Name")]
        public string Name { get; init; } = null!;

        [Display(Name = "Trigger")]
        public string TriggerTypeLabel { get; init; } = null!;

        [Display(Name = "URL")]
        public string Url { get; init; } = null!;

        [Display(Name = "Active")]
        public bool IsActive { get; init; }

        [Display(Name = "Last modified")]
        public string? LastModificationTime { get; init; }
    }
}
