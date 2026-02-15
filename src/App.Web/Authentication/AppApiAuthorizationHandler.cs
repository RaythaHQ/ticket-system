using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using App.Application.Common.Exceptions;
using App.Application.Common.Security;
using App.Application.Common.Utils;
using App.Application.Login.Commands;
using App.Domain.Entities;

namespace App.Web.Authentication;

public interface IHasApiKeyRequirement : IAuthorizationRequirement { }

public class ApiIsAdminRequirement : IHasApiKeyRequirement { }

public class ApiManageUsersRequirement : IHasApiKeyRequirement { }

public class ApiManageSystemSettingsRequirement : IHasApiKeyRequirement { }

public class ApiManageTemplatesRequirement : IHasApiKeyRequirement { }

public class AppApiAuthorizationHandler : IAuthorizationHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = null;
    private readonly IMediator _mediator;
    private const string X_API_KEY = "X-API-KEY";
    public const string POLICY_PREFIX = "API_";

    public AppApiAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IMediator mediator
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _mediator = mediator;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.ToList();
        if (!pendingRequirements.Any(p => p is IHasApiKeyRequirement))
        {
            return;
        }
        if (
            !_httpContextAccessor.HttpContext.Request.Headers.Any(p => p.Key.ToUpper() == X_API_KEY)
        )
        {
            throw new InvalidApiKeyException("Missing api key");
        }

        var apiKey = _httpContextAccessor
            .HttpContext.Request.Headers.FirstOrDefault(p => p.Key.ToUpper() == X_API_KEY)
            .Value.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidApiKeyException("Missing api key");
        }

        var user = await _mediator.Send(new LoginWithApiKey.Command { ApiKey = apiKey });

        if (!user.Success)
        {
            throw new InvalidApiKeyException(user.Error);
        }

        var systemPermissions = new List<string>();

        foreach (var role in user.Result.Roles)
        {
            systemPermissions.AddRange(role.SystemPermissions);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Result.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Result.EmailAddress),
            new Claim(ClaimTypes.GivenName, user.Result.FirstName),
            new Claim(ClaimTypes.Surname, user.Result.LastName),
            new Claim(
                AppClaimTypes.LastModificationTime,
                user.Result.LastModificationTime?.ToString() ?? string.Empty
            ),
            new Claim(AppClaimTypes.IsAdmin, user.Result.IsAdmin.ToString()),
            new Claim(AppClaimTypes.SsoId, user.Result.SsoId.IfNullOrEmpty(string.Empty)),
            new Claim(
                AppClaimTypes.AuthenticationScheme,
                user.Result.AuthenticationScheme.IfNullOrEmpty(string.Empty)
            ),
        };

        foreach (var role in user.Result.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.DeveloperName.ToString()));
        }

        foreach (var systemPermission in systemPermissions.Distinct())
        {
            claims.Add(new Claim(AppClaimTypes.SystemPermissions, systemPermission));
        }

        foreach (var userGroup in user.Result.UserGroups)
        {
            claims.Add(new Claim(AppClaimTypes.UserGroups, userGroup.DeveloperName));
        }

        var identity = new ClaimsIdentity(claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);
        _httpContextAccessor.HttpContext.User = principal;

        foreach (var requirement in pendingRequirements)
        {
            if (requirement is ApiIsAdminRequirement)
            {
                context.Succeed(requirement);
            }
            else if (requirement is ApiManageSystemSettingsRequirement)
            {
                if (
                    systemPermissions.Contains(
                        BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ApiManageUsersRequirement)
            {
                if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_USERS_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ApiManageTemplatesRequirement)
            {
                if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}

