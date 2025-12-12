namespace App.Domain.Events;

public class TicketReopenedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }

    public TicketReopenedEvent(Ticket ticket)
    {
        Ticket = ticket;
    }
}

