namespace App.Domain.Events;

/// <summary>
/// Event raised when a ticket's SLA is approaching breach threshold.
/// </summary>
public class SlaApproachingBreachEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Entities.Ticket Ticket { get; private set; }

    public SlaApproachingBreachEvent(Entities.Ticket ticket)
    {
        Ticket = ticket;
    }
}

