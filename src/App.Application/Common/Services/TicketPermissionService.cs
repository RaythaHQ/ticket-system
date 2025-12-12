using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Common.Services;

/// <summary>
/// Implementation of ITicketPermissionService that checks ticketing permissions for the current user.
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
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return false;

        var user = _db.Users.AsNoTracking()
            .FirstOrDefault(u => u.Id == _currentUser.UserId.Value.Guid);
        return user?.CanManageTickets ?? false;
    }

    public bool CanManageTeams()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return false;

        var user = _db.Users.AsNoTracking()
            .FirstOrDefault(u => u.Id == _currentUser.UserId.Value.Guid);
        return user?.ManageTeams ?? false;
    }

    public bool CanAccessReports()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return false;

        var user = _db.Users.AsNoTracking()
            .FirstOrDefault(u => u.Id == _currentUser.UserId.Value.Guid);
        return user?.AccessReports ?? false;
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
}

