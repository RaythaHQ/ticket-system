using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Common.Services;

/// <summary>
/// Implementation of ITicketPermissionService that checks ticketing permissions using claims.
/// </summary>
public class TicketPermissionService : ITicketPermissionService
{
    private readonly ICurrentUser _currentUser;
    private readonly IAppDbContext _db;

    public TicketPermissionService(ICurrentUser currentUser, IAppDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public bool CanManageTickets()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(
                BuiltInSystemPermission.MANAGE_TICKETS_PERMISSION
            ) ?? false;
    }

    public bool CanManageTeams()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(
                BuiltInSystemPermission.MANAGE_TEAMS_PERMISSION
            ) ?? false;
    }

    public bool CanAccessReports()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(
                BuiltInSystemPermission.ACCESS_REPORTS_PERMISSION
            ) ?? false;
    }

    public bool CanManageSystemViews()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(
                BuiltInSystemPermission.MANAGE_SYSTEM_VIEWS_PERMISSION
            ) ?? false;
    }

    public bool CanManageSystemSettings()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(
                BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION
            ) ?? false;
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
            throw new ForbiddenAccessException(
                "You do not have permission to manage system views."
            );
    }

    public void RequireCanManageSystemSettings()
    {
        if (!CanManageSystemSettings())
            throw new ForbiddenAccessException(
                "You do not have permission to manage system settings."
            );
    }

    public async Task<bool> CanEditTicketAsync(
        Guid? assigneeId,
        Guid? owningTeamId,
        CancellationToken cancellationToken = default
    )
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return false;

        // If user has CanManageTickets permission, they can edit any ticket
        if (CanManageTickets())
            return true;

        var userId = _currentUser.UserId.Value.Guid;

        // User can edit if they are assigned to the ticket
        if (assigneeId.HasValue && assigneeId.Value == userId)
            return true;

        // User can edit if they are a member of the ticket's team
        if (owningTeamId.HasValue)
        {
            var isTeamMember = await _db
                .TeamMemberships.AsNoTracking()
                .AnyAsync(
                    m => m.TeamId == owningTeamId.Value && m.StaffAdminId == userId,
                    cancellationToken
                );

            if (isTeamMember)
                return true;
        }

        return false;
    }

    public async Task RequireCanEditTicketAsync(
        Guid? assigneeId,
        Guid? owningTeamId,
        CancellationToken cancellationToken = default
    )
    {
        if (!await CanEditTicketAsync(assigneeId, owningTeamId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to edit this ticket.");
    }

    public async Task<HashSet<Guid>> GetUserTeamIdsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return new HashSet<Guid>();

        var userId = _currentUser.UserId.Value.Guid;
        var teamIds = await _db
            .TeamMemberships.AsNoTracking()
            .Where(m => m.StaffAdminId == userId)
            .Select(m => m.TeamId)
            .ToListAsync(cancellationToken);

        return teamIds.ToHashSet();
    }
}
