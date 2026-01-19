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

            // Get open-type status developer names for filtering
            var openStatusNames = await _db
                .TicketStatusConfigs.AsNoTracking()
                .Where(s => s.StatusType == TicketStatusType.OPEN)
                .Select(s => s.DeveloperName)
                .ToListAsync(cancellationToken);

            // Open tickets assigned to the logged-in user
            var openTicketsAssigned = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t =>
                        t.AssigneeId == userIdGuid
                        && openStatusNames.Contains(t.Status),
                    cancellationToken
                );

            // Tickets closed BY the logged-in user in last 7 days
            var ticketsClosedLast7Days = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t =>
                        t.ClosedByStaffId == userIdGuid
                        && t.ClosedAt.HasValue
                        && t.ClosedAt.Value >= sevenDaysAgo,
                    cancellationToken
                );

            // Tickets closed BY the logged-in user in last 30 days
            var ticketsClosedLast30Days = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t =>
                        t.ClosedByStaffId == userIdGuid
                        && t.ClosedAt.HasValue
                        && t.ClosedAt.Value >= thirtyDaysAgo,
                    cancellationToken
                );

            // For median close time: only tickets where the user was both the assignee AND the closer
            // Time is calculated from when ticket was assigned to the user until they closed it
            var closedTicketsForMedian = await _db
                .Tickets.AsNoTracking()
                .Where(t =>
                    t.ClosedByStaffId == userIdGuid
                    && t.AssigneeId == userIdGuid
                    && t.ClosedAt.HasValue
                    && t.AssignedAt.HasValue
                    && t.ClosedAt.Value >= thirtyDaysAgo
                )
                .Select(t => new { t.AssignedAt, t.ClosedAt })
                .ToListAsync(cancellationToken);

            double? medianCloseTimeHours = null;
            if (closedTicketsForMedian.Any())
            {
                var closeTimes = closedTicketsForMedian
                    .Select(t => (t.ClosedAt!.Value - t.AssignedAt!.Value).TotalHours)
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
                .Where(e => e.Ticket.ClosedByStaffId == userIdGuid)
                .Where(e => e.Message != null && e.Message.Contains("Ticket reopened"))
                .CountAsync(cancellationToken);

            // Total tickets closed by the user (for reopen rate calculation)
            var totalClosedByUser = await _db
                .Tickets.AsNoTracking()
                .CountAsync(
                    t => t.ClosedByStaffId == userIdGuid && t.ClosedAt.HasValue,
                    cancellationToken
                );

            var reopenRate =
                totalClosedByUser > 0 ? (double)reopenCount / totalClosedByUser * 100 : 0;

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
                        OpenTickets = t.Tickets.Count(x => openStatusNames.Contains(x.Status)),
                        UnassignedTickets = t.Tickets.Count(x =>
                            x.AssigneeId == null && openStatusNames.Contains(x.Status)
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
                    TicketsClosedLast7Days = ticketsClosedLast7Days,
                    TicketsClosedLast30Days = ticketsClosedLast30Days,
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
