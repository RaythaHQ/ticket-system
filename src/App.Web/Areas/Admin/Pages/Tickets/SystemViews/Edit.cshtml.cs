using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.Teams.Queries;
using App.Application.TicketViews;
using App.Application.TicketViews.Commands;
using App.Application.TicketViews.Queries;
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
public class Edit : BaseAdminPageModel
{
    private readonly ITicketConfigService _configService;

    public Edit(ITicketConfigService configService)
    {
        _configService = configService;
    }

    public ShortGuid ViewId { get; set; }

    [BindProperty]
    public EditViewForm Form { get; set; } = new();

    [BindProperty]
    public List<string> VisibleColumns { get; set; } = new();

    // Advanced view models for partials
    public FilterBuilderViewModel FilterBuilder { get; set; } = new();
    public SortConfiguratorViewModel SortConfigurator { get; set; } = new();
    public ColumnSelectorViewModel ColumnSelector { get; set; } = new();

    public List<TeamSelectItem> AvailableTeams { get; set; } = new();
    public List<SelectListItem> AvailableStatuses { get; set; } = new();
    public List<SelectListItem> AvailablePriorities { get; set; } = new();

    public async Task<IActionResult> OnGet(ShortGuid id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit System View";
        ViewData["ActiveMenu"] = "SystemViews";
        ViewData["ExpandTicketingMenu"] = true;

        ViewId = id;

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
                Label = "Edit system view",
                RouteName = RouteNames.SystemViews.Edit,
                IsActive = true,
                RouteValues = new Dictionary<string, string> { { "id", id.ToString() } },
            }
        );

        var response = await Mediator.Send(
            new GetTicketViewById.Query { Id = id },
            cancellationToken
        );
        if (!response.Success || response.Result == null)
        {
            SetErrorMessage("System view not found.");
            return RedirectToPage(RouteNames.SystemViews.Index);
        }

        var view = response.Result;
        if (!view.IsSystem)
        {
            SetErrorMessage("This is not a system view.");
            return RedirectToPage(RouteNames.SystemViews.Index);
        }

        Form = new EditViewForm
        {
            Name = view.Name,
            Description = view.Description,
            IsDefault = view.IsDefault,
        };

        await LoadOptionsAsync(cancellationToken);
        await LoadAdvancedViewModelsAsync(view, cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPost(ShortGuid id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit System View";
        ViewData["ActiveMenu"] = "SystemViews";
        ViewData["ExpandTicketingMenu"] = true;

        ViewId = id;

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(cancellationToken);
            await LoadAdvancedViewModelsAsync(null, cancellationToken);
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
            await LoadAdvancedViewModelsAsync(null, cancellationToken);
            return Page();
        }

        var response = await Mediator.Send(
            new UpdateTicketView.Command
            {
                Id = id,
                Name = Form.Name,
                Description = Form.Description,
                IsDefault = Form.IsDefault,
                Conditions = conditions,
                SortLevels = sortLevels,
                VisibleColumns = columnList,
            },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage("System view updated successfully.");
            return RedirectToPage(RouteNames.SystemViews.Index);
        }

        SetErrorMessage(response.GetErrors());
        await LoadOptionsAsync(cancellationToken);
        await LoadAdvancedViewModelsAsync(null, cancellationToken);
        return Page();
    }

    private ViewConditions? ParseConditionsFromForm()
    {
        var andFilters = new List<ViewFilterCondition>();
        var orFilters = new List<ViewFilterCondition>();

        // Parse AND filter conditions
        var index = 0;
        while (Request.Form.ContainsKey($"Conditions.AndFilters[{index}].Field"))
        {
            var field = Request.Form[$"Conditions.AndFilters[{index}].Field"].FirstOrDefault();
            var op = Request.Form[$"Conditions.AndFilters[{index}].Operator"].FirstOrDefault();
            var value = Request.Form[$"Conditions.AndFilters[{index}].Value"].FirstOrDefault();

            if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(op))
            {
                andFilters.Add(new ViewFilterCondition
                {
                    Field = field,
                    Operator = op,
                    Value = value
                });
            }
            index++;
        }

        // Parse OR filter conditions
        index = 0;
        while (Request.Form.ContainsKey($"Conditions.OrFilters[{index}].Field"))
        {
            var field = Request.Form[$"Conditions.OrFilters[{index}].Field"].FirstOrDefault();
            var op = Request.Form[$"Conditions.OrFilters[{index}].Operator"].FirstOrDefault();
            var value = Request.Form[$"Conditions.OrFilters[{index}].Value"].FirstOrDefault();

            if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(op))
            {
                orFilters.Add(new ViewFilterCondition
                {
                    Field = field,
                    Operator = op,
                    Value = value
                });
            }
            index++;
        }

        if (!andFilters.Any() && !orFilters.Any())
            return null;

        return new ViewConditions
        {
            AndFilters = andFilters,
            OrFilters = orFilters
        };
    }

    private List<UpdateTicketView.SortLevelInput>? ParseSortLevelsFromForm()
    {
        var sortLevels = new List<UpdateTicketView.SortLevelInput>();
        var index = 0;

        while (Request.Form.ContainsKey($"SortLevels[{index}].Field"))
        {
            var field = Request.Form[$"SortLevels[{index}].Field"].FirstOrDefault();
            var direction = Request.Form[$"SortLevels[{index}].Direction"].FirstOrDefault() ?? "asc";
            var orderStr = Request.Form[$"SortLevels[{index}].Order"].FirstOrDefault();
            var order = int.TryParse(orderStr, out var o) ? o : index;

            if (!string.IsNullOrEmpty(field))
            {
                sortLevels.Add(new UpdateTicketView.SortLevelInput
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

    private async Task LoadAdvancedViewModelsAsync(TicketViewDto? existingView, CancellationToken cancellationToken)
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
        FilterBuilder.Conditions = existingView?.Conditions;

        // Sort Configurator
        SortConfigurator = new SortConfiguratorViewModel
        {
            SortableFields = FilterAttributes.All
                .Where(a => a.IsSortable)
                .Select(a => new SortFieldModel { Field = a.Field, Label = a.Label })
                .ToList(),
            SortLevels = existingView?.SortLevels?
                .Select(s => new SortLevelModel { Order = s.Order, Field = s.Field, Direction = s.Direction })
                .ToList()
        };

        // Column Selector
        ColumnSelector = new ColumnSelectorViewModel
        {
            AvailableColumns = ColumnRegistry.Columns
                .Select(c => new ColumnModel { Field = c.Field, Label = c.Label })
                .ToList(),
            SelectedColumns = existingView?.VisibleColumns ?? 
                new List<string> { "Id", "Title", "Status", "Priority", "AssigneeName", "CreationTime" }
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
    }

    public class EditViewForm
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; }
    }

    public class TeamSelectItem
    {
        public ShortGuid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
