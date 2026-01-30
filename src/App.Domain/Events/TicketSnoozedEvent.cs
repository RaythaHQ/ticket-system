namespace App.Domain.Events;

/// <summary>
/// Raised when a ticket is snoozed.
/// </summary>
public class TicketSnoozedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }
    public DateTime SnoozedUntil { get; private set; }
    public Guid SnoozedById { get; private set; }
    public string? Reason { get; private set; }

    public TicketSnoozedEvent(
        Ticket ticket,
        DateTime snoozedUntil,
        Guid snoozedById,
        string? reason = null
    )
    {
        Ticket = ticket;
        SnoozedUntil = snoozedUntil;
        SnoozedById = snoozedById;
        Reason = reason;
    }
}
