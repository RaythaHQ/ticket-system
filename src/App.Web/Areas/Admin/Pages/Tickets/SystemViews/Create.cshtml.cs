using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.Teams.Queries;
using App.Application.TicketViews;
using App.Application.TicketViews.Commands;
using App.Application.Users.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using App.Web.Areas.Staff.Pages.Shared.Models;
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
    public List<string> VisibleColumns { get; set; } = new();

    // Advanced view models for partials
    public FilterBuilderViewModel FilterBuilder { get; set; } = new();
    public SortConfiguratorViewModel SortConfigurator { get; set; } = new();
    public ColumnSelectorViewModel ColumnSelector { get; set; } = new();

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
        await LoadAdvancedViewModelsAsync(cancellationToken);
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
            await LoadAdvancedViewModelsAsync(cancellationToken);
            return Page();
        }

        // Get columns from form directly
        var formColumns = Request.Form["VisibleColumns"];
        var columnList = formColumns.Count > 0 ? formColumns.ToList() : VisibleColumns;

        // Build conditions from form
        var conditions = ParseConditionsFromForm();

        // Build sort levels from form
        var sortLevels = ParseSortLevelsFromForm();

        // Validate at least one column is selected
        if (!columnList.Any())
        {
            SetErrorMessage("You must select at least one column.");
            await LoadOptionsAsync(cancellationToken);
            await LoadAdvancedViewModelsAsync(cancellationToken);
            return Page();
        }

        var response = await Mediator.Send(
            new CreateTicketView.Command
            {
                Name = Form.Name,
                Description = Form.Description,
                OwnerUserId = null, // System views don't have an owner
                IsSystemView = true,
                IsDefault = Form.IsDefault,
                Conditions = conditions,
                SortLevels = sortLevels,
                VisibleColumns = columnList,
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
        await LoadAdvancedViewModelsAsync(cancellationToken);
        return Page();
    }

    private ViewConditions? ParseConditionsFromForm()
    {
        var logic = Request.Form["Conditions.Logic"].FirstOrDefault() ?? "AND";
        var filters = new List<ViewFilterCondition>();

        // Parse filter conditions
        var index = 0;
        while (Request.Form.ContainsKey($"Conditions.Filters[{index}].Field"))
        {
            var field = Request.Form[$"Conditions.Filters[{index}].Field"].FirstOrDefault();
            var op = Request.Form[$"Conditions.Filters[{index}].Operator"].FirstOrDefault();
            var value = Request.Form[$"Conditions.Filters[{index}].Value"].FirstOrDefault();

            if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(op))
            {
                filters.Add(new ViewFilterCondition
                {
                    Field = field,
                    Operator = op,
                    Value = value
                });
            }
            index++;
        }

        if (!filters.Any())
            return null;

        return new ViewConditions
        {
            Logic = logic,
            Filters = filters
        };
    }

    private List<CreateTicketView.SortLevelInput>? ParseSortLevelsFromForm()
    {
        var sortLevels = new List<CreateTicketView.SortLevelInput>();
        var index = 0;

        while (Request.Form.ContainsKey($"SortLevels[{index}].Field"))
        {
            var field = Request.Form[$"SortLevels[{index}].Field"].FirstOrDefault();
            var direction = Request.Form[$"SortLevels[{index}].Direction"].FirstOrDefault() ?? "asc";
            var orderStr = Request.Form[$"SortLevels[{index}].Order"].FirstOrDefault();
            var order = int.TryParse(orderStr, out var o) ? o : index;

            if (!string.IsNullOrEmpty(field))
            {
                sortLevels.Add(new CreateTicketView.SortLevelInput
                {
                    Order = order,
                    Field = field,
                    Direction = direction
                });
            }
            index++;
        }

        return sortLevels.Any() ? sortLevels.OrderBy(s => s.Order).ToList() : null;
    }

    private async Task LoadAdvancedViewModelsAsync(CancellationToken cancellationToken)
    {
        // Load users for filter dropdowns
        var usersResponse = await Mediator.Send(new GetUsers.Query { PageSize = 1000 }, cancellationToken);
        var users = usersResponse.Result.Items.Select(u => new UserOption
        {
            Id = u.Id.ToString(),
            Name = u.FullName,
            IsDeactivated = !u.IsActive
        }).ToList();

        // Load teams
        var teamsResponse = await Mediator.Send(new GetTeams.Query(), cancellationToken);
        var teams = teamsResponse.Result.Items.Select(t => new TeamOption
        {
            Id = t.Id.ToString(),
            Name = t.Name
        }).ToList();

        // Load statuses
        var statuses = await _configService.GetAllStatusesAsync(includeInactive: true, cancellationToken);
        var statusOptions = statuses.Select(s => new SelectOption
        {
            Value = s.DeveloperName,
            Label = s.Label + (s.IsActive ? "" : " (inactive)")
        }).ToList();

        // Load priorities
        var priorities = await _configService.GetAllPrioritiesAsync(includeInactive: true, cancellationToken);
        var priorityOptions = priorities.Select(p => new SelectOption
        {
            Value = p.DeveloperName,
            Label = p.Label + (p.IsActive ? "" : " (inactive)")
        }).ToList();

        // Filter Builder
        FilterBuilder = FilterBuilderViewModel.CreateWithDefaults();
        FilterBuilder.Users = users;
        FilterBuilder.Teams = teams;
        FilterBuilder.Statuses = statusOptions;
        FilterBuilder.Priorities = priorityOptions;

        // Sort Configurator
        SortConfigurator = new SortConfiguratorViewModel
        {
            SortableFields = FilterAttributes.All
                .Where(a => a.IsSortable)
                .Select(a => new SortFieldModel { Field = a.Field, Label = a.Label })
                .ToList()
        };

        // Column Selector
        ColumnSelector = new ColumnSelectorViewModel
        {
            AvailableColumns = ColumnRegistry.Columns
                .Select(c => new ColumnModel { Field = c.Field, Label = c.Label })
                .ToList(),
            SelectedColumns = new List<string> { "Id", "Title", "Status", "Priority", "AssigneeName", "CreationTime" }
        };
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
