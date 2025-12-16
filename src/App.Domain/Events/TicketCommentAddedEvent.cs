namespace App.Domain.Events;

public class TicketCommentAddedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public TicketComment Comment { get; private set; }
    public Ticket Ticket { get; private set; }

    /// <summary>
    /// User IDs explicitly mentioned in the comment via @ syntax.
    /// These users receive notifications regardless of their preferences.
    /// </summary>
    public List<Guid> MentionedUserIds { get; private set; }

    /// <summary>
    /// Team IDs mentioned in the comment via @ syntax.
    /// All members of these teams receive notifications regardless of their preferences.
    /// </summary>
    public List<Guid> MentionedTeamIds { get; private set; }

    public TicketCommentAddedEvent(
        TicketComment comment,
        Ticket ticket,
        List<Guid>? mentionedUserIds = null,
        List<Guid>? mentionedTeamIds = null
    )
    {
        Comment = comment;
        Ticket = ticket;
        MentionedUserIds = mentionedUserIds ?? new List<Guid>();
        MentionedTeamIds = mentionedTeamIds ?? new List<Guid>();
    }
}
