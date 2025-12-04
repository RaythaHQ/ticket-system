using App.Web.Areas.Public.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Public.Pages.Login;

public class LoginRedirect : BasePublicLoginPageModel
{
    public async Task<IActionResult> OnGet(string returnUrl = null)
    {
        return RedirectToPage(
            RouteNames.Login.LoginWithEmailAndPassword,
            new { area = "Public", returnUrl }
        );
    }
}
