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

            // Get closed-type status developer names for filtering
            // Use negative matching (NOT closed) to be consistent with custom view filters
            // This ensures tickets with orphaned/missing status configs are counted as open
            var closedStatusNames = await _db
                .TicketStatusConfigs.AsNoTracking()
                .Where(s => s.StatusType == TicketStatusType.CLOSED)
                .Select(s => s.DeveloperName)
                .ToListAsync(cancellationToken);

            // Tickets CREATED in the period (for "tickets created" count)
            var ticketsCreatedInPeriod = await _db
                .Tickets.AsNoTracking()
                .Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate)
                .ToListAsync(cancellationToken);

            // Tickets CLOSED in the period (regardless of when created)
            // Match staff leaderboard: only count tickets still in closed status
            var ticketsClosedInPeriod = await _db
                .Tickets.AsNoTracking()
                .Where(t =>
                    t.ClosedAt.HasValue
                    && t.ClosedAt.Value >= startDate
                    && t.ClosedAt.Value <= endDate
                    && closedStatusNames.Contains(t.Status)
                )
                .ToListAsync(cancellationToken);

            // Tickets REOPENED in the period (count via change log entries)
            var totalTicketsReopened = await _db
                .TicketChangeLogEntries.AsNoTracking()
                .Where(e =>
                    e.CreationTime >= startDate
                    && e.CreationTime <= endDate
                    && e.Message != null
                    && e.Message.Contains("Ticket reopened")
                )
                .CountAsync(cancellationToken);

            var allCurrentTickets = await _db.Tickets.AsNoTracking().ToListAsync(cancellationToken);
            var nowUtc = DateTime.UtcNow;

            var totalTicketsCreated = ticketsCreatedInPeriod.Count;
            var totalTicketsClosed = ticketsClosedInPeriod.Count;
            var currentOpenTickets = allCurrentTickets.Count(t =>
                !closedStatusNames.Contains(t.Status)
            );
            var currentSnoozedTickets = allCurrentTickets.Count(t =>
                t.SnoozedUntil.HasValue && t.SnoozedUntil.Value > nowUtc
            );
            var currentUnassignedTickets = allCurrentTickets.Count(t =>
                t.AssigneeId == null && t.OwningTeamId == null && !closedStatusNames.Contains(t.Status)
            );

            // SLA metrics: count breaches that occurred in the period
            var totalSlaBreaches = ticketsCreatedInPeriod.Count(t =>
                t.SlaBreachedAt.HasValue && t.SlaBreachedAt.Value >= startDate
            );
            var totalSlaMet = ticketsCreatedInPeriod.Count(t => t.SlaStatus == SlaStatus.COMPLETED);
            var slaWithResult = totalSlaBreaches + totalSlaMet;
            var overallSlaComplianceRate =
                slaWithResult > 0 ? (double)totalSlaMet / slaWithResult * 100 : 100;

            // Resolution times: use tickets closed in the period
            var resolvedWithTimes = ticketsClosedInPeriod
                .Where(t => t.ClosedAt.HasValue)
                .Select(t => (t.ClosedAt!.Value - t.CreationTime).TotalHours)
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

            // Distribution stats: based on tickets created in the period
            var ticketsByPriority = ticketsCreatedInPeriod
                .GroupBy(t => t.Priority ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var ticketsByCategory = ticketsCreatedInPeriod
                .GroupBy(t => t.Category ?? "Uncategorized")
                .ToDictionary(g => g.Key, g => g.Count());

            var ticketsByStatus = ticketsCreatedInPeriod
                .GroupBy(t => t.Status ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            // Team breakdown
            var teams = await _db.Teams.AsNoTracking().ToListAsync(cancellationToken);
            var teamBreakdown = teams
                .Select(team =>
                {
                    var teamTicketsCreated = ticketsCreatedInPeriod
                        .Where(t => t.OwningTeamId == team.Id)
                        .ToList();
                    var teamTicketsClosed = ticketsClosedInPeriod
                        .Where(t => t.OwningTeamId == team.Id)
                        .ToList();
                    var teamBreaches = teamTicketsCreated.Count(t => t.SlaStatus == SlaStatus.BREACHED);
                    var teamMet = teamTicketsCreated.Count(t => t.SlaStatus == SlaStatus.COMPLETED);
                    var teamSlaTotal = teamBreaches + teamMet;

                    return new TeamSummaryReportDto
                    {
                        TeamId = new ShortGuid(team.Id),
                        TeamName = team.Name,
                        TicketsCreated = teamTicketsCreated.Count,
                        TicketsClosed = teamTicketsClosed.Count,
                        OpenTickets = teamTicketsCreated.Count(t => !closedStatusNames.Contains(t.Status)),
                        SlaComplianceRate =
                            teamSlaTotal > 0
                                ? Math.Round((double)teamMet / teamSlaTotal * 100, 2)
                                : 100,
                    };
                })
                .ToList();

            // Daily breakdown for chart - group by date
            var dailyBreakdown = new List<DailyTicketStatsDto>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayStart = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1);

                var createdCount = ticketsCreatedInPeriod.Count(t =>
                    t.CreationTime >= dayStart && t.CreationTime < dayEnd
                );
                var closedCount = ticketsClosedInPeriod.Count(t =>
                    t.ClosedAt.HasValue && t.ClosedAt.Value >= dayStart && t.ClosedAt.Value < dayEnd
                );

                dailyBreakdown.Add(
                    new DailyTicketStatsDto
                    {
                        Date = dayStart,
                        TicketsCreated = createdCount,
                        TicketsClosed = closedCount,
                    }
                );
            }

            return new QueryResponseDto<OrganizationReportDto>(
                new OrganizationReportDto
                {
                    ReportDate = DateTime.UtcNow,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    TotalTicketsCreated = totalTicketsCreated,
                    TotalTicketsClosed = totalTicketsClosed,
                    TotalTicketsReopened = totalTicketsReopened,
                    CurrentOpenTickets = currentOpenTickets,
                    CurrentSnoozedTickets = currentSnoozedTickets,
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
                    DailyBreakdown = dailyBreakdown,
                }
            );
        }
    }
}
