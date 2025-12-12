namespace App.Domain.Events;

public class TicketCommentAddedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketComment Comment { get; private set; }
    public Ticket Ticket { get; private set; }

    public TicketCommentAddedEvent(TicketComment comment, Ticket ticket)
    {
        Comment = comment;
        Ticket = ticket;
    }
}

