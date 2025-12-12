namespace App.Domain.Events;

public class TicketStatusChangedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }
    public string OldStatus { get; private set; }
    public string NewStatus { get; private set; }

    public TicketStatusChangedEvent(Ticket ticket, string oldStatus, string newStatus)
    {
        Ticket = ticket;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}

