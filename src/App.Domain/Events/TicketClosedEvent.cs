namespace App.Domain.Events;

public class TicketClosedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    public TicketClosedEvent(Ticket ticket, Guid? closedByUserId = null)
    {
        Ticket = ticket;
        ClosedByUserId = closedByUserId;
    }
}
