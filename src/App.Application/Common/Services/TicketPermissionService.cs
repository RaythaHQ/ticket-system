using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Domain.Entities;

namespace App.Application.Common.Services;

/// <summary>
/// Implementation of ITicketPermissionService that checks ticketing permissions using claims.
/// </summary>
public class TicketPermissionService : ITicketPermissionService
{
    private readonly ICurrentUser _currentUser;

    public TicketPermissionService(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public bool CanManageTickets()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(BuiltInSystemPermission.MANAGE_TICKETS_PERMISSION) ?? false;
    }

    public bool CanManageTeams()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(BuiltInSystemPermission.MANAGE_TEAMS_PERMISSION) ?? false;
    }

    public bool CanAccessReports()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(BuiltInSystemPermission.ACCESS_REPORTS_PERMISSION) ?? false;
    }

    public bool CanManageSystemViews()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(BuiltInSystemPermission.MANAGE_SYSTEM_VIEWS_PERMISSION) ?? false;
    }

    public void RequireCanManageTickets()
    {
        if (!CanManageTickets())
            throw new ForbiddenAccessException("You do not have permission to manage tickets.");
    }

    public void RequireCanManageTeams()
    {
        if (!CanManageTeams())
            throw new ForbiddenAccessException("You do not have permission to manage teams.");
    }

    public void RequireCanAccessReports()
    {
        if (!CanAccessReports())
            throw new ForbiddenAccessException("You do not have permission to access reports.");
    }

    public void RequireCanManageSystemViews()
    {
        if (!CanManageSystemViews())
            throw new ForbiddenAccessException("You do not have permission to manage system views.");
    }
}
