namespace App.Domain.Events;

/// <summary>
/// Event raised when a ticket's SLA has been breached.
/// </summary>
public class SlaBreachedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Entities.Ticket Ticket { get; private set; }

    public SlaBreachedEvent(Entities.Ticket ticket)
    {
        Ticket = ticket;
    }
}

