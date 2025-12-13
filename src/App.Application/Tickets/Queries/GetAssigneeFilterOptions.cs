using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetAssigneeFilterOptions
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<AssigneeFilterOptionDto>>>
    {
        public string? BuiltInView { get; init; }
        public ShortGuid? CurrentUserId { get; init; }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<IEnumerable<AssigneeFilterOptionDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<AssigneeFilterOptionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var assignees = new List<AssigneeFilterOptionDto>();

            // Get user's team memberships for filtering
            var userTeamIds = new HashSet<Guid>();
            if (request.CurrentUserId.HasValue)
            {
                userTeamIds = await _db
                    .TeamMemberships.AsNoTracking()
                    .Where(m => m.StaffAdminId == request.CurrentUserId.Value.Guid)
                    .Select(m => m.TeamId)
                    .ToHashSetAsync(cancellationToken);
            }

            // Filter options based on built-in view
            switch (request.BuiltInView)
            {
                case "unassigned":
                    // Unassigned view: only show options that have no individual assignee
                    // Show "Unassigned" (no team, no individual) and "Team/Anyone" (team assigned, no individual)
                    assignees.Add(
                        new AssigneeFilterOptionDto
                        {
                            Value = "unassigned",
                            DisplayText = "Unassigned",
                        }
                    );

                    var unassignedTeams = await _db
                        .Teams.AsNoTracking()
                        .OrderBy(t => t.Name)
                        .ToListAsync(cancellationToken);

                    foreach (var team in unassignedTeams)
                    {
                        var teamShortGuid = new ShortGuid(team.Id);
                        assignees.Add(
                            new AssigneeFilterOptionDto
                            {
                                Value = $"team:{teamShortGuid}",
                                DisplayText = $"{team.Name}/Anyone",
                            }
                        );
                    }
                    break;

                case "my-tickets":
                    // My Tickets view: only show the current user
                    if (request.CurrentUserId.HasValue)
                    {
                        // Find which team(s) the user belongs to
                        var userTeams = await _db
                            .Teams.AsNoTracking()
                            .Include(t => t.Memberships)
                            .ThenInclude(m => m.StaffAdmin)
                            .Where(t =>
                                t.Memberships.Any(m =>
                                    m.StaffAdminId == request.CurrentUserId.Value.Guid
                                )
                            )
                            .OrderBy(t => t.Name)
                            .ToListAsync(cancellationToken);

                        foreach (var team in userTeams)
                        {
                            var membership = team.Memberships.FirstOrDefault(m =>
                                m.StaffAdminId == request.CurrentUserId.Value.Guid
                            );
                            if (membership?.StaffAdmin != null)
                            {
                                var teamShortGuid = new ShortGuid(team.Id);
                                var memberShortGuid = new ShortGuid(membership.StaffAdminId);
                                assignees.Add(
                                    new AssigneeFilterOptionDto
                                    {
                                        Value = $"team:{teamShortGuid}:assignee:{memberShortGuid}",
                                        DisplayText =
                                            $"{team.Name}/{membership.StaffAdmin.FirstName} {membership.StaffAdmin.LastName}",
                                    }
                                );
                            }
                        }
                    }
                    break;

                case "team-tickets":
                    // Team Tickets view: only show teams the user belongs to (and their members)
                    assignees.Add(
                        new AssigneeFilterOptionDto
                        {
                            Value = "unassigned",
                            DisplayText = "Unassigned",
                        }
                    );

                    if (userTeamIds.Any())
                    {
                        var userTeams = await _db
                            .Teams.AsNoTracking()
                            .Include(t => t.Memberships)
                            .ThenInclude(m => m.StaffAdmin)
                            .Where(t => userTeamIds.Contains(t.Id))
                            .OrderBy(t => t.Name)
                            .ToListAsync(cancellationToken);

                        foreach (var team in userTeams)
                        {
                            var teamShortGuid = new ShortGuid(team.Id);
                            
                            // Team/Unassigned - tickets assigned to this team but with no individual
                            assignees.Add(
                                new AssigneeFilterOptionDto
                                {
                                    Value = $"team:{teamShortGuid}:unassigned",
                                    DisplayText = $"{team.Name}/Unassigned",
                                }
                            );
                            
                            // Team/Anyone - all tickets for this team regardless of individual assignment
                            assignees.Add(
                                new AssigneeFilterOptionDto
                                {
                                    Value = $"team:{teamShortGuid}",
                                    DisplayText = $"{team.Name}/Anyone",
                                }
                            );

                            var members = team
                                .Memberships.Where(m =>
                                    m.StaffAdmin != null && m.StaffAdmin.IsActive
                                )
                                .OrderBy(m => m.StaffAdmin!.FirstName)
                                .ThenBy(m => m.StaffAdmin!.LastName)
                                .ToList();

                            foreach (var member in members)
                            {
                                var memberShortGuid = new ShortGuid(member.StaffAdminId);
                                assignees.Add(
                                    new AssigneeFilterOptionDto
                                    {
                                        Value = $"team:{teamShortGuid}:assignee:{memberShortGuid}",
                                        DisplayText =
                                            $"{team.Name}/{member.StaffAdmin!.FirstName} {member.StaffAdmin!.LastName}",
                                    }
                                );
                            }
                        }
                    }
                    break;

                case "created-by-me":
                case "my-opened":
                case "all":
                case null:
                case "":
                default:
                    // All Tickets, Created by Me, or no view: show all options
                    assignees.Add(
                        new AssigneeFilterOptionDto
                        {
                            Value = "unassigned",
                            DisplayText = "Unassigned",
                        }
                    );

                    var allTeams = await _db
                        .Teams.AsNoTracking()
                        .Include(t => t.Memberships)
                        .ThenInclude(m => m.StaffAdmin)
                        .OrderBy(t => t.Name)
                        .ToListAsync(cancellationToken);

                    foreach (var team in allTeams)
                    {
                        var teamShortGuid = new ShortGuid(team.Id);
                        
                        // Team/Unassigned - tickets assigned to this team but with no individual
                        assignees.Add(
                            new AssigneeFilterOptionDto
                            {
                                Value = $"team:{teamShortGuid}:unassigned",
                                DisplayText = $"{team.Name}/Unassigned",
                            }
                        );
                        
                        // Team/Anyone - all tickets for this team regardless of individual assignment
                        assignees.Add(
                            new AssigneeFilterOptionDto
                            {
                                Value = $"team:{teamShortGuid}",
                                DisplayText = $"{team.Name}/Anyone",
                            }
                        );

                        var members = team
                            .Memberships.Where(m => m.StaffAdmin != null && m.StaffAdmin.IsActive)
                            .OrderBy(m => m.StaffAdmin!.FirstName)
                            .ThenBy(m => m.StaffAdmin!.LastName)
                            .ToList();

                        foreach (var member in members)
                        {
                            var memberShortGuid = new ShortGuid(member.StaffAdminId);
                            assignees.Add(
                                new AssigneeFilterOptionDto
                                {
                                    Value = $"team:{teamShortGuid}:assignee:{memberShortGuid}",
                                    DisplayText =
                                        $"{team.Name}/{member.StaffAdmin!.FirstName} {member.StaffAdmin!.LastName}",
                                }
                            );
                        }
                    }
                    break;
            }

            return new QueryResponseDto<IEnumerable<AssigneeFilterOptionDto>>(assignees);
        }
    }
}

public record AssigneeFilterOptionDto
{
    public string Value { get; init; } = string.Empty;
    public string DisplayText { get; init; } = string.Empty;
}
