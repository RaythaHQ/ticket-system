using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetUserDashboardMetrics
{
    public record Query : IRequest<QueryResponseDto<UserDashboardMetricsDto>>
    {
        public ShortGuid UserId { get; init; }
    }

    public class Handler : IRequestHandler<Query, QueryResponseDto<UserDashboardMetricsDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<QueryResponseDto<UserDashboardMetricsDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);
            var thirtyDaysAgo = now.AddDays(-30);

            var userIdGuid = request.UserId.Guid;

            // NOTE: DbContext is not thread-safe, so queries must be sequential
            var openTicketsAssigned = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t =>
                        t.AssigneeId == userIdGuid
                        && t.Status != TicketStatus.CLOSED
                        && t.Status != TicketStatus.RESOLVED,
                    cancellationToken
                );

            var ticketsResolvedLast7Days = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t =>
                        t.AssigneeId == userIdGuid
                        && t.ResolvedAt.HasValue
                        && t.ResolvedAt.Value >= sevenDaysAgo,
                    cancellationToken
                );

            var ticketsResolvedLast30Days = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t =>
                        t.AssigneeId == userIdGuid
                        && t.ResolvedAt.HasValue
                        && t.ResolvedAt.Value >= thirtyDaysAgo,
                    cancellationToken
                );

            var closedTickets = await _db
                .Tickets.AsNoTracking()
                .Where(t => t.AssigneeId == userIdGuid && t.ClosedAt.HasValue)
                .Select(t => new { t.CreationTime, t.ClosedAt })
                .ToListAsync(cancellationToken);

            double? medianCloseTimeHours = null;
            if (closedTickets.Any())
            {
                var closeTimes = closedTickets
                    .Select(t => (t.ClosedAt!.Value - t.CreationTime).TotalHours)
                    .OrderBy(h => h)
                    .ToList();

                int mid = closeTimes.Count / 2;
                medianCloseTimeHours =
                    closeTimes.Count % 2 == 0
                        ? (closeTimes[mid - 1] + closeTimes[mid]) / 2
                        : closeTimes[mid];
            }

            var reopenCount = await _db
                .TicketChangeLogEntries.AsNoTracking()
                .Where(e => e.Ticket.AssigneeId == userIdGuid)
                .Where(e => e.Message != null && e.Message.Contains("Ticket reopened"))
                .CountAsync(cancellationToken);

            var totalResolved = closedTickets.Count;
            var reopenRate = totalResolved > 0 ? (double)reopenCount / totalResolved * 100 : 0;

            var slaBreachCount = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t => t.AssigneeId == userIdGuid && t.SlaStatus == SlaStatus.BREACHED,
                    cancellationToken
                );

            var slaApproachingCount = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t => t.AssigneeId == userIdGuid && t.SlaStatus == SlaStatus.APPROACHING_BREACH,
                    cancellationToken
                );

            var userTeamIds = await _db
                .TeamMemberships.AsNoTracking()
                .Where(m => m.StaffAdminId == userIdGuid)
                .Select(m => m.TeamId)
                .ToListAsync(cancellationToken);

            var teamSummaries = new List<TeamSummaryDto>();
            if (userTeamIds.Any())
            {
                var teams = await _db
                    .Teams.AsNoTracking()
                    .Where(t => userTeamIds.Contains(t.Id))
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        OpenTickets = t.Tickets.Count(x =>
                            x.Status != TicketStatus.CLOSED && x.Status != TicketStatus.RESOLVED
                        ),
                        UnassignedTickets = t.Tickets.Count(x =>
                            x.AssigneeId == null && x.Status != TicketStatus.CLOSED
                        ),
                    })
                    .ToListAsync(cancellationToken);

                teamSummaries = teams
                    .Select(t => new TeamSummaryDto
                    {
                        TeamId = t.Id,
                        TeamName = t.Name,
                        OpenTickets = t.OpenTickets,
                        UnassignedTickets = t.UnassignedTickets,
                    })
                    .ToList();
            }

            return new QueryResponseDto<UserDashboardMetricsDto>(
                new UserDashboardMetricsDto
                {
                    OpenTicketsAssigned = openTicketsAssigned,
                    TicketsResolvedLast7Days = ticketsResolvedLast7Days,
                    TicketsResolvedLast30Days = ticketsResolvedLast30Days,
                    MedianCloseTimeHours = medianCloseTimeHours,
                    ReopenCount = reopenCount,
                    ReopenRate = Math.Round(reopenRate, 2),
                    SlaBreachCount = slaBreachCount,
                    SlaApproachingCount = slaApproachingCount,
                    TeamSummaries = teamSummaries,
                }
            );
        }
    }
}
