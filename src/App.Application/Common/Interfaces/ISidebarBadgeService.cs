namespace App.Application.Common.Interfaces;

/// <summary>
/// Provides badge counts for the staff sidebar navigation.
/// Returns the number of open tickets and tasks assigned to the current user.
/// </summary>
public interface ISidebarBadgeService
{
    /// <summary>
    /// Gets the count of open tickets assigned to the specified user.
    /// </summary>
    Task<int> GetMyTicketCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of open tasks assigned to the specified user.
    /// </summary>
    Task<int> GetMyTaskCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
