namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for SLA breached email notifications.
/// </summary>
public record SlaBreach_RenderModel
{
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AssigneeName { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string SlaDueAt { get; init; } = string.Empty;
    public string BreachedAt { get; init; } = string.Empty;
    public string SlaRuleName { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;
}

