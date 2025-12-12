namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for ticket assignment email notifications.
/// </summary>
public record TicketAssigned_RenderModel
{
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? TeamName { get; init; }
    public string? SlaDueAt { get; init; }
    public string TicketUrl { get; init; } = string.Empty;
}

