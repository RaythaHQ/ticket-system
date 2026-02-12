namespace App.Domain.Events;

public class TicketTaskAssignedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketTask Task { get; private set; }
    public Guid? PreviousAssigneeId { get; private set; }
    public Guid? PreviousTeamId { get; private set; }

    public TicketTaskAssignedEvent(
        TicketTask task,
        Guid? previousAssigneeId,
        Guid? previousTeamId)
    {
        Task = task;
        PreviousAssigneeId = previousAssigneeId;
        PreviousTeamId = previousTeamId;
    }
}
