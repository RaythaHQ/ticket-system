using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetLeaderboard
{
    public record Query : IRequest<QueryResponseDto<LeaderboardDto>> { }

    public class Handler : IRequestHandler<Query, QueryResponseDto<LeaderboardDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentOrganization _currentOrganization;

        public Handler(IAppDbContext db, ICurrentOrganization currentOrganization)
        {
            _db = db;
            _currentOrganization = currentOrganization;
        }

        public async ValueTask<QueryResponseDto<LeaderboardDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Get time boundaries in organization's timezone, then convert to UTC
            var timeZone = _currentOrganization.TimeZone;
            var nowUtc = DateTime.UtcNow;
            var nowLocal = nowUtc.UtcToTimeZone(timeZone);

            // Start of today (midnight in org timezone)
            var startOfTodayLocal = nowLocal.Date;
            var startOfTodayUtc = startOfTodayLocal.TimeZoneToUtc(timeZone);

            // Start of this week (Monday in org timezone)
            var daysFromMonday = ((int)nowLocal.DayOfWeek - 1 + 7) % 7;
            var startOfWeekLocal = nowLocal.Date.AddDays(-daysFromMonday);
            var startOfWeekUtc = startOfWeekLocal.TimeZoneToUtc(timeZone);

            // Start of this month (1st of month in org timezone)
            var startOfMonthLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1);
            var startOfMonthUtc = startOfMonthLocal.TimeZoneToUtc(timeZone);

            // Get closed status developer names from configuration
            // Only count tickets that are still in a closed-type status (not reopened)
            var closedStatusNames = await _db
                .TicketStatusConfigs.AsNoTracking()
                .Where(s => s.StatusType == TicketStatusType.CLOSED)
                .Select(s => s.DeveloperName)
                .ToListAsync(cancellationToken);

            // Get all closed tickets with their close times for the month
            // (we'll filter for day/week in memory since we need all three counts)
            var closedTickets = await _db
                .Tickets.AsNoTracking()
                .Where(t =>
                    t.ClosedAt.HasValue
                    && t.ClosedAt.Value >= startOfMonthUtc
                    && closedStatusNames.Contains(t.Status)
                )
                .Select(t => new
                {
                    t.ClosedByStaffId,
                    t.OwningTeamId,
                    t.ClosedAt,
                })
                .ToListAsync(cancellationToken);

            // Get staff info for leaderboard (based on who closed the ticket)
            var staffIds = closedTickets
                .Where(t => t.ClosedByStaffId.HasValue)
                .Select(t => t.ClosedByStaffId!.Value)
                .Distinct()
                .ToList();

            var staffNames = await _db
                .Users.AsNoTracking()
                .Where(u => staffIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                })
                .ToDictionaryAsync(
                    u => u.Id,
                    u => $"{u.FirstName} {u.LastName}",
                    cancellationToken
                );

            // Get team info for leaderboard
            var teamIds = closedTickets
                .Where(t => t.OwningTeamId.HasValue)
                .Select(t => t.OwningTeamId!.Value)
                .Distinct()
                .ToList();

            var teamNames = await _db
                .Teams.AsNoTracking()
                .Where(t => teamIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Name })
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

            // Build staff leaderboard (by who closed the ticket)
            var staffStats = closedTickets
                .Where(t => t.ClosedByStaffId.HasValue)
                .GroupBy(t => t.ClosedByStaffId!.Value)
                .Select(g => new StaffLeaderboardEntry
                {
                    StaffId = g.Key,
                    StaffName = staffNames.GetValueOrDefault(g.Key, "Unknown"),
                    ClosedToday = g.Count(t => t.ClosedAt >= startOfTodayUtc),
                    ClosedThisWeek = g.Count(t => t.ClosedAt >= startOfWeekUtc),
                    ClosedThisMonth = g.Count(),
                })
                .OrderByDescending(s => s.ClosedThisMonth)
                .ThenByDescending(s => s.ClosedThisWeek)
                .ThenByDescending(s => s.ClosedToday)
                .Take(10)
                .ToList();

            // Build team leaderboard
            var teamStats = closedTickets
                .Where(t => t.OwningTeamId.HasValue)
                .GroupBy(t => t.OwningTeamId!.Value)
                .Select(g => new TeamLeaderboardEntry
                {
                    TeamId = g.Key,
                    TeamName = teamNames.GetValueOrDefault(g.Key, "Unknown"),
                    ClosedToday = g.Count(t => t.ClosedAt >= startOfTodayUtc),
                    ClosedThisWeek = g.Count(t => t.ClosedAt >= startOfWeekUtc),
                    ClosedThisMonth = g.Count(),
                })
                .OrderByDescending(t => t.ClosedThisMonth)
                .ThenByDescending(t => t.ClosedThisWeek)
                .ThenByDescending(t => t.ClosedToday)
                .ToList();

            return new QueryResponseDto<LeaderboardDto>(
                new LeaderboardDto { StaffLeaderboard = staffStats, TeamLeaderboard = teamStats }
            );
        }
    }
}

public record LeaderboardDto
{
    public List<StaffLeaderboardEntry> StaffLeaderboard { get; init; } = new();
    public List<TeamLeaderboardEntry> TeamLeaderboard { get; init; } = new();
}

public record StaffLeaderboardEntry
{
    public Guid StaffId { get; init; }
    public string StaffName { get; init; } = string.Empty;
    public int ClosedToday { get; init; }
    public int ClosedThisWeek { get; init; }
    public int ClosedThisMonth { get; init; }
}

public record TeamLeaderboardEntry
{
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public int ClosedToday { get; init; }
    public int ClosedThisWeek { get; init; }
    public int ClosedThisMonth { get; init; }
}
