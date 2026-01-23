using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.Teams.Queries;
using App.Application.TicketViews;
using App.Application.TicketViews.Commands;
using App.Application.Users.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Views;

public class Create : BaseStaffPageModel
{
    private readonly ITicketConfigService _configService;

    public Create(ITicketConfigService configService)
    {
        _configService = configService;
    }

    [BindProperty]
    public CreateViewForm Form { get; set; } = new();

    [BindProperty]
    public ViewConditions? Conditions { get; set; }

    [BindProperty]
    public List<SortLevelInput> SortLevels { get; set; } = new();

    [BindProperty]
    public List<string> VisibleColumns { get; set; } = new();

    // View model data for partials
    public FilterBuilderViewModel FilterBuilderModel { get; set; } = null!;
    public SortConfiguratorViewModel SortConfiguratorModel { get; set; } = null!;
    public ColumnSelectorViewModel ColumnSelectorModel { get; set; } = null!;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create View";
        ViewData["ActiveMenu"] = "Views";

        await LoadOptionsAsync(cancellationToken);

        // Set default columns
        VisibleColumns = new List<string>
        {
            "Id",
            "Title",
            "Status",
            "Priority",
            "CreationTime",
            "AssigneeName",
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create View";
        ViewData["ActiveMenu"] = "Views";

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        var userId = CurrentUser.UserId?.Guid;
        if (!userId.HasValue)
            return RedirectToPage(RouteNames.Error.Index);

        // Get visible columns from form
        var formColumns = Request.Form["VisibleColumns"];
        var columnList = formColumns.Count > 0 ? formColumns.ToList() : VisibleColumns;

        // Validate at least one column is selected
        if (!columnList.Any())
        {
            SetErrorMessage("You must select at least one column.");
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        // Parse sort levels from form
        var sortLevelInputs = ParseSortLevelsFromForm();

        var response = await Mediator.Send(
            new CreateTicketView.Command
            {
                Name = Form.Name,
                Description = Form.Description,
                OwnerUserId = userId.Value,
                IsSystemView = false,
                Conditions = Conditions,
                SortLevels = sortLevelInputs,
                VisibleColumns = columnList,
            },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage("View created successfully.");
            return RedirectToPage(RouteNames.Views.Index);
        }

        SetErrorMessage(response.GetErrors());
        await LoadOptionsAsync(cancellationToken);
        return Page();
    }

    private List<CreateTicketView.SortLevelInput> ParseSortLevelsFromForm()
    {
        var sortLevels = new List<CreateTicketView.SortLevelInput>();
        var i = 0;

        while (Request.Form.ContainsKey($"SortLevels[{i}].Field"))
        {
            var field = Request.Form[$"SortLevels[{i}].Field"].ToString();
            var direction = Request.Form[$"SortLevels[{i}].Direction"].ToString();
            var orderStr = Request.Form[$"SortLevels[{i}].Order"].ToString();

            if (!string.IsNullOrEmpty(field))
            {
                sortLevels.Add(
                    new CreateTicketView.SortLevelInput
                    {
                        Order = int.TryParse(orderStr, out var order) ? order : i,
                        Field = field,
                        Direction = direction ?? "asc",
                    }
                );
            }
            i++;
        }

        // Default sort if none provided
        if (!sortLevels.Any())
        {
            sortLevels.Add(
                new CreateTicketView.SortLevelInput
                {
                    Order = 0,
                    Field = "CreationTime",
                    Direction = "desc",
                }
            );
        }

        return sortLevels;
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        // Load filter builder data
        FilterBuilderModel = FilterBuilderViewModel.CreateWithDefaults();

        // Load users for assignee filter
        var usersResponse = await Mediator.Send(
            new GetUsers.Query { PageSize = 1000, OrderBy = "FirstName ASC, LastName ASC" },
            cancellationToken
        );
        FilterBuilderModel.Users = usersResponse
            .Result.Items.Select(u => new UserOption
            {
                Id = u.Id.ToString(),
                Name = u.FullName,
                IsDeactivated = !u.IsActive,
            })
            .ToList();

        // Load teams
        var teamsResponse = await Mediator.Send(new GetTeams.Query(), cancellationToken);
        FilterBuilderModel.Teams = teamsResponse
            .Result.Items.Select(t => new TeamOption { Id = t.Id.ToString(), Name = t.Name })
            .ToList();

        // Load statuses and priorities dynamically from config service
        var statuses = await _configService.GetAllStatusesAsync(
            includeInactive: true,
            cancellationToken
        );
        FilterBuilderModel.Statuses = statuses
            .Select(s => new SelectOption
            {
                Value = s.DeveloperName,
                Label = s.IsActive ? s.Label : $"{s.Label} (inactive)",
            })
            .ToList();

        var priorities = await _configService.GetAllPrioritiesAsync(
            includeInactive: true,
            cancellationToken
        );
        FilterBuilderModel.Priorities = priorities
            .OrderBy(p => p.SortOrder)
            .Select(p => new SelectOption
            {
                Value = p.DeveloperName,
                Label = p.IsActive ? p.Label : $"{p.Label} (inactive)",
            })
            .ToList();

        // Load languages from ValueObject
        FilterBuilderModel.Languages = Domain
            .ValueObjects.TicketLanguage.SupportedTypes.OrderBy(l => l.SortOrder)
            .Select(l => new SelectOption { Value = l.DeveloperName, Label = l.Label })
            .ToList();

        // Load sort configurator data
        SortConfiguratorModel = SortConfiguratorViewModel.CreateWithDefaults();
        if (!SortLevels.Any())
        {
            SortConfiguratorModel.SortLevels = new List<SortLevelModel>
            {
                new()
                {
                    Order = 0,
                    Field = "CreationTime",
                    Direction = "desc",
                },
            };
        }
        else
        {
            SortConfiguratorModel.SortLevels = SortLevels
                .Select(s => new SortLevelModel
                {
                    Order = s.Order,
                    Field = s.Field,
                    Direction = s.Direction,
                })
                .ToList();
        }

        // Load column selector data
        ColumnSelectorModel = ColumnSelectorViewModel.CreateWithDefaults();
        ColumnSelectorModel.SelectedColumns = VisibleColumns;
    }

    public class CreateViewForm
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class SortLevelInput
    {
        public int Order { get; set; }
        public string Field { get; set; } = null!;
        public string Direction { get; set; } = "asc";
    }
}
