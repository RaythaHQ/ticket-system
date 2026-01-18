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

            var tickets = await _db
                .Tickets.AsNoTracking()
                .Where(t => t.OwningTeamId == teamId)
                .Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate)
                .ToListAsync(cancellationToken);

            var totalTickets = tickets.Count;
            var openTickets = tickets.Count(t =>
                t.Status != TicketStatus.CLOSED && t.Status != TicketStatus.RESOLVED
            );
            var resolvedTickets = tickets.Count(t => t.ResolvedAt.HasValue);
            var closedTickets = tickets.Count(t => t.Status == TicketStatus.CLOSED);
            var unassignedTickets = tickets.Count(t =>
                t.AssigneeId == null && t.Status != TicketStatus.CLOSED
            );

            var slaBreachedCount = tickets.Count(t => t.SlaStatus == SlaStatus.BREACHED);
            var slaMetCount = tickets.Count(t => t.SlaStatus == SlaStatus.COMPLETED);
            var slaTicketsWithResult = slaBreachedCount + slaMetCount;
            var slaComplianceRate =
                slaTicketsWithResult > 0 ? (double)slaMetCount / slaTicketsWithResult * 100 : 100;

            var resolvedWithTimes = tickets
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

            // Member breakdown
            var teamMembers = await _db
                .TeamMemberships.AsNoTracking()
                .Where(m => m.TeamId == teamId)
                .Include(m => m.StaffAdmin)
                .ToListAsync(cancellationToken);

            var memberMetrics = new List<MemberMetricsDto>();
            foreach (var member in teamMembers)
            {
                var memberTickets = tickets
                    .Where(t => t.AssigneeId == member.StaffAdminId)
                    .ToList();
                var memberResolved = memberTickets.Where(t => t.ResolvedAt.HasValue).ToList();
                var memberAvgTime = memberResolved.Any()
                    ? memberResolved.Average(t => (t.ResolvedAt!.Value - t.CreationTime).TotalHours)
                    : (double?)null;

                memberMetrics.Add(
                    new MemberMetricsDto
                    {
                        UserId = member.StaffAdminId,
                        UserName = member.StaffAdmin?.FullName ?? "Unknown",
                        AssignedTickets = memberTickets.Count,
                        ResolvedTickets = memberResolved.Count,
                        AverageResolutionTimeHours = memberAvgTime,
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
