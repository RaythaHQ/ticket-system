using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketTasks.Queries;

public class GetTaskReports
{
    public record Query : IRequest<QueryResponseDto<TaskReportDto>>
    {
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
    }

    public class Handler : IRequestHandler<Query, QueryResponseDto<TaskReportDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<QueryResponseDto<TaskReportDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            // Ensure dates are UTC for PostgreSQL
            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
            var nowUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            // Load tasks created in the period (with navigation properties for breakdowns)
            var tasksCreatedInPeriod = await _db.TicketTasks.AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate)
                .ToListAsync(cancellationToken);

            // Tasks completed in the period
            var tasksCompletedInPeriod = await _db.TicketTasks.AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Where(t => t.ClosedAt.HasValue
                    && t.ClosedAt.Value >= startDate
                    && t.ClosedAt.Value <= endDate)
                .ToListAsync(cancellationToken);

            // Tasks reopened in the period (use change log entries)
            var tasksReopenedInPeriod = await _db.TicketChangeLogEntries.AsNoTracking()
                .Where(e => e.CreationTime >= startDate
                    && e.CreationTime <= endDate
                    && e.Message != null
                    && e.Message.StartsWith("Task reopened:"))
                .CountAsync(cancellationToken);

            // Current open count
            var currentOpenTasks = await _db.TicketTasks.AsNoTracking()
                .Where(t => t.Status == TicketTaskStatus.OPEN)
                .CountAsync(cancellationToken);

            // Overdue count (open + DueAt < UtcNow)
            var overdueTasks = await _db.TicketTasks.AsNoTracking()
                .Where(t => t.Status == TicketTaskStatus.OPEN
                    && t.DueAt.HasValue
                    && t.DueAt.Value < nowUtc)
                .CountAsync(cancellationToken);

            // Blocked count
            var blockedTasks = await _db.TicketTasks.AsNoTracking()
                .Include(t => t.DependsOnTask)
                .Where(t => t.Status == TicketTaskStatus.OPEN
                    && t.DependsOnTaskId != null
                    && t.DependsOnTask!.Status != TicketTaskStatus.CLOSED)
                .CountAsync(cancellationToken);

            // Daily breakdown for chart data
            var dailyBreakdown = new List<DailyTaskStatsDto>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayStart = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1);

                dailyBreakdown.Add(new DailyTaskStatsDto
                {
                    Date = dayStart,
                    TasksCreated = tasksCreatedInPeriod.Count(t => t.CreationTime >= dayStart && t.CreationTime < dayEnd),
                    TasksCompleted = tasksCompletedInPeriod.Count(t => t.ClosedAt!.Value >= dayStart && t.ClosedAt!.Value < dayEnd),
                });
            }

            // ── Team breakdown ──
            var teams = await _db.Teams.AsNoTracking()
                .Include(t => t.Memberships)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            // Also load all current tasks with team info for "current open" per team
            var allCurrentTasks = await _db.TicketTasks.AsNoTracking()
                .Where(t => t.Status == TicketTaskStatus.OPEN)
                .ToListAsync(cancellationToken);

            var teamBreakdown = teams.Select(team =>
            {
                var created = tasksCreatedInPeriod.Count(t => t.OwningTeamId == team.Id);
                var completed = tasksCompletedInPeriod.Count(t => t.OwningTeamId == team.Id);
                var open = allCurrentTasks.Count(t => t.OwningTeamId == team.Id);
                var overdue = allCurrentTasks.Count(t =>
                    t.OwningTeamId == team.Id && t.DueAt.HasValue && t.DueAt.Value < nowUtc);

                return new TeamTaskBreakdownDto
                {
                    TeamId = new ShortGuid(team.Id),
                    TeamName = team.Name,
                    MemberCount = team.Memberships.Count,
                    TasksCreated = created,
                    TasksCompleted = completed,
                    CurrentOpen = open,
                    CurrentOverdue = overdue,
                };
            }).ToList();

            // Unassigned (team)
            var unassignedCreated = tasksCreatedInPeriod.Count(t => t.OwningTeamId == null);
            var unassignedCompleted = tasksCompletedInPeriod.Count(t => t.OwningTeamId == null);
            var unassignedOpen = allCurrentTasks.Count(t => t.OwningTeamId == null);
            var unassignedOverdue = allCurrentTasks.Count(t =>
                t.OwningTeamId == null && t.DueAt.HasValue && t.DueAt.Value < nowUtc);

            if (unassignedCreated > 0 || unassignedOpen > 0)
            {
                teamBreakdown.Add(new TeamTaskBreakdownDto
                {
                    TeamName = "Unassigned",
                    TasksCreated = unassignedCreated,
                    TasksCompleted = unassignedCompleted,
                    CurrentOpen = unassignedOpen,
                    CurrentOverdue = unassignedOverdue,
                });
            }

            // ── Individual breakdown ──
            // Get all users who had tasks created or completed in period, or have current open tasks
            var relevantUserIds = tasksCreatedInPeriod
                .Where(t => t.AssigneeId.HasValue).Select(t => t.AssigneeId!.Value)
                .Union(tasksCompletedInPeriod.Where(t => t.AssigneeId.HasValue).Select(t => t.AssigneeId!.Value))
                .Union(allCurrentTasks.Where(t => t.AssigneeId.HasValue).Select(t => t.AssigneeId!.Value))
                .Distinct()
                .ToList();

            var users = await _db.Users.AsNoTracking()
                .Where(u => relevantUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            var individualBreakdown = users.Select(user =>
            {
                var created = tasksCreatedInPeriod.Count(t => t.AssigneeId == user.Id);
                var completed = tasksCompletedInPeriod.Count(t => t.AssigneeId == user.Id);
                var open = allCurrentTasks.Count(t => t.AssigneeId == user.Id);
                var overdue = allCurrentTasks.Count(t =>
                    t.AssigneeId == user.Id && t.DueAt.HasValue && t.DueAt.Value < nowUtc);

                // Find the user's team name
                var teamName = tasksCreatedInPeriod
                    .Where(t => t.AssigneeId == user.Id && t.OwningTeam != null)
                    .Select(t => t.OwningTeam!.Name)
                    .FirstOrDefault()
                    ?? allCurrentTasks
                        .Where(t => t.AssigneeId == user.Id)
                        .Join(teams, t => t.OwningTeamId, tm => tm.Id, (t, tm) => tm.Name)
                        .FirstOrDefault();

                return new IndividualTaskBreakdownDto
                {
                    UserId = new ShortGuid(user.Id),
                    UserName = $"{user.FirstName} {user.LastName}".Trim(),
                    TeamName = teamName,
                    TasksCreated = created,
                    TasksCompleted = completed,
                    CurrentOpen = open,
                    CurrentOverdue = overdue,
                    CompletionRate = (created + completed) > 0
                        ? Math.Round((double)completed / (created + completed) * 100, 1)
                        : 0,
                };
            })
            .OrderByDescending(u => u.TasksCompleted)
            .ThenBy(u => u.UserName)
            .ToList();

            // Unassigned individual
            var indUnassignedCreated = tasksCreatedInPeriod.Count(t => t.AssigneeId == null);
            var indUnassignedCompleted = tasksCompletedInPeriod.Count(t => t.AssigneeId == null);
            var indUnassignedOpen = allCurrentTasks.Count(t => t.AssigneeId == null);
            if (indUnassignedCreated > 0 || indUnassignedOpen > 0)
            {
                individualBreakdown.Add(new IndividualTaskBreakdownDto
                {
                    UserName = "Unassigned",
                    TasksCreated = indUnassignedCreated,
                    TasksCompleted = indUnassignedCompleted,
                    CurrentOpen = indUnassignedOpen,
                    CurrentOverdue = allCurrentTasks.Count(t =>
                        t.AssigneeId == null && t.DueAt.HasValue && t.DueAt.Value < nowUtc),
                });
            }

            return new QueryResponseDto<TaskReportDto>(new TaskReportDto
            {
                ReportDate = DateTime.UtcNow,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TasksCreated = tasksCreatedInPeriod.Count,
                TasksCompleted = tasksCompletedInPeriod.Count,
                TasksReopened = tasksReopenedInPeriod,
                CurrentOpenTasks = currentOpenTasks,
                OverdueTasks = overdueTasks,
                BlockedTasks = blockedTasks,
                DailyBreakdown = dailyBreakdown,
                TeamBreakdown = teamBreakdown,
                IndividualBreakdown = individualBreakdown,
            });
        }
    }
}

