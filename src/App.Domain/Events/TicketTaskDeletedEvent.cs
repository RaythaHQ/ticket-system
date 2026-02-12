namespace App.Domain.Events;

public class TicketTaskDeletedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public long TicketId { get; private set; }
    public string TaskTitle { get; private set; }
    public Guid? AssigneeId { get; private set; }

    public TicketTaskDeletedEvent(long ticketId, string taskTitle, Guid? assigneeId)
    {
        TicketId = ticketId;
        TaskTitle = taskTitle;
        AssigneeId = assigneeId;
    }
}
