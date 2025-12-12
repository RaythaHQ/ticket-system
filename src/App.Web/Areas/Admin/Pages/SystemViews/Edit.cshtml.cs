using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Application.Common.Interfaces;
using App.Application.TicketViews;
using App.Application.TicketViews.Queries;
using App.Application.TicketViews.Commands;
using App.Application.Teams.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared.Models;
using CSharpVitamins;

namespace App.Web.Areas.Admin.Pages.SystemViews;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_VIEWS_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    public ShortGuid ViewId { get; set; }

    [BindProperty]
    public EditViewForm Form { get; set; } = new();

    [BindProperty]
    public List<string> SelectedColumnNames { get; set; } = new();

    public List<TeamSelectItem> AvailableTeams { get; set; } = new();
    public List<ColumnOption> AvailableColumns { get; set; } = new();
    public List<SelectListItem> AvailableStatuses { get; set; } = new();
    public List<SelectListItem> AvailablePriorities { get; set; } = new();

    public async Task<IActionResult> OnGet(ShortGuid id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit System View";
        ViewData["ActiveMenu"] = "SystemViews";
        ViewData["ExpandTicketingMenu"] = true;

        ViewId = id;

        var response = await Mediator.Send(new GetTicketViewById.Query { Id = id }, cancellationToken);
        if (!response.Success || response.Result == null)
        {
            SetErrorMessage("System view not found.");
            return RedirectToPage("./Index");
        }

        var view = response.Result;
        if (!view.IsSystem)
        {
            SetErrorMessage("This is not a system view.");
            return RedirectToPage("./Index");
        }

        Form = new EditViewForm
        {
            Name = view.Name,
            Description = view.Description,
            IsDefault = view.IsDefault,
            SortField = view.SortPrimaryField ?? "CreationTime",
            SortDescending = view.SortPrimaryDirection?.ToLower() == "desc"
        };

        // Parse existing filters
        if (view.Conditions?.Filters != null)
        {
            foreach (var filter in view.Conditions.Filters)
            {
                switch (filter.Field)
                {
                    case "Status":
                        Form.StatusFilter = filter.Value;
                        break;
                    case "Priority":
                        Form.PriorityFilter = filter.Value;
                        break;
                    case "OwningTeamId":
                        if (Guid.TryParse(filter.Value, out var teamId))
                            Form.TeamIdFilter = teamId;
                        break;
                    case "AssigneeId" when filter.Operator == "is_null":
                        Form.UnassignedOnly = true;
                        break;
                }
            }
        }

        // Store selected columns
        SelectedColumnNames = view.VisibleColumns ?? new List<string>();

        await LoadOptionsAsync(cancellationToken);

        // Mark selected columns
        foreach (var col in AvailableColumns)
        {
            col.Selected = SelectedColumnNames.Contains(col.Name);
        }

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
            return Page();
        }

        // Get columns from form directly
        var formColumns = Request.Form["SelectedColumnNames"];
        var columnList = formColumns.Count > 0 
            ? formColumns.ToList() 
            : SelectedColumnNames;

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

        // Get selected columns
        var selectedColumns = columnList.Any() 
            ? columnList 
            : new List<string> { "Id", "Title", "Status", "Priority", "CreationTime" };

        var response = await Mediator.Send(new UpdateTicketView.Command
        {
            Id = id,
            Name = Form.Name,
            Description = Form.Description,
            IsDefault = Form.IsDefault,
            Filters = filters,
            VisibleColumns = selectedColumns,
            SortField = Form.SortField ?? "CreationTime",
            SortDirection = Form.SortDescending ? "desc" : "asc"
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("System view updated successfully.");
            return RedirectToPage("./Index");
        }

        SetErrorMessage(response.GetErrors());
        await LoadOptionsAsync(cancellationToken);
        return Page();
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        // Load teams
        var teamsResponse = await Mediator.Send(new GetTeams.Query(), cancellationToken);
        AvailableTeams = teamsResponse.Result.Items.Select(t => new TeamSelectItem
        {
            Id = t.Id.Guid,
            Name = t.Name
        }).ToList();

        // Status options
        AvailableStatuses = TicketStatus.SupportedTypes.Select(s => new SelectListItem
        {
            Value = s.DeveloperName,
            Text = s.Label
        }).ToList();

        // Priority options
        AvailablePriorities = TicketPriority.SupportedTypes.Select(p => new SelectListItem
        {
            Value = p.DeveloperName,
            Text = p.Label
        }).ToList();

        // Available columns
        AvailableColumns = new List<ColumnOption>
        {
            new() { Name = "Id", Label = "Ticket ID", Selected = false },
            new() { Name = "Title", Label = "Title", Selected = false },
            new() { Name = "Status", Label = "Status", Selected = false },
            new() { Name = "Priority", Label = "Priority", Selected = false },
            new() { Name = "Category", Label = "Category", Selected = false },
            new() { Name = "OwningTeamName", Label = "Team", Selected = false },
            new() { Name = "AssigneeName", Label = "Assignee", Selected = false },
            new() { Name = "ContactName", Label = "Contact", Selected = false },
            new() { Name = "SlaStatus", Label = "SLA Status", Selected = false },
            new() { Name = "CreationTime", Label = "Created", Selected = false },
            new() { Name = "LastModificationTime", Label = "Last Updated", Selected = false }
        };
    }

    public class EditViewForm
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
        public Guid? TeamIdFilter { get; set; }
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
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

