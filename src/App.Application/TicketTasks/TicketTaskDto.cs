using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;

namespace App.Application.TicketTasks;

public record TicketTaskDto
{
    public ShortGuid Id { get; init; }
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsBlocked { get; init; }
    public ShortGuid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public ShortGuid? OwningTeamId { get; init; }
    public string? OwningTeamName { get; init; }
    public DateTime? DueAt { get; init; }
    public bool IsOverdue { get; init; }
    public ShortGuid? DependsOnTaskId { get; init; }
    public string? DependsOnTaskTitle { get; init; }
    public int SortOrder { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? ClosedByStaffName { get; init; }
    public ShortGuid? CreatedByStaffId { get; init; }
    public string? CreatedByStaffName { get; init; }
    public DateTime CreatedAt { get; init; }

    public static TicketTaskDto MapFrom(TicketTask task)
    {
        var isBlocked = task.DependsOnTaskId != null
            && task.DependsOnTask?.Status != TicketTaskStatus.CLOSED;

        return new TicketTaskDto
        {
            Id = new ShortGuid(task.Id),
            TicketId = task.TicketId,
            Title = task.Title,
            Status = task.Status,
            IsBlocked = isBlocked,
            AssigneeId = task.AssigneeId.HasValue ? new ShortGuid(task.AssigneeId.Value) : null,
            AssigneeName = task.Assignee?.FirstName != null
                ? $"{task.Assignee.FirstName} {task.Assignee.LastName}".Trim()
                : null,
            OwningTeamId = task.OwningTeamId.HasValue ? new ShortGuid(task.OwningTeamId.Value) : null,
            OwningTeamName = task.OwningTeam?.Name,
            DueAt = task.DueAt,
            IsOverdue = task.Status == TicketTaskStatus.OPEN
                && task.DueAt.HasValue
                && task.DueAt.Value < DateTime.UtcNow,
            DependsOnTaskId = task.DependsOnTaskId.HasValue
                ? new ShortGuid(task.DependsOnTaskId.Value)
                : null,
            DependsOnTaskTitle = task.DependsOnTask?.Title,
            SortOrder = task.SortOrder,
            ClosedAt = task.ClosedAt,
            ClosedByStaffName = task.ClosedByStaff != null
                ? $"{task.ClosedByStaff.FirstName} {task.ClosedByStaff.LastName}".Trim()
                : null,
            CreatedByStaffId = task.CreatedByStaffId.HasValue
                ? new ShortGuid(task.CreatedByStaffId.Value)
                : null,
            CreatedByStaffName = task.CreatedByStaff != null
                ? $"{task.CreatedByStaff.FirstName} {task.CreatedByStaff.LastName}".Trim()
                : null,
            CreatedAt = task.CreationTime,
        };
    }
}

/// <summary>
/// Extended DTO for the Staff Tasks page — includes parent ticket context.
/// </summary>
public record TaskListItemDto : TicketTaskDto
{
    public string TicketTitle { get; init; } = string.Empty;
    public string? TicketAssigneeName { get; init; }
    public string? TicketPriority { get; init; }
    public string? TicketPriorityLabel { get; init; }

    public static TaskListItemDto MapFromWithTicket(TicketTask task)
    {
        var baseDto = MapFrom(task);
        return new TaskListItemDto
        {
            Id = baseDto.Id,
            TicketId = baseDto.TicketId,
            Title = baseDto.Title,
            Status = baseDto.Status,
            IsBlocked = baseDto.IsBlocked,
            AssigneeId = baseDto.AssigneeId,
            AssigneeName = baseDto.AssigneeName,
            OwningTeamId = baseDto.OwningTeamId,
            OwningTeamName = baseDto.OwningTeamName,
            DueAt = baseDto.DueAt,
            IsOverdue = baseDto.IsOverdue,
            DependsOnTaskId = baseDto.DependsOnTaskId,
            DependsOnTaskTitle = baseDto.DependsOnTaskTitle,
            SortOrder = baseDto.SortOrder,
            ClosedAt = baseDto.ClosedAt,
            ClosedByStaffName = baseDto.ClosedByStaffName,
            CreatedByStaffId = baseDto.CreatedByStaffId,
            CreatedByStaffName = baseDto.CreatedByStaffName,
            CreatedAt = baseDto.CreatedAt,
            TicketTitle = task.Ticket?.Title ?? string.Empty,
            TicketAssigneeName = task.Ticket?.Assignee != null
                ? $"{task.Ticket.Assignee.FirstName} {task.Ticket.Assignee.LastName}".Trim()
                : null,
            TicketPriority = task.Ticket?.Priority,
            TicketPriorityLabel = task.Ticket?.PriorityValue?.Label,
        };
    }
}
