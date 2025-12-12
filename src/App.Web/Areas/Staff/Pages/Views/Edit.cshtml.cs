using System.ComponentModel.DataAnnotations;
using App.Application.Teams.Queries;
using App.Application.TicketViews.Commands;
using App.Application.TicketViews.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Views;

public class Edit : BaseStaffPageModel
{
    [BindProperty]
    public EditViewForm Form { get; set; } = new();

    public Guid ViewId { get; set; }
    public List<TeamSelectItem> AvailableTeams { get; set; } = new();
    public List<ColumnOption> AvailableColumns { get; set; } = new();
    public List<string> AvailableStatuses { get; set; } = new();
    public List<string> AvailablePriorities { get; set; } = new();

    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit View";
        ViewData["ActiveMenu"] = "Views";

        ViewId = new ShortGuid(id).Guid;

        var response = await Mediator.Send(new GetTicketViewById.Query { Id = ViewId }, cancellationToken);
        if (!response.Success || response.Result == null)
        {
            SetErrorMessage("View not found.");
            return RedirectToPage(RouteNames.Views.Index);
        }

        var view = response.Result;

        // Check ownership
        var userId = CurrentUser.UserId?.Guid;
        if (view.OwnerUserId != userId)
        {
            SetErrorMessage("You can only edit your own views.");
            return RedirectToPage(RouteNames.Views.Index);
        }

        await LoadOptionsAsync(cancellationToken);

        Form = new EditViewForm
        {
            Name = view.Name,
            Description = view.Description,
            SortField = view.SortField,
            SortDescending = view.SortDirection == "desc"
        };

        // Parse existing filters
        foreach (var filter in view.Filters)
        {
            switch (filter.Field?.ToLower())
            {
                case "status":
                    Form.StatusFilter = filter.Value;
                    break;
                case "priority":
                    Form.PriorityFilter = filter.Value;
                    break;
                case "owningteamid":
                    if (Guid.TryParse(filter.Value, out var teamId))
                        Form.TeamIdFilter = teamId;
                    break;
                case "assigneeid":
                    if (filter.Operator == "is_null")
                        Form.UnassignedOnly = true;
                    else if (filter.Value == userId?.ToString())
                        Form.AssignedToMe = true;
                    break;
            }
        }

        // Set selected columns
        foreach (var col in AvailableColumns)
        {
            col.Selected = view.Columns.Contains(col.Name);
        }
        Form.SelectedColumns = AvailableColumns;

        return Page();
    }

    public async Task<IActionResult> OnPost(string id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit View";
        ViewData["ActiveMenu"] = "Views";

        ViewId = new ShortGuid(id).Guid;

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        var userId = CurrentUser.UserId?.Guid;
        if (!userId.HasValue)
            return RedirectToPage(RouteNames.Error.Index);

        // Build filters
        var filters = new List<UpdateTicketView.FilterCondition>();

        if (!string.IsNullOrEmpty(Form.StatusFilter))
        {
            filters.Add(new UpdateTicketView.FilterCondition
            {
                Field = "Status",
                Operator = "equals",
                Value = Form.StatusFilter
            });
        }

        if (!string.IsNullOrEmpty(Form.PriorityFilter))
        {
            filters.Add(new UpdateTicketView.FilterCondition
            {
                Field = "Priority",
                Operator = "equals",
                Value = Form.PriorityFilter
            });
        }

        if (Form.TeamIdFilter.HasValue)
        {
            filters.Add(new UpdateTicketView.FilterCondition
            {
                Field = "OwningTeamId",
                Operator = "equals",
                Value = Form.TeamIdFilter.Value.ToString()
            });
        }

        if (Form.UnassignedOnly)
        {
            filters.Add(new UpdateTicketView.FilterCondition
            {
                Field = "AssigneeId",
                Operator = "is_null",
                Value = "true"
            });
        }

        if (Form.AssignedToMe)
        {
            filters.Add(new UpdateTicketView.FilterCondition
            {
                Field = "AssigneeId",
                Operator = "equals",
                Value = userId.Value.ToString()
            });
        }

        var selectedColumns = Form.SelectedColumns?.Where(c => c.Selected).Select(c => c.Name).ToList()
            ?? new List<string> { "Id", "Title", "Status", "Priority" };

        var response = await Mediator.Send(new UpdateTicketView.Command
        {
            Id = ViewId,
            Name = Form.Name,
            Description = Form.Description,
            Filters = filters,
            Columns = selectedColumns,
            SortField = Form.SortField ?? "CreationTime",
            SortDirection = Form.SortDescending ? "desc" : "asc"
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("View updated successfully.");
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
            Id = t.Id.Guid,
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

    public class EditViewForm
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public Guid? TeamIdFilter { get; set; }
        public bool UnassignedOnly { get; set; }
        public bool AssignedToMe { get; set; }

        public List<ColumnOption>? SelectedColumns { get; set; }

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
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

