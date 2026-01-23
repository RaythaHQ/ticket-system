using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetOrganizationReport
{
    public record Query : IRequest<QueryResponseDto<OrganizationReportDto>>
    {
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
    }

    public class Handler : IRequestHandler<Query, QueryResponseDto<OrganizationReportDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<QueryResponseDto<OrganizationReportDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanAccessReports();

            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            // Ensure dates are UTC for PostgreSQL
            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            // Get open-type status developer names for filtering
            var openStatusNames = await _db
                .TicketStatusConfigs.AsNoTracking()
                .Where(s => s.StatusType == TicketStatusType.OPEN)
                .Select(s => s.DeveloperName)
                .ToListAsync(cancellationToken);

            var ticketsInPeriod = await _db
                .Tickets.AsNoTracking()
                .Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate)
                .ToListAsync(cancellationToken);

            var allCurrentTickets = await _db.Tickets.AsNoTracking().ToListAsync(cancellationToken);

            var totalTicketsCreated = ticketsInPeriod.Count;
            var totalTicketsResolved = ticketsInPeriod.Count(t =>
                t.ResolvedAt.HasValue && t.ResolvedAt.Value >= startDate
            );
            var totalTicketsClosed = ticketsInPeriod.Count(t =>
                t.ClosedAt.HasValue && t.ClosedAt.Value >= startDate
            );
            var currentOpenTickets = allCurrentTickets.Count(t =>
                openStatusNames.Contains(t.Status)
            );
            var currentUnassignedTickets = allCurrentTickets.Count(t =>
                t.AssigneeId == null && t.OwningTeamId == null && openStatusNames.Contains(t.Status)
            );

            var totalSlaBreaches = ticketsInPeriod.Count(t =>
                t.SlaBreachedAt.HasValue && t.SlaBreachedAt.Value >= startDate
            );
            var totalSlaMet = ticketsInPeriod.Count(t => t.SlaStatus == SlaStatus.COMPLETED);
            var slaWithResult = totalSlaBreaches + totalSlaMet;
            var overallSlaComplianceRate =
                slaWithResult > 0 ? (double)totalSlaMet / slaWithResult * 100 : 100;

            var resolvedWithTimes = ticketsInPeriod
                .Where(t => t.ResolvedAt.HasValue)
                .Select(t => (t.ResolvedAt!.Value - t.CreationTime).TotalHours)
                .ToList();

            double? avgResolutionTime = resolvedWithTimes.Any()
                ? resolvedWithTimes.Average()
                : null;
            double? medianResolutionTime = null;
            if (resolvedWithTimes.Any())
            {
                var sorted = resolvedWithTimes.OrderBy(h => h).ToList();
                int mid = sorted.Count / 2;
                medianResolutionTime =
                    sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
            }

            var ticketsByPriority = ticketsInPeriod
                .GroupBy(t => t.Priority ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var ticketsByCategory = ticketsInPeriod
                .GroupBy(t => t.Category ?? "Uncategorized")
                .ToDictionary(g => g.Key, g => g.Count());

            var ticketsByStatus = ticketsInPeriod
                .GroupBy(t => t.Status ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            // Team breakdown
            var teams = await _db.Teams.AsNoTracking().ToListAsync(cancellationToken);
            var teamBreakdown = teams
                .Select(team =>
                {
                    var teamTickets = ticketsInPeriod
                        .Where(t => t.OwningTeamId == team.Id)
                        .ToList();
                    var teamBreaches = teamTickets.Count(t => t.SlaStatus == SlaStatus.BREACHED);
                    var teamMet = teamTickets.Count(t => t.SlaStatus == SlaStatus.COMPLETED);
                    var teamSlaTotal = teamBreaches + teamMet;

                    return new TeamSummaryReportDto
                    {
                        TeamId = new ShortGuid(team.Id),
                        TeamName = team.Name,
                        TicketsCreated = teamTickets.Count,
                        TicketsResolved = teamTickets.Count(t => t.ResolvedAt.HasValue),
                        OpenTickets = teamTickets.Count(t => openStatusNames.Contains(t.Status)),
                        SlaComplianceRate =
                            teamSlaTotal > 0
                                ? Math.Round((double)teamMet / teamSlaTotal * 100, 2)
                                : 100,
                    };
                })
                .ToList();

            return new QueryResponseDto<OrganizationReportDto>(
                new OrganizationReportDto
                {
                    ReportDate = DateTime.UtcNow,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    TotalTicketsCreated = totalTicketsCreated,
                    TotalTicketsResolved = totalTicketsResolved,
                    TotalTicketsClosed = totalTicketsClosed,
                    CurrentOpenTickets = currentOpenTickets,
                    CurrentUnassignedTickets = currentUnassignedTickets,
                    TotalSlaBreaches = totalSlaBreaches,
                    TotalSlaMet = totalSlaMet,
                    OverallSlaComplianceRate = Math.Round(overallSlaComplianceRate, 2),
                    AverageResolutionTimeHours = avgResolutionTime.HasValue
                        ? Math.Round(avgResolutionTime.Value, 2)
                        : null,
                    MedianResolutionTimeHours = medianResolutionTime.HasValue
                        ? Math.Round(medianResolutionTime.Value, 2)
                        : null,
                    TicketsByPriority = ticketsByPriority,
                    TicketsByCategory = ticketsByCategory,
                    TicketsByStatus = ticketsByStatus,
                    TeamBreakdown = teamBreakdown,
                }
            );
        }
    }
}
