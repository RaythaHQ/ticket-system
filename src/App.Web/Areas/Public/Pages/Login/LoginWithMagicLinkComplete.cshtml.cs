using App.Web.Areas.Public.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Public.Pages.Login;

public class LoginWithMagicLinkComplete : BasePublicLoginPageModel
{
    /// <summary>
    /// The magic link token from the email.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    /// <summary>
    /// Optional return URL after login.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// GET handler - displays the intermediary page without consuming the token.
    /// This prevents email client prefetch from using up one-time tokens.
    /// </summary>
    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Token))
        {
            SetErrorMessage("Login token is missing.");
            return RedirectToPage(RouteNames.Login.LoginWithMagicLink);
        }

        // Just display the page - don't consume the token yet
        return Page();
    }

    /// <summary>
    /// POST handler - actually consumes the token and completes the login.
    /// Only triggered by user clicking the button.
    /// </summary>
    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrEmpty(Token))
        {
            SetErrorMessage("Login token is missing.");
            return RedirectToPage(RouteNames.Login.LoginWithMagicLink);
        }

        var response = await Mediator.Send(
            new App.Application.Login.Commands.CompleteLoginWithMagicLink.Command { Id = Token }
        );

        if (response.Success)
        {
            await LoginWithClaims(response.Result, true);
            if (HasLocalRedirect(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }
            else
            {
                return RedirectToPage(RouteNames.Dashboard.Index);
            }
        }
        else
        {
            SetErrorMessage(response.Error);
            return RedirectToPage(
                RouteNames.Login.LoginWithMagicLink,
                new { returnUrl = ReturnUrl }
            );
        }
    }
}
