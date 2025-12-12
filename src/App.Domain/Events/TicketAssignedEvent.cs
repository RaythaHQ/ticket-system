namespace App.Domain.Events;

public class TicketAssignedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Ticket Ticket { get; private set; }
    public Guid? OldAssigneeId { get; private set; }
    public Guid? NewAssigneeId { get; private set; }
    public Guid? OldTeamId { get; private set; }
    public Guid? NewTeamId { get; private set; }

    public TicketAssignedEvent(Ticket ticket, Guid? oldAssigneeId, Guid? newAssigneeId, Guid? oldTeamId = null, Guid? newTeamId = null)
    {
        Ticket = ticket;
        OldAssigneeId = oldAssigneeId;
        NewAssigneeId = newAssigneeId;
        OldTeamId = oldTeamId;
        NewTeamId = newTeamId;
    }
}

