namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for sending real-time in-app notifications via SignalR.
/// </summary>
public interface IInAppNotificationService
{
    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    Task SendToUserAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? url = null,
        long? ticketId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends a notification to multiple users.
    /// </summary>
    Task SendToUsersAsync(
        IEnumerable<Guid> userIds,
        string type,
        string title,
        string message,
        string? url = null,
        long? ticketId = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Notification types for styling and categorization.
/// </summary>
public static class NotificationType
{
    public const string TicketAssigned = "ticket_assigned";
    public const string CommentAdded = "comment_added";
    public const string StatusChanged = "status_changed";
    public const string SlaApproaching = "sla_approaching";
    public const string SlaBreach = "sla_breach";
    public const string TicketReopened = "ticket_reopened";
    public const string Info = "info";
    public const string Success = "success";
    public const string Warning = "warning";
    public const string Error = "error";
}
