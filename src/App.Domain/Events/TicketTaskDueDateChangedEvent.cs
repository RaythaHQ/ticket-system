namespace App.Domain.Events;

public class TicketTaskDueDateChangedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketTask Task { get; private set; }
    public DateTime? PreviousDueAt { get; private set; }

    public TicketTaskDueDateChangedEvent(TicketTask task, DateTime? previousDueAt)
    {
        Task = task;
        PreviousDueAt = previousDueAt;
    }
}
