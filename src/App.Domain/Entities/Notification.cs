using App.Domain.Common;

namespace App.Domain.Entities;

/// <summary>
/// Represents a notification event recorded for a staff user.
/// Notifications are created when events occur, regardless of user delivery preferences.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// The staff user who receives this notification.
    /// </summary>
    public Guid RecipientUserId { get; set; }
    public virtual User RecipientUser { get; set; } = null!;

    /// <summary>
    /// The type of notification event (from NotificationEventType).
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Short title for display in notification list.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Full notification message with details.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Optional URL to navigate to when notification is clicked.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Optional reference to the related ticket.
    /// </summary>
    public long? TicketId { get; set; }
    public virtual Ticket? Ticket { get; set; }

    /// <summary>
    /// Whether the notification has been read by the user.
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the notification was marked as read.
    /// Null if unread.
    /// </summary>
    public DateTime? ReadAt { get; set; }
}