public record TaskReportDto
{
    public DateTime ReportDate { get; init; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }

    public int TasksCreated { get; init; }
    public int TasksCompleted { get; init; }
    public int TasksReopened { get; init; }
    public int CurrentOpenTasks { get; init; }
    public int OverdueTasks { get; init; }
    public int BlockedTasks { get; init; }

    public List<DailyTaskStatsDto> DailyBreakdown { get; init; } = new();
    public List<TeamTaskBreakdownDto> TeamBreakdown { get; init; } = new();
    public List<IndividualTaskBreakdownDto> IndividualBreakdown { get; init; } = new();
}

public record DailyTaskStatsDto
{
    public DateTime Date { get; init; }
    public int TasksCreated { get; init; }
    public int TasksCompleted { get; init; }
}

public record TeamTaskBreakdownDto
{
    public ShortGuid? TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public int MemberCount { get; init; }
    public int TasksCreated { get; init; }
    public int TasksCompleted { get; init; }
    public int CurrentOpen { get; init; }
    public int CurrentOverdue { get; init; }
}

public record IndividualTaskBreakdownDto
{
    public ShortGuid? UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? TeamName { get; init; }
    public int TasksCreated { get; init; }
    public int TasksCompleted { get; init; }
    public int CurrentOpen { get; init; }
    public int CurrentOverdue { get; init; }
    public double CompletionRate { get; init; }
}
