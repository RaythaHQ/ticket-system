using App.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace App.Web.Filters;

/// <summary>
/// Page filter that restricts access to /staff/scheduler pages to active scheduler staff members.
/// Non-scheduler staff are redirected to the ticket dashboard.
/// </summary>
public class SchedulerStaffAccessFilter : IAsyncPageFilter
{
    private readonly ISchedulerPermissionService _schedulerPermissions;

    public SchedulerStaffAccessFilter(ISchedulerPermissionService schedulerPermissions)
    {
        _schedulerPermissions = schedulerPermissions;
    }

    public async Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) { }

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next
    )
    {
        // Only apply to pages under /staff/scheduler
        var path = context.HttpContext.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/staff/scheduler"))
        {
            var isStaff = await _schedulerPermissions.IsSchedulerStaffAsync(
                context.HttpContext.RequestAborted
            );

            if (!isStaff)
            {
                // Redirect non-scheduler staff to the ticket dashboard
                context.Result = new RedirectToPageResult(
                    "/Dashboard/Index",
                    null,
                    new { area = "Staff" }
                );
                return;
            }
        }

        await next();
    }
}
