using App.Application.Common.Interfaces;
using App.Web.Areas.Public.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Public.Pages.Dashboard;

public class Index : BasePublicPageModel
{
    private readonly ICurrentUser _currentUser;

    public Index(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public IActionResult OnGet()
    {
        // If user is an admin, redirect to staff dashboard
        if (_currentUser.IsAdmin)
        {
            return Redirect("/staff/dashboard");
        }

        // Otherwise, show the public dashboard
        return Page();
    }
}
