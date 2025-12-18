namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for broadcasting real-time activity events to the activity stream via SignalR.
/// </summary>
public interface IActivityStreamService
{
    /// <summary>
    /// Broadcasts an activity event to all users watching the activity stream.
    /// </summary>
    Task BroadcastActivityAsync(
        ActivityEvent activityEvent,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Represents an activity event for the live activity stream.
/// </summary>
public class ActivityEvent
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The type of activity (e.g., ticket_created, comment_added).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Display message describing what happened.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the activity.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// The user who performed the action.
    /// </summary>
    public Guid? ActorId { get; set; }

    /// <summary>
    /// Name of the user who performed the action.
    /// </summary>
    public string? ActorName { get; set; }

    /// <summary>
    /// Ticket ID if the activity relates to a ticket.
    /// </summary>
    public long? TicketId { get; set; }

    /// <summary>
    /// Contact ID if the activity relates to a contact.
    /// </summary>
    public long? ContactId { get; set; }

    /// <summary>
    /// URL to navigate to for this activity.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// When the activity occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Activity event types for the activity stream.
/// </summary>
public static class ActivityEventType
{
    public const string TicketCreated = "ticket_created";
    public const string TicketUpdated = "ticket_updated";
    public const string TicketAssigned = "ticket_assigned";
    public const string TicketStatusChanged = "ticket_status_changed";
    public const string TicketClosed = "ticket_closed";
    public const string TicketReopened = "ticket_reopened";
    public const string CommentAdded = "comment_added";
    public const string ContactCreated = "contact_created";
    public const string ContactUpdated = "contact_updated";
}
