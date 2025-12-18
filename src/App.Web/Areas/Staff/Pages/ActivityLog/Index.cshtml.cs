using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.ActivityLog;

/// <summary>
/// Activity Log page showing real-time activity stream via SignalR.
/// </summary>
public class Index : BaseStaffPageModel
{
    public IActionResult OnGet()
    {
        ViewData["Title"] = "Activity Log";
        ViewData["ActiveMenu"] = "ActivityLog";

        return Page();
    }
}
