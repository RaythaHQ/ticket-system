using App.Application.Common.Models;
using App.Application.Tickets.Queries;
using App.Application.TicketTasks;
using App.Application.TicketTasks.Commands;
using App.Application.TicketTasks.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Tasks;

public class Index : BaseStaffPageModel
{
    public ListViewModel<TaskListItemDto> ListView { get; set; } = null!;
    public string CurrentView { get; set; } = "my-tasks";
    public string CurrentSortBy { get; set; } = "default";
    public bool ExcludeClosed { get; set; } = true;
    public List<AssigneeFilterItem> AvailableAssignees { get; set; } = new();
    public bool CanExport { get; set; }

    /// <summary>
    /// Full list of assignee options for inline task editing (Team/Individual format).
    /// </summary>
    public List<AssigneeFilterItem> AllAssigneeOptions { get; set; } = new();

    public async Task<IActionResult> OnGet(
        string? view,
        string? search,
        string sortBy = "default",
        int pageNumber = 1,
        int pageSize = 25,
        string? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Tasks";
        ViewData["ActiveMenu"] = "Tasks";

        CurrentView = view ?? "my-tasks";
        CurrentSortBy = sortBy;

        // Status filter: "open" (default) or "all"
        ExcludeClosed = statusFilter != "all";

        ViewData["ActiveTaskView"] = CurrentView;

        var taskView = CurrentView switch
        {
            "unassigned" => GetTasks.TaskView.Unassigned,
            "my-tasks" => GetTasks.TaskView.MyTasks,
            "created-by-me" => GetTasks.TaskView.CreatedByMe,
            "team-tasks" => GetTasks.TaskView.TeamTasks,
            "overdue" => GetTasks.TaskView.Overdue,
            "all" => GetTasks.TaskView.AllTasks,
            _ => GetTasks.TaskView.MyTasks,
        };

        // Map sort pills to query sort params
        var (sort, sortDir) = sortBy switch
        {
            "newest" => ("newest", "desc"),
            "oldest" => ("oldest", "asc"),
            "due-date" => ("dueat", "asc"),
            "assignee" => ("assignee", "asc"),
            "ticket" => ("ticket", "desc"),
            "title" => ("title", "asc"),
            _ => (null as string, null as string),
        };

        var query = new GetTasks.Query
        {
            View = taskView,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Sort = sort,
            SortDir = sortDir,
            ExcludeClosed = ExcludeClosed,
        };

        var response = await Mediator.Send(query, cancellationToken);

        // Load assignee filter options (reuse from tickets)
        var assigneeOptionsResponse = await Mediator.Send(
            new GetAssigneeFilterOptions.Query
            {
                BuiltInView = CurrentView,
                CurrentUserId = CurrentUser.UserId?.Guid,
            },
            cancellationToken
        );
        AvailableAssignees = assigneeOptionsResponse.Result
            .Select(a => new AssigneeFilterItem { Value = a.Value, DisplayText = a.DisplayText })
            .ToList();

        // Load ALL assignees for inline editing (unfiltered)
        var allAssigneeResponse = await Mediator.Send(
            new GetAssigneeSelectOptions.Query
            {
                CanManageTickets = true,
                CurrentUserId = CurrentUser.UserId?.Guid,
            },
            cancellationToken
        );
        AllAssigneeOptions = allAssigneeResponse.Result
            .Select(a => new AssigneeFilterItem { Value = a.Value, DisplayText = a.DisplayText })
            .ToList();

        ListView = new ListViewModel<TaskListItemDto>(response.Result.Items, response.Result.TotalCount)
        {
            Search = search ?? "",
            PageNumber = pageNumber,
            PageSize = pageSize,
            BuiltInView = CurrentView,
            SortBy = sortBy,
            PageName = RouteNames.Tasks.Index,
        };

        CanExport = CurrentUser.SystemPermissions.Contains(
            Domain.Entities.BuiltInSystemPermission.IMPORT_EXPORT_TICKETS_PERMISSION);

        return Page();
    }

    public async Task<IActionResult> OnPostCompleteTask([FromBody] TaskActionRequest request, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new CompleteTicketTask.Command { TaskId = request.TaskId },
            cancellationToken);
        return new JsonResult(new { success = response.Success, result = response.Result, error = response.Error });
    }

    public async Task<IActionResult> OnPostReopenTask([FromBody] TaskActionRequest request, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new ReopenTicketTask.Command { TaskId = request.TaskId },
            cancellationToken);
        return new JsonResult(new { success = response.Success, result = response.Result, error = response.Error });
    }

    public async Task<IActionResult> OnPostUpdateTask([FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTicketTask.Command
        {
            TaskId = request.TaskId,
            AssigneeId = request.AssigneeId,
            OwningTeamId = request.OwningTeamId,
            DueAt = request.DueAt,
            ClearAssignee = request.ClearAssignee,
            ClearDueAt = request.ClearDueAt,
        };
        var response = await Mediator.Send(command, cancellationToken);
        return new JsonResult(new { success = response.Success, result = response.Result, error = response.Error });
    }

    public async Task<IActionResult> OnPostExportAsync(
        string? view,
        string? search,
        string? sortBy,
        string? statusFilter,
        CancellationToken cancellationToken)
    {
        var (sort, sortDir) = (sortBy ?? "default") switch
        {
            "newest" => ("newest", "desc"),
            "oldest" => ("oldest", "asc"),
            "due-date" => ("dueat", "asc"),
            "assignee" => ("assignee", "asc"),
            "ticket" => ("ticket", "desc"),
            "title" => ("title", "asc"),
            _ => (null as string, null as string),
        };

        var snapshotPayload = new Application.Exports.Models.TaskExportSnapshotPayload
        {
            BuiltInView = view ?? "my-tasks",
            RequestingUserId = CurrentUser.UserId?.Guid,
            SearchTerm = search,
            SortField = sort,
            SortDirection = sortDir,
            StatusFilter = statusFilter ?? "open",
        };

        var command = new Application.Exports.Commands.CreateTaskExportJob.Command
        {
            SnapshotPayload = snapshotPayload,
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            return RedirectToPage(RouteNames.Exports.Status, new { id = response.Result });
        }
        else
        {
            SetErrorMessage(
                !string.IsNullOrEmpty(response.Error) ? response.Error : "Failed to start export.");
            return RedirectToPage(RouteNames.Tasks.Index, new { view });
        }
    }

    // Request DTOs
    public record TaskActionRequest { public ShortGuid TaskId { get; init; } }
    public record UpdateTaskRequest
    {
        public ShortGuid TaskId { get; init; }
        public ShortGuid? AssigneeId { get; init; }
        public ShortGuid? OwningTeamId { get; init; }
        public DateTime? DueAt { get; init; }
        public bool ClearAssignee { get; init; }
        public bool ClearDueAt { get; init; }
    }
    public record AssigneeFilterItem
    {
        public string Value { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
    }
}
