using System.Linq.Expressions;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Tickets;

/// <summary>
/// Lightweight ticket DTO for list views.
/// </summary>
public record TicketListItemDto : BaseNumericEntityDto
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string Status { get; init; } = null!;
    public string StatusLabel { get; init; } = null!;
    public string Priority { get; init; } = null!;
    public string PriorityLabel { get; init; } = null!;
    public string Language { get; init; } = null!;
    public string LanguageLabel { get; init; } = null!;
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
    public ShortGuid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public ShortGuid? OwningTeamId { get; init; }
    public string? OwningTeamName { get; init; }
    public string? ContactName { get; init; }
    public long? ContactId { get; init; }
    public int CommentCount { get; init; }
    public DateTime? SlaDueAt { get; init; }
    public string? SlaStatus { get; init; }
    public string? SlaStatusLabel { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? CreatedByStaffName { get; init; }

    /// <summary>
    /// Gets an Expression-based projection for EF Core translation.
    /// This allows CommentCount to be calculated as a SQL subquery instead of loading all comments.
    /// </summary>
    public static Expression<Func<Ticket, TicketListItemDto>> GetProjection()
    {
        return ticket => new TicketListItemDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            StatusLabel = Domain.ValueObjects.TicketStatus.From(ticket.Status).Label,
            Priority = ticket.Priority,
            PriorityLabel = Domain.ValueObjects.TicketPriority.From(ticket.Priority).Label,
            Language = ticket.Language,
            LanguageLabel = Domain.ValueObjects.TicketLanguage.From(ticket.Language).Label,
            Category = ticket.Category,
            Tags = ticket.Tags,
            AssigneeId = ticket.AssigneeId,
            AssigneeName =
                ticket.Assignee != null
                    ? ticket.Assignee.FirstName + " " + ticket.Assignee.LastName
                    : null,
            OwningTeamId = ticket.OwningTeamId,
            OwningTeamName = ticket.OwningTeam != null ? ticket.OwningTeam.Name : null,
            ContactName =
                ticket.Contact != null
                    ? ticket.Contact.FirstName
                        + (ticket.Contact.LastName != null ? " " + ticket.Contact.LastName : "")
                    : null,
            ContactId = ticket.ContactId,
            CommentCount = ticket.Comments.Count, // Translated to SQL COUNT subquery
            SlaDueAt = ticket.SlaDueAt,
            SlaStatus = ticket.SlaStatus,
            SlaStatusLabel =
                ticket.SlaStatus != null
                    ? Domain.ValueObjects.SlaStatus.From(ticket.SlaStatus).Label
                    : null,
            CreationTime = ticket.CreationTime,
            LastModificationTime = ticket.LastModificationTime,
            ClosedAt = ticket.ClosedAt,
            CreatedByStaffName =
                ticket.CreatedByStaff != null
                    ? ticket.CreatedByStaff.FirstName + " " + ticket.CreatedByStaff.LastName
                    : null,
        };
    }

    /// <summary>
    /// Maps from an entity when already loaded (for use outside of EF Core queries).
    /// </summary>
    public static TicketListItemDto MapFrom(Ticket ticket)
    {
        return new TicketListItemDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            StatusLabel = ticket.StatusValue.Label,
            Priority = ticket.Priority,
            PriorityLabel = ticket.PriorityValue.Label,
            Language = ticket.Language,
            LanguageLabel = ticket.LanguageValue.Label,
            Category = ticket.Category,
            Tags = ticket.Tags,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = ticket.Assignee?.FullName,
            OwningTeamId = ticket.OwningTeamId,
            OwningTeamName = ticket.OwningTeam?.Name,
            ContactName = ticket.Contact?.FullName,
            ContactId = ticket.ContactId,
            CommentCount = ticket.Comments?.Count ?? 0,
            SlaDueAt = ticket.SlaDueAt,
            SlaStatus = ticket.SlaStatus,
            SlaStatusLabel = ticket.SlaStatusValue?.Label,
            CreationTime = ticket.CreationTime,
            LastModificationTime = ticket.LastModificationTime,
            ClosedAt = ticket.ClosedAt,
            CreatedByStaffName = ticket.CreatedByStaff?.FullName,
        };
    }
}
