using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetTeamReport
{
    public record Query : IRequest<QueryResponseDto<TeamReportDto>>
    {
        public ShortGuid TeamId { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
    }

    public class Handler : IRequestHandler<Query, QueryResponseDto<TeamReportDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<QueryResponseDto<TeamReportDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanAccessReports();

            var teamId = request.TeamId.Guid;
            var team = await _db
                .Teams.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

            if (team == null)
                throw new NotFoundException("Team", teamId);

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

            // Tickets CREATED in the period for this team
            var ticketsCreated = await _db
                .Tickets.AsNoTracking()
                .Where(t => t.OwningTeamId == teamId)
                .Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate)
                .ToListAsync(cancellationToken);

            // Tickets CLOSED in the period for this team (regardless of when created)
            var ticketsClosed = await _db
                .Tickets.AsNoTracking()
                .Where(t => t.OwningTeamId == teamId)
                .Where(t =>
                    t.ClosedAt.HasValue
                    && t.ClosedAt.Value >= startDate
                    && t.ClosedAt.Value <= endDate
                    && closedStatusNames.Contains(t.Status))
                .ToListAsync(cancellationToken);

            var totalTickets = ticketsCreated.Count;
            var openTickets = ticketsCreated.Count(t => !closedStatusNames.Contains(t.Status));
            var resolvedTickets = ticketsClosed.Count(t => t.ResolvedAt.HasValue);
            var closedTickets = ticketsClosed.Count;
            var unassignedTickets = ticketsCreated.Count(t =>
                t.AssigneeId == null && !closedStatusNames.Contains(t.Status)
            );

            var slaBreachedCount = ticketsCreated.Count(t => t.SlaStatus == SlaStatus.BREACHED);
            var slaMetCount = ticketsCreated.Count(t => t.SlaStatus == SlaStatus.COMPLETED);
            var slaTicketsWithResult = slaBreachedCount + slaMetCount;
            var slaComplianceRate =
                slaTicketsWithResult > 0 ? (double)slaMetCount / slaTicketsWithResult * 100 : 100;

            // Resolution times: use tickets closed in the period
            var resolvedWithTimes = ticketsClosed
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

            // Member breakdown
            var teamMembers = await _db
                .TeamMemberships.AsNoTracking()
                .Where(m => m.TeamId == teamId)
                .Include(m => m.StaffAdmin)
                .ToListAsync(cancellationToken);

            var memberMetrics = new List<MemberMetricsDto>();
            foreach (var member in teamMembers)
            {
                var memberTicketsCreated = ticketsCreated
                    .Where(t => t.AssigneeId == member.StaffAdminId)
                    .ToList();
                var memberTicketsClosed = ticketsClosed
                    .Where(t => t.AssigneeId == member.StaffAdminId)
                    .ToList();

                // Calculate median resolution time from tickets closed by this member
                double? memberMedianTime = null;
                if (memberTicketsClosed.Any())
                {
                    var resolutionTimes = memberTicketsClosed
                        .Where(t => t.ClosedAt.HasValue)
                        .Select(t => (t.ClosedAt!.Value - t.CreationTime).TotalHours)
                        .OrderBy(h => h)
                        .ToList();
                    if (resolutionTimes.Any())
                    {
                        int mid = resolutionTimes.Count / 2;
                        memberMedianTime = resolutionTimes.Count % 2 == 0
                            ? (resolutionTimes[mid - 1] + resolutionTimes[mid]) / 2
                            : resolutionTimes[mid];
                    }
                }

                memberMetrics.Add(
                    new MemberMetricsDto
                    {
                        UserId = member.StaffAdminId,
                        UserName = member.StaffAdmin?.FullName ?? "Unknown",
                        AssignedTickets = memberTicketsCreated.Count,
                        ResolvedTickets = memberTicketsClosed.Count,
                        MedianResolutionTimeHours = memberMedianTime.HasValue
                            ? Math.Round(memberMedianTime.Value, 2)
                            : null,
                    }
                );
            }

            return new QueryResponseDto<TeamReportDto>(
                new TeamReportDto
                {
                    TeamId = teamId,
                    TeamName = team.Name,
                    TotalTickets = totalTickets,
                    OpenTickets = openTickets,
                    ResolvedTickets = resolvedTickets,
                    ClosedTickets = closedTickets,
                    UnassignedTickets = unassignedTickets,
                    SlaBreachedCount = slaBreachedCount,
                    SlaMetCount = slaMetCount,
                    SlaComplianceRate = Math.Round(slaComplianceRate, 2),
                    AverageResolutionTimeHours = avgResolutionTime.HasValue
                        ? Math.Round(avgResolutionTime.Value, 2)
                        : null,
                    MedianResolutionTimeHours = medianResolutionTime.HasValue
                        ? Math.Round(medianResolutionTime.Value, 2)
                        : null,
                    MemberMetrics = memberMetrics,
                }
            );
        }
    }
}
