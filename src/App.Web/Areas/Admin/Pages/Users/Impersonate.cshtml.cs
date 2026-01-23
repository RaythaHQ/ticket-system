using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Security;
using App.Application.Login;
using App.Application.Login.Commands;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;

namespace App.Web.Areas.Admin.Pages.Users;

/// <summary>
/// Page handler for impersonating a user.
/// Only accessible by Super Admins.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Impersonate : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        // Verify the current user is a Super Admin
        var isSuperAdmin = CurrentUser.Roles?.Contains(BuiltInRole.SuperAdmin.DeveloperName) ?? false;
        if (!isSuperAdmin)
        {
            SetErrorMessage("Only Super Admins can impersonate other users.");
            return RedirectToPage(RouteNames.Users.Edit, new { id });
        }

        var input = new BeginImpersonation.Command { TargetUserId = id };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            // Sign in as the impersonated user with impersonation claims
            await SignInWithImpersonation(response.Result);

            Logger.LogWarning(
                "SECURITY: Super Admin {OriginalEmail} (ID: {OriginalId}) started impersonating user {TargetEmail} (ID: {TargetId}) from IP: {IpAddress}",
                response.Result.OriginalUserEmail,
                response.Result.OriginalUserId,
                response.Result.ImpersonatedUser.EmailAddress,
                response.Result.ImpersonatedUser.Id,
                CurrentUser.RemoteIpAddress
            );

            SetSuccessMessage($"You are now impersonating {response.Result.ImpersonatedUser.FullName}. Click 'End Impersonation' in the header to return to your account.");

            // If impersonating a non-admin user, redirect to public area
            if (!response.Result.ImpersonatedUser.IsAdmin)
            {
                return Redirect("/");
            }

            return RedirectToPage(RouteNames.Dashboard.Index);
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
            return RedirectToPage(RouteNames.Users.Edit, new { id });
        }
    }

    private async Task SignInWithImpersonation(BeginImpersonation.ImpersonationResultDto result)
    {
        var impersonatedUser = result.ImpersonatedUser;
        
        var claims = new List<Claim>
        {
            // Standard identity claims
            new Claim(ClaimTypes.NameIdentifier, impersonatedUser.Id.ToString()),
            new Claim(ClaimTypes.GivenName, impersonatedUser.FirstName ?? string.Empty),
            new Claim(ClaimTypes.Surname, impersonatedUser.LastName ?? string.Empty),
            new Claim(ClaimTypes.Email, impersonatedUser.EmailAddress ?? string.Empty),
            new Claim(RaythaClaimTypes.IsAdmin, impersonatedUser.IsAdmin.ToString()),
            new Claim(RaythaClaimTypes.LastModificationTime, impersonatedUser.LastModificationTime?.ToString() ?? DateTime.UtcNow.ToString()),
            // Impersonation-specific claims
            new Claim(RaythaClaimTypes.IsImpersonating, "true"),
            new Claim(RaythaClaimTypes.OriginalUserId, result.OriginalUserId.ToString()),
            new Claim(RaythaClaimTypes.OriginalUserEmail, result.OriginalUserEmail),
            new Claim(RaythaClaimTypes.OriginalUserFullName, result.OriginalUserFullName),
            new Claim(RaythaClaimTypes.ImpersonationStartedAt, result.ImpersonationStartedAt.ToString("O")),
        };

        // Add role claims and collect system permissions
        if (impersonatedUser.Roles != null)
        {
            foreach (var role in impersonatedUser.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.DeveloperName));
                
                // Add system permissions from each role
                if (role.SystemPermissions != null)
                {
                    foreach (var permission in role.SystemPermissions)
                    {
                        claims.Add(new Claim(RaythaClaimTypes.SystemPermissions, permission));
                    }
                }
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false } // Don't persist impersonation sessions
        );
    }
}

