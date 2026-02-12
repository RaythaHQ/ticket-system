namespace App.Domain.Events;

public class TicketTaskDependencyChangedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketTask Task { get; private set; }
    public Guid? PreviousDependsOnTaskId { get; private set; }

    public TicketTaskDependencyChangedEvent(TicketTask task, Guid? previousDependsOnTaskId)
    {
        Task = task;
        PreviousDependsOnTaskId = previousDependsOnTaskId;
    }
}
