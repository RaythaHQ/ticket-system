using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

/// <summary>
/// Implementation of ISchedulerPermissionService that checks scheduler permissions
/// using claims for admin permission and DB for scheduler staff membership.
/// </summary>
public class SchedulerPermissionService : ISchedulerPermissionService
{
    private readonly ICurrentUser _currentUser;
    private readonly IAppDbContext _db;

    public SchedulerPermissionService(ICurrentUser currentUser, IAppDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public bool CanManageSchedulerSystem()
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        return _currentUser.SystemPermissions?.Contains(
                BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION
            ) ?? false;
    }

    public async Task<bool> IsSchedulerStaffAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return false;

        var userId = _currentUser.UserId.Value.Guid;
        return await _db
            .SchedulerStaffMembers.AsNoTracking()
            .AnyAsync(s => s.UserId == userId && s.IsActive, cancellationToken);
    }

    public void RequireCanManageSchedulerSystem()
    {
        if (!CanManageSchedulerSystem())
            throw new ForbiddenAccessException(
                "You do not have permission to manage the scheduler system."
            );
    }

    public async Task RequireIsSchedulerStaffAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsSchedulerStaffAsync(cancellationToken))
            throw new ForbiddenAccessException(
                "You must be an active scheduler staff member to access this area."
            );
    }

    public async Task<Guid?> GetCurrentStaffMemberIdAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return null;

        var userId = _currentUser.UserId.Value.Guid;
        var staffMember = await _db
            .SchedulerStaffMembers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive, cancellationToken);

        return staffMember?.Id;
    }
}
