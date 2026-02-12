namespace App.Domain.Events;

public class TicketTaskCompletedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketTask Task { get; private set; }

    public TicketTaskCompletedEvent(TicketTask task)
    {
        Task = task;
    }
}
