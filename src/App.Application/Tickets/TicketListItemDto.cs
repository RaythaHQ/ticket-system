using App.Application.Common.Models;
using App.Domain.Entities;

namespace App.Application.Tickets;

/// <summary>
/// Lightweight ticket DTO for list views.
/// </summary>
public record TicketListItemDto : BaseNumericEntityDto
{
    public string Title { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string StatusLabel { get; init; } = null!;
    public string Priority { get; init; } = null!;
    public string PriorityLabel { get; init; } = null!;
    public string? Category { get; init; }
    public Guid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public Guid? OwningTeamId { get; init; }
    public string? OwningTeamName { get; init; }
    public string? ContactName { get; init; }
    public long? ContactId { get; init; }
    public int CommentCount { get; init; }
    public DateTime? SlaDueAt { get; init; }
    public string? SlaStatus { get; init; }
    public string? SlaStatusLabel { get; init; }
    public DateTime CreationTime { get; init; }

    public static TicketListItemDto MapFrom(Ticket ticket)
    {
        return new TicketListItemDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Status = ticket.Status,
            StatusLabel = ticket.StatusValue.Label,
            Priority = ticket.Priority,
            PriorityLabel = ticket.PriorityValue.Label,
            Category = ticket.Category,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = ticket.Assignee?.FullName,
            OwningTeamId = ticket.OwningTeamId,
            OwningTeamName = ticket.OwningTeam?.Name,
            ContactName = ticket.Contact?.Name,
            ContactId = ticket.ContactId,
            CommentCount = ticket.Comments?.Count ?? 0,
            SlaDueAt = ticket.SlaDueAt,
            SlaStatus = ticket.SlaStatus,
            SlaStatusLabel = ticket.SlaStatusValue?.Label,
            CreationTime = ticket.CreationTime
        };
    }
}

