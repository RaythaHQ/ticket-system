namespace App.Domain.Events;

public class TicketTaskUnblockedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketTask Task { get; private set; }

    public TicketTaskUnblockedEvent(TicketTask task)
    {
        Task = task;
    }
}
