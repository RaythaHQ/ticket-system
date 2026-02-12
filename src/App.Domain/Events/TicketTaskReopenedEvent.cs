namespace App.Domain.Events;

public class TicketTaskReopenedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketTask Task { get; private set; }

    public TicketTaskReopenedEvent(TicketTask task)
    {
        Task = task;
    }
}
