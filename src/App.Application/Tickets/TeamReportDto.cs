using CSharpVitamins;

namespace App.Application.Tickets;

/// <summary>
/// DTO for team-level reporting metrics.
/// </summary>
public record TeamReportDto
{
    public ShortGuid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;

    // Volume metrics
    public int TotalTickets { get; init; }
    public int OpenTickets { get; init; }
    public int ResolvedTickets { get; init; }
    public int ClosedTickets { get; init; }
    public int UnassignedTickets { get; init; }

    // SLA metrics
    public int SlaBreachedCount { get; init; }
    public int SlaMetCount { get; init; }
    public double SlaComplianceRate { get; init; }

    // Time metrics
    public double? AverageResolutionTimeHours { get; init; }
    public double? MedianResolutionTimeHours { get; init; }

    // Member breakdown
    public List<MemberMetricsDto> MemberMetrics { get; init; } = new();
}

public record MemberMetricsDto
{
    public ShortGuid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int AssignedTickets { get; init; }
    public int ResolvedTickets { get; init; }
    public double? MedianResolutionTimeHours { get; init; }
}

