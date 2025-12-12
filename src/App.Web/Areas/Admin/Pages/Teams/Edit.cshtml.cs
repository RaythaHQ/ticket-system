using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Interfaces;
using App.Application.Teams;
using App.Application.Teams.Commands;
using App.Application.Teams.Queries;
using App.Web.Areas.Admin.Pages.Shared.Models;
using CSharpVitamins;

namespace App.Web.Areas.Admin.Pages.Teams;

/// <summary>
/// Page model for editing a team.
/// </summary>
public class Edit : BaseAdminPageModel
{
    private readonly ITicketPermissionService _permissionService;

    public Edit(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public TeamDto Team { get; set; } = null!;

    [BindProperty]
    public EditTeamViewModel Form { get; set; } = new();

    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken)
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        var response = await Mediator.Send(new GetTeamById.Query { Id = id }, cancellationToken);
        Team = response.Result;

        Form = new EditTeamViewModel
        {
            Id = Team.Id.ToString(),
            Name = Team.Name,
            Description = Team.Description,
            RoundRobinEnabled = Team.RoundRobinEnabled
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        if (!ModelState.IsValid)
            return Page();

        var command = new UpdateTeam.Command
        {
            Id = Form.Id,
            Name = Form.Name,
            Description = Form.Description,
            RoundRobinEnabled = Form.RoundRobinEnabled
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Team updated successfully.");
            return RedirectToPage("./Index");
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record EditTeamViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Enable Round Robin")]
        public bool RoundRobinEnabled { get; set; }
    }
}

