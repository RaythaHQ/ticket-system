namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for checking user notification preferences.
/// </summary>
public interface INotificationPreferenceService
{
    /// <summary>
    /// Checks if email notifications are enabled for a user and event type.
    /// Returns true if no preference exists (defaults to enabled).
    /// </summary>
    Task<bool> IsEmailEnabledAsync(
        Guid userId,
        string eventType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Filters a list of user IDs to only those who have email enabled for the event type.
    /// </summary>
    Task<List<Guid>> FilterUsersWithEmailEnabledAsync(
        IEnumerable<Guid> userIds,
        string eventType,
        CancellationToken cancellationToken = default
    );
}
