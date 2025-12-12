namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for ticket status changed email notifications.
/// </summary>
public record TicketStatusChanged_RenderModel
{
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public string ChangedBy { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;
}

