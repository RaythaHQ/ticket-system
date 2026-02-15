using App.Application.SchedulerAdmin.DTOs;
using App.Application.SchedulerAdmin.Queries;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Scheduler.EmailTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Index : BaseAdminPageModel
{
    public List<SchedulerEmailTemplateDto> Templates { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Scheduler Email Templates",
                RouteName = RouteNames.Scheduler.EmailTemplates.Index,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetSchedulerEmailTemplates.Query
        {
            Channel = "email",
        }, cancellationToken);

        Templates = response.Result.Items.ToList();

        return Page();
    }
}
