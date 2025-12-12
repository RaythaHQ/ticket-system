using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Interfaces;
using App.Application.Teams;
using App.Application.Teams.Commands;
using App.Application.Teams.Queries;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;

namespace App.Web.Areas.Admin.Pages.Teams;

/// <summary>
/// Page model for deleting a team.
/// </summary>
public class Delete : BaseAdminPageModel
{
    private readonly ITicketPermissionService _permissionService;

    public Delete(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public TeamDto Team { get; set; } = null!;

    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken)
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        var response = await Mediator.Send(new GetTeamById.Query { Id = id }, cancellationToken);
        Team = response.Result;

        return Page();
    }

    public async Task<IActionResult> OnPost(string id, CancellationToken cancellationToken)
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        var command = new DeleteTeam.Command { Id = id };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Team deleted successfully.");
            return RedirectToPage(RouteNames.Teams.Index);
        }

        SetErrorMessage(response.GetErrors());
        return await OnGet(id, cancellationToken);
    }
}

