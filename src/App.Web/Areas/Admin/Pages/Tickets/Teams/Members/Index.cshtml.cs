using App.Application.Common.Interfaces;
using App.Application.Teams;
using App.Application.Teams.Commands;
using App.Application.Teams.Queries;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Areas.Admin.Pages.Teams.Members;

/// <summary>
/// Page model for managing team members.
/// </summary>
public class Index : BaseAdminPageModel
{
    private readonly ITicketPermissionService _permissionService;
    private readonly IAppDbContext _db;

    public Index(ITicketPermissionService permissionService, IAppDbContext db)
    {
        _permissionService = permissionService;
        _db = db;
    }

    public TeamDto Team { get; set; } = null!;
    public IEnumerable<TeamMembershipDto> Members { get; set; } =
        Enumerable.Empty<TeamMembershipDto>();
    public List<AvailableUserItem> AvailableUsers { get; set; } = new();
    public bool CanManageTeams { get; set; }

    [BindProperty]
    public ShortGuid? SelectedUserId { get; set; }

    public async Task<IActionResult> OnGet(string teamId, CancellationToken cancellationToken)
    {
        CanManageTeams = _permissionService.CanManageTeams();

        var teamResponse = await Mediator.Send(
            new GetTeamById.Query { Id = teamId },
            cancellationToken
        );
        Team = teamResponse.Result;

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Teams",
                RouteName = RouteNames.Teams.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Team Members",
                RouteName = RouteNames.Teams.Members.Index,
                IsActive = true,
                RouteValues = new Dictionary<string, string> { { "teamId", teamId } },
            }
        );

        var membersResponse = await Mediator.Send(
            new GetTeamMembers.Query { TeamId = teamId },
            cancellationToken
        );
        Members = membersResponse.Result;

        await LoadAvailableUsersAsync(teamId, cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAddMember(
        string teamId,
        CancellationToken cancellationToken
    )
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        if (!SelectedUserId.HasValue)
        {
            SetErrorMessage("Please select a user to add.");
            return await OnGet(teamId, cancellationToken);
        }

        var command = new AddTeamMember.Command
        {
            TeamId = teamId,
            StaffAdminId = SelectedUserId.Value,
            IsAssignable = true,
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Member added successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Teams.Members.Index, new { teamId });
    }

    public async Task<IActionResult> OnPostRemoveMember(
        string teamId,
        string membershipId,
        CancellationToken cancellationToken
    )
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        var command = new RemoveTeamMember.Command { Id = membershipId };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Member removed successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Teams.Members.Index, new { teamId });
    }

    public async Task<IActionResult> OnPostToggleAssignable(
        string teamId,
        string membershipId,
        bool isAssignable,
        CancellationToken cancellationToken
    )
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        var command = new SetMemberAssignable.Command
        {
            Id = membershipId,
            IsAssignable = isAssignable,
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Teams.Members.Index, new { teamId });
    }

    private async Task LoadAvailableUsersAsync(string teamId, CancellationToken cancellationToken)
    {
        var teamGuid = new ShortGuid(teamId).Guid;
        var existingMemberIds = await _db
            .TeamMemberships.Where(m => m.TeamId == teamGuid)
            .Select(m => m.StaffAdminId)
            .ToListAsync(cancellationToken);

        AvailableUsers = await _db
            .Users.AsNoTracking()
            .Where(u => u.IsAdmin && u.IsActive && !existingMemberIds.Contains(u.Id))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new AvailableUserItem
            {
                Id = u.Id,
                Name = u.FirstName + " " + u.LastName,
                Email = u.EmailAddress,
            })
            .ToListAsync(cancellationToken);
    }

    public record AvailableUserItem
    {
        public ShortGuid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
