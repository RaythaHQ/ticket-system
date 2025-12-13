using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetAssigneeSelectOptions
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<AssigneeSelectOptionDto>>>
    {
        public bool CanManageTickets { get; init; }
        public ShortGuid? CurrentUserId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<AssigneeSelectOptionDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<AssigneeSelectOptionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var assignees = new List<AssigneeSelectOptionDto>();

            // Get current user's team memberships
            var userTeamIds = new HashSet<Guid>();
            if (request.CurrentUserId.HasValue)
            {
                userTeamIds = await _db
                    .TeamMemberships.AsNoTracking()
                    .Where(m => m.StaffAdminId == request.CurrentUserId.Value.Guid)
                    .Select(m => m.TeamId)
                    .ToHashSetAsync(cancellationToken);
            }

            // Add "Unassigned" option
            assignees.Add(
                new AssigneeSelectOptionDto
                {
                    Value = "unassigned",
                    DisplayText = "Unassigned",
                    TeamId = null,
                    AssigneeId = null
                }
            );

            // Load teams with their members
            var teams = await _db
                .Teams.AsNoTracking()
                .Include(t => t.Memberships)
                .ThenInclude(m => m.StaffAdmin)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            foreach (var team in teams)
            {
                // Add "Team Name/Anyone" option (team assigned, individual unassigned)
                var teamShortGuid = new ShortGuid(team.Id);
                assignees.Add(
                    new AssigneeSelectOptionDto
                    {
                        Value = $"team:{teamShortGuid}",
                        DisplayText = $"{team.Name}/Anyone",
                        TeamId = teamShortGuid,
                        AssigneeId = null
                    }
                );

                // Add individual team members if:
                // 1. User has CanManageTickets permission (can assign to anyone in any team), OR
                // 2. User is a member of this team (can assign to others in their team)
                var showTeamMembers = request.CanManageTickets || userTeamIds.Contains(team.Id);

                if (showTeamMembers)
                {
                    var members = team.Memberships
                        .Where(m => m.StaffAdmin != null && m.StaffAdmin.IsActive)
                        .OrderBy(m => m.StaffAdmin!.FirstName)
                        .ThenBy(m => m.StaffAdmin!.LastName)
                        .ToList();

                    foreach (var member in members)
                    {
                        var memberShortGuid = new ShortGuid(member.StaffAdminId);
                        assignees.Add(
                            new AssigneeSelectOptionDto
                            {
                                Value = $"team:{teamShortGuid}:assignee:{memberShortGuid}",
                                DisplayText =
                                    $"{team.Name}/{member.StaffAdmin!.FirstName} {member.StaffAdmin!.LastName}",
                                TeamId = teamShortGuid,
                                AssigneeId = memberShortGuid
                            }
                        );
                    }
                }
            }

            return new QueryResponseDto<IEnumerable<AssigneeSelectOptionDto>>(assignees);
        }
    }
}

public record AssigneeSelectOptionDto
{
    public string Value { get; init; } = string.Empty;
    public string DisplayText { get; init; } = string.Empty;
    public ShortGuid? TeamId { get; init; }
    public ShortGuid? AssigneeId { get; init; }
}

