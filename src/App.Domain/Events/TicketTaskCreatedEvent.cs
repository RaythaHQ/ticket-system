namespace App.Domain.Events;

public class TicketTaskCreatedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketTask Task { get; private set; }

    public TicketTaskCreatedEvent(TicketTask task)
    {
        Task = task;
    }
}
