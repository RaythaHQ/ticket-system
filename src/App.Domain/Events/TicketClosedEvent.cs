namespace App.Domain.Events;

public class TicketClosedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }

    public TicketClosedEvent(Ticket ticket)
    {
        Ticket = ticket;
    }
}

