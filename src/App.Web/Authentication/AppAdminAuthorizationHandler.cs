using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using App.Application.Common.Security;
using App.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace App.Web.Authentication;

public class IsAdminRequirement : IAuthorizationRequirement { }

public class ManageUsersRequirement : IAuthorizationRequirement { }

public class ManageSystemSettingsRequirement : IAuthorizationRequirement { }

public class ManageAdministratorsRequirement : IAuthorizationRequirement { }

public class ManageTemplatesRequirement : IAuthorizationRequirement { }

public class ManageAuditLogsRequirement : IAuthorizationRequirement { }

// Ticketing system requirements
public class ManageTeamsRequirement : IAuthorizationRequirement { }

public class ManageTicketsRequirement : IAuthorizationRequirement { }

public class AccessReportsRequirement : IAuthorizationRequirement { }

public class ManageSystemViewsRequirement : IAuthorizationRequirement { }

public class ImportExportTicketsRequirement : IAuthorizationRequirement { }

public class AppAdminAuthorizationHandler : IAuthorizationHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = null;

    public AppAdminAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (
            context.User == null
            || !context.User.Identity.IsAuthenticated
            || !IsAdmin(context.User)
        )
        {
            return Task.CompletedTask;
        }

        var systemPermissionsClaims = context
            .User.Claims.Where(p => p.Type == RaythaClaimTypes.SystemPermissions)
            .Select(p => p.Value)
            .ToArray();

        var pendingRequirements = context.PendingRequirements.ToList();

        foreach (var requirement in pendingRequirements)
        {
            if (requirement is IsAdminRequirement)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (requirement is ManageUsersRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.MANAGE_USERS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageSystemSettingsRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageTemplatesRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageAuditLogsRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageAdministratorsRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            // Ticketing system permissions
            else if (requirement is ManageTeamsRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.MANAGE_TEAMS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageTicketsRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.MANAGE_TICKETS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is AccessReportsRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.ACCESS_REPORTS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageSystemViewsRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.MANAGE_SYSTEM_VIEWS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ImportExportTicketsRequirement)
            {
                if (
                    systemPermissionsClaims.Contains(
                        BuiltInSystemPermission.IMPORT_EXPORT_TICKETS_PERMISSION
                    )
                )
                {
                    context.Succeed(requirement);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        var isAdminClaim = user.Claims.FirstOrDefault(c => c.Type == RaythaClaimTypes.IsAdmin);
        if (isAdminClaim == null)
        {
            return false;
        }
        return Convert.ToBoolean(isAdminClaim.Value);
    }
}
