using App.Application.Common.Models;
using App.Domain.Entities;

namespace App.Application.Tickets;

/// <summary>
/// Full ticket data transfer object with all details.
/// </summary>
public record TicketDto : BaseNumericAuditableEntityDto
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string Status { get; init; } = null!;
    public string StatusLabel { get; init; } = null!;
    public string Priority { get; init; } = null!;
    public string PriorityLabel { get; init; } = null!;
    public string? Category { get; init; }
    public List<string> Tags { get; init; } = new();

    // Relationships
    public Guid? OwningTeamId { get; init; }
    public string? OwningTeamName { get; init; }
    public Guid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public Guid? CreatedByStaffId { get; init; }
    public string? CreatedByStaffName { get; init; }
    public long? ContactId { get; init; }
    public string? ContactName { get; init; }

    // Timestamps
    public DateTime? ResolvedAt { get; init; }
    public DateTime? ClosedAt { get; init; }

    // SLA
    public Guid? SlaRuleId { get; init; }
    public string? SlaRuleName { get; init; }
    public DateTime? SlaDueAt { get; init; }
    public DateTime? SlaBreachedAt { get; init; }
    public string? SlaStatus { get; init; }
    public string? SlaStatusLabel { get; init; }

    public static TicketDto MapFrom(Ticket ticket)
    {
        return new TicketDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            StatusLabel = ticket.StatusValue.Label,
            Priority = ticket.Priority,
            PriorityLabel = ticket.PriorityValue.Label,
            Category = ticket.Category,
            Tags = ticket.Tags,
            OwningTeamId = ticket.OwningTeamId,
            OwningTeamName = ticket.OwningTeam?.Name,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = ticket.Assignee?.FullName,
            CreatedByStaffId = ticket.CreatedByStaffId,
            CreatedByStaffName = ticket.CreatedByStaff?.FullName,
            ContactId = ticket.ContactId,
            ContactName = ticket.Contact?.Name,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            SlaRuleId = ticket.SlaRuleId,
            SlaRuleName = ticket.SlaRule?.Name,
            SlaDueAt = ticket.SlaDueAt,
            SlaBreachedAt = ticket.SlaBreachedAt,
            SlaStatus = ticket.SlaStatus,
            SlaStatusLabel = ticket.SlaStatusValue?.Label,
            CreationTime = ticket.CreationTime,
            CreatorUserId = ticket.CreatorUserId,
            LastModifierUserId = ticket.LastModifierUserId,
            LastModificationTime = ticket.LastModificationTime
        };
    }
}

