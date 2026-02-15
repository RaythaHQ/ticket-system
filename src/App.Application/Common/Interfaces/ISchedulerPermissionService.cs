namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for checking scheduler-related permissions.
/// </summary>
public interface ISchedulerPermissionService
{
    /// <summary>
    /// Checks if the current user has the "Manage Scheduler System" permission.
    /// </summary>
    bool CanManageSchedulerSystem();

    /// <summary>
    /// Checks if the current user is an active scheduler staff member.
    /// </summary>
    Task<bool> IsSchedulerStaffAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws ForbiddenAccessException if the current user does not have the "Manage Scheduler System" permission.
    /// </summary>
    void RequireCanManageSchedulerSystem();

    /// <summary>
    /// Throws ForbiddenAccessException if the current user is not an active scheduler staff member.
    /// </summary>
    Task RequireIsSchedulerStaffAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the SchedulerStaffMember ID for the current user, or null if not a scheduler staff member.
    /// </summary>
    Task<Guid?> GetCurrentStaffMemberIdAsync(CancellationToken cancellationToken = default);
}
