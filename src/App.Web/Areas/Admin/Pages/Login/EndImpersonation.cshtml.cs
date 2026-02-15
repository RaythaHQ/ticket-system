using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Interfaces;
using App.Application.Common.Security;
using App.Application.Login;
using App.Application.Login.Commands;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;

namespace App.Web.Areas.Admin.Pages.Login;

/// <summary>
/// Page handler for ending an impersonation session.
/// Restores the original Super Admin's identity.
/// </summary>
[Authorize]
public class EndImpersonation : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost()
    {
        if (!CurrentUser.IsImpersonating)
        {
            SetErrorMessage("You are not currently impersonating anyone.");
            return RedirectToPage(RouteNames.Dashboard.Index);
        }

        var originalUserId = CurrentUser.OriginalUserId;
        if (originalUserId == null)
        {
            // This should never happen, but handle gracefully
            Logger.LogError("SECURITY: Impersonation session without original user ID detected. Logging out user.");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage(RouteNames.Login.LoginWithEmailAndPassword);
        }

        var impersonatedEmail = CurrentUser.EmailAddress;
        var impersonatedId = CurrentUser.UserId;
        var originalEmail = CurrentUser.OriginalUserEmail;

        var input = new App.Application.Login.Commands.EndImpersonation.Command
        {
            OriginalUserId = originalUserId.Value
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            // Restore the original admin's session
            await RestoreOriginalSession(response.Result);

            Logger.LogWarning(
                "SECURITY: Super Admin {OriginalEmail} (ID: {OriginalId}) ended impersonation of user {ImpersonatedEmail} (ID: {ImpersonatedId}) from IP: {IpAddress}",
                originalEmail,
                originalUserId,
                impersonatedEmail,
                impersonatedId,
                CurrentUser.RemoteIpAddress
            );

            SetSuccessMessage($"Impersonation ended. You are now logged in as {response.Result.FullName}.");
            return RedirectToPage(RouteNames.Dashboard.Index);
        }
        else
        {
            // If we can't restore the original session, log out for safety
            Logger.LogError(
                "SECURITY: Failed to restore original admin session after impersonation. Error: {Error}. Logging out user.",
                response.Error
            );
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            SetErrorMessage("Failed to restore your session. Please log in again.");
            return RedirectToPage(RouteNames.Login.LoginWithEmailAndPassword);
        }
    }

    private async Task RestoreOriginalSession(LoginDto result)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.Id.ToString()),
            new Claim(AppClaimTypes.LastModificationTime, result.LastModificationTime?.ToString() ?? DateTime.UtcNow.ToString()),
            // No impersonation claims - this is a normal session
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true }
        );
    }
}

