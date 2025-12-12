namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for SLA approaching breach email notifications.
/// </summary>
public record SlaApproaching_RenderModel
{
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AssigneeName { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string SlaDueAt { get; init; } = string.Empty;
    public string TimeRemaining { get; init; } = string.Empty;
    public string SlaRuleName { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;
}

