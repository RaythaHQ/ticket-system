using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages;

public class Index : BaseStaffPageModel
{
    public IActionResult OnGet()
    {
        return Redirect("/staff/dashboard");
    }
}
