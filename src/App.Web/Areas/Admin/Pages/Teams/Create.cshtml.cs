using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Interfaces;
using App.Application.Teams.Commands;
using App.Web.Areas.Admin.Pages.Shared.Models;

namespace App.Web.Areas.Admin.Pages.Teams;

/// <summary>
/// Page model for creating a new team.
/// </summary>
public class Create : BaseAdminPageModel
{
    private readonly ITicketPermissionService _permissionService;

    public Create(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [BindProperty]
    public CreateTeamViewModel Form { get; set; } = new();

    public IActionResult OnGet()
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!_permissionService.CanManageTeams())
            return Forbid();

        if (!ModelState.IsValid)
            return Page();

        var command = new CreateTeam.Command
        {
            Name = Form.Name,
            Description = Form.Description,
            RoundRobinEnabled = Form.RoundRobinEnabled
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage($"Team '{Form.Name}' created successfully.");
            return RedirectToPage("./Index");
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record CreateTeamViewModel
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Enable Round Robin")]
        public bool RoundRobinEnabled { get; set; }
    }
}

