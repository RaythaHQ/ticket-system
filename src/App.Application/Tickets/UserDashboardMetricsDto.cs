namespace App.Application.Tickets;

/// <summary>
/// DTO for user dashboard metrics.
/// </summary>
public record UserDashboardMetricsDto
{
    public int OpenTicketsAssigned { get; init; }
    public int TicketsResolvedLast7Days { get; init; }
    public int TicketsResolvedLast30Days { get; init; }
    public double? MedianCloseTimeHours { get; init; }
    public int ReopenCount { get; init; }
    public double ReopenRate { get; init; }
    public int SlaBreachCount { get; init; }
    public int SlaApproachingCount { get; init; }

    // Team summaries
    public List<TeamSummaryDto> TeamSummaries { get; init; } = new();
}

public record TeamSummaryDto
{
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public int OpenTickets { get; init; }
    public int UnassignedTickets { get; init; }
}

