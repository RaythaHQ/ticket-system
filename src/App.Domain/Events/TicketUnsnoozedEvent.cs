namespace App.Domain.Events;

/// <summary>
/// Raised when a ticket is unsnoozed (either automatically or manually).
/// </summary>
public class TicketUnsnoozedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }

    /// <summary>
    /// The user who unsnoozed the ticket. Null if auto-unsnoozed by the system.
    /// </summary>
    public Guid? UnsnoozedById { get; private set; }

    /// <summary>
    /// True if the ticket was automatically unsnoozed by the background job.
    /// </summary>
    public bool WasAutoUnsnooze { get; private set; }

    /// <summary>
    /// How long the ticket was snoozed.
    /// </summary>
    public TimeSpan SnoozeDuration { get; private set; }

    public TicketUnsnoozedEvent(
        Ticket ticket,
        Guid? unsnoozedById,
        bool wasAutoUnsnooze,
        TimeSpan snoozeDuration
    )
    {
        Ticket = ticket;
        UnsnoozedById = unsnoozedById;
        WasAutoUnsnooze = wasAutoUnsnooze;
        SnoozeDuration = snoozeDuration;
    }
}
