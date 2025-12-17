namespace App.Domain.Events;

/// <summary>
/// Event raised when a ticket's title, description, or priority is changed.
/// This is separate from status/assignee changes which have their own events.
/// </summary>
public class TicketUpdatedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }

    /// <summary>
    /// Previous title before the update (null if unchanged).
    /// </summary>
    public string? OldTitle { get; private set; }

    /// <summary>
    /// New title after the update (null if unchanged).
    /// </summary>
    public string? NewTitle { get; private set; }

    /// <summary>
    /// Previous description before the update (null if unchanged).
    /// </summary>
    public string? OldDescription { get; private set; }

    /// <summary>
    /// New description after the update (null if unchanged).
    /// </summary>
    public string? NewDescription { get; private set; }

    /// <summary>
    /// Previous priority before the update (null if unchanged).
    /// </summary>
    public string? OldPriority { get; private set; }

    /// <summary>
    /// New priority after the update (null if unchanged).
    /// </summary>
    public string? NewPriority { get; private set; }

    /// <summary>
    /// User ID who made the change.
    /// </summary>
    public Guid? ChangedByUserId { get; private set; }

    public TicketUpdatedEvent(
        Ticket ticket,
        string? oldTitle = null,
        string? newTitle = null,
        string? oldDescription = null,
        string? newDescription = null,
        string? oldPriority = null,
        string? newPriority = null,
        Guid? changedByUserId = null
    )
    {
        Ticket = ticket;
        OldTitle = oldTitle;
        NewTitle = newTitle;
        OldDescription = oldDescription;
        NewDescription = newDescription;
        OldPriority = oldPriority;
        NewPriority = newPriority;
        ChangedByUserId = changedByUserId;
    }

    /// <summary>
    /// Returns true if any tracked field changed.
    /// </summary>
    public bool HasChanges =>
        (OldTitle != null || NewTitle != null)
        || (OldDescription != null || NewDescription != null)
        || (OldPriority != null || NewPriority != null);
}
