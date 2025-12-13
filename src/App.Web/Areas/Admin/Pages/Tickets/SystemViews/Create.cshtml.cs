using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.Teams.Queries;
using App.Application.TicketViews.Commands;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Web.Areas.Admin.Pages.SystemViews;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_VIEWS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    private readonly ITicketConfigService _configService;

    public Create(ITicketConfigService configService)
    {
        _configService = configService;
    }
    [BindProperty]
    public CreateViewForm Form { get; set; } = new();

    [BindProperty]
    public List<string> SelectedColumnNames { get; set; } = new();

    public List<TeamSelectItem> AvailableTeams { get; set; } = new();
    public List<ColumnOption> AvailableColumns { get; set; } = new();
    public List<SelectListItem> AvailableStatuses { get; set; } = new();
    public List<SelectListItem> AvailablePriorities { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create System View";
        ViewData["ActiveMenu"] = "SystemViews";
        ViewData["ExpandTicketingMenu"] = true;

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "System Views",
                RouteName = RouteNames.SystemViews.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create system view",
                RouteName = RouteNames.SystemViews.Create,
                IsActive = true,
            }
        );

        await LoadOptionsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create System View";
        ViewData["ActiveMenu"] = "SystemViews";
        ViewData["ExpandTicketingMenu"] = true;

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        // Get columns from form directly
        var formColumns = Request.Form["SelectedColumnNames"];
        var columnList = formColumns.Count > 0 ? formColumns.ToList() : SelectedColumnNames;

        // Build filters
        var filters = new List<CreateTicketView.FilterCondition>();

        if (!string.IsNullOrEmpty(Form.StatusFilter))
        {
            filters.Add(
                new CreateTicketView.FilterCondition
                {
                    Field = "Status",
                    Operator = "equals",
                    Value = Form.StatusFilter,
                }
            );
        }

        if (!string.IsNullOrEmpty(Form.PriorityFilter))
        {
            filters.Add(
                new CreateTicketView.FilterCondition
                {
                    Field = "Priority",
                    Operator = "equals",
                    Value = Form.PriorityFilter,
                }
            );
        }

        if (Form.TeamIdFilter.HasValue)
        {
            filters.Add(
                new CreateTicketView.FilterCondition
                {
                    Field = "OwningTeamId",
                    Operator = "equals",
                    Value = Form.TeamIdFilter.Value.ToString(),
                }
            );
        }

        if (Form.UnassignedOnly)
        {
            filters.Add(
                new CreateTicketView.FilterCondition
                {
                    Field = "AssigneeId",
                    Operator = "is_null",
                    Value = "true",
                }
            );
        }

        // Get selected columns
        var selectedColumns = columnList.Any()
            ? columnList
            : new List<string> { "Id", "Title", "Status", "Priority", "CreationTime" };

        var response = await Mediator.Send(
            new CreateTicketView.Command
            {
                Name = Form.Name,
                Description = Form.Description,
                OwnerUserId = null, // System views don't have an owner
                IsSystemView = true,
                IsDefault = Form.IsDefault,
                Filters = filters,
                VisibleColumns = selectedColumns,
                SortField = Form.SortField ?? "CreationTime",
                SortDirection = Form.SortDescending ? "desc" : "asc",
            },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage("System view created successfully.");
            return RedirectToPage(RouteNames.SystemViews.Index);
        }

        SetErrorMessage(response.GetErrors());
        await LoadOptionsAsync(cancellationToken);
        return Page();
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        // Load teams
        var teamsResponse = await Mediator.Send(new GetTeams.Query(), cancellationToken);
        AvailableTeams = teamsResponse
            .Result.Items.Select(t => new TeamSelectItem { Id = t.Id, Name = t.Name })
            .ToList();

        // Status options from config
        var statuses = await _configService.GetAllStatusesAsync(includeInactive: true, cancellationToken);
        AvailableStatuses = statuses.Select(s => new SelectListItem
        {
            Value = s.DeveloperName,
            Text = s.Label + (s.IsActive ? "" : " (inactive)"),
        }).ToList();

        // Priority options from config
        var priorities = await _configService.GetAllPrioritiesAsync(includeInactive: true, cancellationToken);
        AvailablePriorities = priorities.Select(p => new SelectListItem
        {
            Value = p.DeveloperName,
            Text = p.Label + (p.IsActive ? "" : " (inactive)"),
        }).ToList();

        // Available columns
        AvailableColumns = new List<ColumnOption>
        {
            new()
            {
                Name = "Id",
                Label = "Ticket ID",
                Selected = true,
            },
            new()
            {
                Name = "Title",
                Label = "Title",
                Selected = true,
            },
            new()
            {
                Name = "Status",
                Label = "Status",
                Selected = true,
            },
            new()
            {
                Name = "Priority",
                Label = "Priority",
                Selected = true,
            },
            new()
            {
                Name = "Category",
                Label = "Category",
                Selected = false,
            },
            new()
            {
                Name = "OwningTeamName",
                Label = "Team",
                Selected = true,
            },
            new()
            {
                Name = "AssigneeName",
                Label = "Assignee",
                Selected = true,
            },
            new()
            {
                Name = "ContactName",
                Label = "Contact",
                Selected = false,
            },
            new()
            {
                Name = "SlaStatus",
                Label = "SLA Status",
                Selected = false,
            },
            new()
            {
                Name = "CreationTime",
                Label = "Created",
                Selected = true,
            },
            new()
            {
                Name = "LastModificationTime",
                Label = "Last Updated",
                Selected = false,
            },
        };
    }

    public class CreateViewForm
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; }

        // Filters
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public ShortGuid? TeamIdFilter { get; set; }
        public bool UnassignedOnly { get; set; }

        // Sorting
        public string? SortField { get; set; } = "CreationTime";
        public bool SortDescending { get; set; } = true;
    }

    public class ColumnOption
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }

    public class TeamSelectItem
    {
        public ShortGuid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
