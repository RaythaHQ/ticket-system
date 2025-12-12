using CSharpVitamins;

namespace App.Application.Tickets;

/// <summary>
/// DTO for organization-wide reporting metrics.
/// </summary>
public record OrganizationReportDto
{
    public DateTime ReportDate { get; init; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }

    // Volume metrics
    public int TotalTicketsCreated { get; init; }
    public int TotalTicketsResolved { get; init; }
    public int TotalTicketsClosed { get; init; }
    public int CurrentOpenTickets { get; init; }
    public int CurrentUnassignedTickets { get; init; }

    // SLA metrics
    public int TotalSlaBreaches { get; init; }
    public int TotalSlaMet { get; init; }
    public double OverallSlaComplianceRate { get; init; }

    // Time metrics
    public double? AverageResolutionTimeHours { get; init; }
    public double? MedianResolutionTimeHours { get; init; }
    public double? AverageFirstResponseTimeHours { get; init; }

    // Priority breakdown
    public Dictionary<string, int> TicketsByPriority { get; init; } = new();
    public Dictionary<string, int> TicketsByCategory { get; init; } = new();
    public Dictionary<string, int> TicketsByStatus { get; init; } = new();

    // Team breakdown
    public List<TeamSummaryReportDto> TeamBreakdown { get; init; } = new();
}

public record TeamSummaryReportDto
{
    public ShortGuid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public int TicketsCreated { get; init; }
    public int TicketsResolved { get; init; }
    public int OpenTickets { get; init; }
    public double SlaComplianceRate { get; init; }
}
