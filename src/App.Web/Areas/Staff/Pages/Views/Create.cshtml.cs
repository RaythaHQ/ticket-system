using System.ComponentModel.DataAnnotations;
using App.Application.Teams.Queries;
using App.Application.TicketViews.Commands;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Views;

public class Create : BaseStaffPageModel
{
    [BindProperty]
    public CreateViewForm Form { get; set; } = new();

    [BindProperty]
    public List<string> SelectedColumnNames { get; set; } = new();

    public List<TeamSelectItem> AvailableTeams { get; set; } = new();
    public List<ColumnOption> AvailableColumns { get; set; } = new();
    public List<string> AvailableStatuses { get; set; } = new();
    public List<string> AvailablePriorities { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create View";
        ViewData["ActiveMenu"] = "Views";

        await LoadOptionsAsync(cancellationToken);

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

        // Get columns from form directly (model binding for List<string> can be unreliable)
        var formColumns = Request.Form["SelectedColumnNames"];
        var columnList = formColumns.Count > 0 
            ? formColumns.ToList() 
            : SelectedColumnNames;

        // Build filters
        var filters = new List<CreateTicketView.FilterCondition>();
        
        if (!string.IsNullOrEmpty(Form.StatusFilter))
        {
            filters.Add(new CreateTicketView.FilterCondition
            {
                Field = "Status",
                Operator = "equals",
                Value = Form.StatusFilter
            });
        }

        if (!string.IsNullOrEmpty(Form.PriorityFilter))
        {
            filters.Add(new CreateTicketView.FilterCondition
            {
                Field = "Priority",
                Operator = "equals",
                Value = Form.PriorityFilter
            });
        }

        if (Form.TeamIdFilter.HasValue)
        {
            filters.Add(new CreateTicketView.FilterCondition
            {
                Field = "OwningTeamId",
                Operator = "equals",
                Value = Form.TeamIdFilter.Value.ToString()
            });
        }

        if (Form.UnassignedOnly)
        {
            filters.Add(new CreateTicketView.FilterCondition
            {
                Field = "AssigneeId",
                Operator = "is_null",
                Value = "true"
            });
        }

        if (Form.AssignedToMe)
        {
            filters.Add(new CreateTicketView.FilterCondition
            {
                Field = "AssigneeId",
                Operator = "equals",
                Value = userId.Value.ToString()
            });
        }

        // Get selected columns - use the form values, or defaults if none selected
        var selectedColumns = columnList.Any() 
            ? columnList 
            : new List<string> { "Id", "Title", "Status", "Priority", "CreationTime" };

        var response = await Mediator.Send(new CreateTicketView.Command
        {
            Name = Form.Name,
            Description = Form.Description,
            OwnerUserId = userId.Value,
            IsSystemView = false,
            Filters = filters,
            VisibleColumns = selectedColumns,
            SortField = Form.SortField ?? "CreationTime",
            SortDirection = Form.SortDescending ? "desc" : "asc"
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("View created successfully.");
            return RedirectToPage(RouteNames.Views.Index);
        }

        SetErrorMessage(response.GetErrors());
        await LoadOptionsAsync(cancellationToken);
        return Page();
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var teamsResponse = await Mediator.Send(new GetTeams.Query(), cancellationToken);
        AvailableTeams = teamsResponse.Result.Items.Select(t => new TeamSelectItem
        {
            Id = t.Id,
            Name = t.Name
        }).ToList();

        AvailableStatuses = TicketStatus.SupportedTypes.Select(s => s.DeveloperName).ToList();
        AvailablePriorities = TicketPriority.SupportedTypes.Select(p => p.DeveloperName).ToList();

        AvailableColumns = new List<ColumnOption>
        {
            new() { Name = "Id", Label = "ID", Selected = true },
            new() { Name = "Title", Label = "Title", Selected = true },
            new() { Name = "Status", Label = "Status", Selected = true },
            new() { Name = "Priority", Label = "Priority", Selected = true },
            new() { Name = "Category", Label = "Category", Selected = false },
            new() { Name = "AssigneeName", Label = "Assignee", Selected = true },
            new() { Name = "OwningTeamName", Label = "Team", Selected = false },
            new() { Name = "ContactName", Label = "Contact", Selected = false },
            new() { Name = "CreationTime", Label = "Created", Selected = true },
            new() { Name = "SlaDueAt", Label = "SLA Due", Selected = false },
            new() { Name = "SlaStatus", Label = "SLA Status", Selected = false }
        };
    }

    public class CreateViewForm
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Filters
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public ShortGuid? TeamIdFilter { get; set; }
        public bool UnassignedOnly { get; set; }
        public bool AssignedToMe { get; set; }

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

