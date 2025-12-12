namespace App.Domain.Events;

public class TicketCreatedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }

    public TicketCreatedEvent(Ticket ticket)
    {
        Ticket = ticket;
    }
}

