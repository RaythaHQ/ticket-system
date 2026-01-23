using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using App.Application.Common.Security;
using App.Application.Common.Utils;
using App.Application.Login.Queries;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace App.Web.Authentication;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly IMediator _mediator;

    public CustomCookieAuthenticationEvents(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userPrincipal = context.Principal;

        // Preserve impersonation claims (otherwise they get wiped out when we rehydrate claims below)
        var isImpersonating =
            userPrincipal?.Claims.FirstOrDefault(p => p.Type == RaythaClaimTypes.IsImpersonating)?.Value
            is "true";

        var originalUserId = userPrincipal
            ?.Claims.FirstOrDefault(p => p.Type == RaythaClaimTypes.OriginalUserId)
            ?.Value;
        var originalUserEmail = userPrincipal
            ?.Claims.FirstOrDefault(p => p.Type == RaythaClaimTypes.OriginalUserEmail)
            ?.Value;
        var originalUserFullName = userPrincipal
            ?.Claims.FirstOrDefault(p => p.Type == RaythaClaimTypes.OriginalUserFullName)
            ?.Value;
        var impersonationStartedAt = userPrincipal
            ?.Claims.FirstOrDefault(p => p.Type == RaythaClaimTypes.ImpersonationStartedAt)
            ?.Value;

        // Look for the LastChanged claim.
        var lastModifiedAsString = userPrincipal
            .Claims.FirstOrDefault(p => p.Type == "LastModificationTime")
            ?.Value;
        var userIdAsString = userPrincipal
            .Claims.FirstOrDefault(p => p.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        if (lastModifiedAsString == null || userIdAsString == null)
            return;

        var user = await _mediator.Send(
            new GetUserForAuthenticationById.Query { Id = userIdAsString }
        );

        if (user == null || !user.Success || !user.Result.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            return;
        }

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Result.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Result.EmailAddress),
            new Claim(ClaimTypes.GivenName, user.Result.FirstName),
            new Claim(ClaimTypes.Surname, user.Result.LastName),
            new Claim(
                RaythaClaimTypes.LastModificationTime,
                user.Result.LastModificationTime.ToString()
            ),
            new Claim(RaythaClaimTypes.IsAdmin, user.Result.IsAdmin.ToString()),
            new Claim(RaythaClaimTypes.SsoId, user.Result.SsoId.IfNullOrEmpty(string.Empty)),
            new Claim(RaythaClaimTypes.AuthenticationScheme, user.Result.AuthenticationScheme),
        };

        // Re-add impersonation claims if this session is impersonating
        if (isImpersonating)
        {
            claims.Add(new Claim(RaythaClaimTypes.IsImpersonating, "true"));
            if (!string.IsNullOrWhiteSpace(originalUserId))
                claims.Add(new Claim(RaythaClaimTypes.OriginalUserId, originalUserId));
            if (!string.IsNullOrWhiteSpace(originalUserEmail))
                claims.Add(new Claim(RaythaClaimTypes.OriginalUserEmail, originalUserEmail));
            if (!string.IsNullOrWhiteSpace(originalUserFullName))
                claims.Add(new Claim(RaythaClaimTypes.OriginalUserFullName, originalUserFullName));
            if (!string.IsNullOrWhiteSpace(impersonationStartedAt))
                claims.Add(new Claim(RaythaClaimTypes.ImpersonationStartedAt, impersonationStartedAt));
        }

        var systemPermissions = new List<string>();

        foreach (var role in user.Result.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.DeveloperName.ToString()));
            systemPermissions.AddRange(role.SystemPermissions);
        }

        foreach (var systemPermission in systemPermissions.Distinct())
        {
            claims.Add(new Claim(RaythaClaimTypes.SystemPermissions, systemPermission));
        }

        foreach (var userGroup in user.Result.UserGroups)
        {
            claims.Add(new Claim(RaythaClaimTypes.UserGroups, userGroup.DeveloperName));
        }

        ClaimsIdentity identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        context.ReplacePrincipal(principal);
        context.ShouldRenew = true;
    }
}
