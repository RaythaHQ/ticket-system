namespace App.Application.Common.Interfaces;

/// <summary>
/// Service to check if notifications should be suppressed for the current request.
/// This allows API automation to perform operations without triggering notifications.
/// </summary>
public interface INotificationSuppressionService
{
    /// <summary>
    /// Returns true if notifications should be suppressed for the current request.
    /// Checks for X-API-SuppressNotifications header or X-SuppressNotifications header.
    /// </summary>
    bool ShouldSuppressNotifications();
}

