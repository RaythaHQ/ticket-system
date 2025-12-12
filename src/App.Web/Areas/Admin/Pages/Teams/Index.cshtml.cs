using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Teams;
using App.Application.Teams.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using App.Application.Common.Interfaces;

namespace App.Web.Areas.Admin.Pages.Teams;

/// <summary>
/// Page model for displaying a list of teams.
/// </summary>
public class Index : BaseAdminPageModel, IHasListView<Index.TeamListItemViewModel>
{
    private readonly ITicketPermissionService _permissionService;

    public Index(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public ListViewModel<TeamListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<TeamListItemViewModel>(), 0);

    public bool CanManageTeams { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"Name {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        CanManageTeams = _permissionService.CanManageTeams();

        var input = new GetTeams.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await Mediator.Send(input, cancellationToken);

        var items = response.Result.Items.Select(t => new TeamListItemViewModel
        {
            Id = t.Id.ToString(),
            Name = t.Name,
            Description = t.Description ?? "-",
            MemberCount = t.MemberCount,
            AssignableMemberCount = t.AssignableMemberCount,
            RoundRobinEnabled = t.RoundRobinEnabled ? "Yes" : "No",
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(t.CreationTime)
        });

        ListView = new ListViewModel<TeamListItemViewModel>(items, response.Result.TotalCount);

        return Page();
    }

    public record TeamListItemViewModel
    {
        public string Id { get; init; } = string.Empty;

        [Display(Name = "Name")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Members")]
        public int MemberCount { get; init; }

        [Display(Name = "Assignable")]
        public int AssignableMemberCount { get; init; }

        [Display(Name = "Round Robin")]
        public string RoundRobinEnabled { get; init; } = string.Empty;

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;
    }
}

