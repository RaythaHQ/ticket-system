namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for ticket comment added email notifications.
/// </summary>
public record TicketCommentAdded_RenderModel
{
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string CommentAuthor { get; init; } = string.Empty;
    public string CommentBody { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;
}

