using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Tickets;

/// <summary>
/// Ticket comment data transfer object.
/// </summary>
public record TicketCommentDto : BaseEntityDto
{
    public long TicketId { get; init; }
    public ShortGuid AuthorStaffId { get; init; }
    public string AuthorStaffName { get; init; } = null!;
    public string Body { get; init; } = null!;
    public DateTime CreationTime { get; init; }

    public static TicketCommentDto MapFrom(TicketComment comment)
    {
        return new TicketCommentDto
        {
            Id = comment.Id,
            TicketId = comment.TicketId,
            AuthorStaffId = comment.AuthorStaffId,
            AuthorStaffName = comment.AuthorStaff?.FullName ?? "Unknown",
            Body = comment.Body,
            CreationTime = comment.CreationTime
        };
    }
}

