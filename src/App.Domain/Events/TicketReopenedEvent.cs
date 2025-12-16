namespace App.Domain.Events;

public class TicketReopenedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }
    public Guid? ReopenedByUserId { get; private set; }

    public TicketReopenedEvent(Ticket ticket, Guid? reopenedByUserId = null)
    {
        Ticket = ticket;
        ReopenedByUserId = reopenedByUserId;
    }
}
