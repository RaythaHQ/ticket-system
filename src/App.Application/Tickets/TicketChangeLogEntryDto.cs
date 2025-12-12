using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Tickets;

/// <summary>
/// Ticket change log entry data transfer object.
/// </summary>
public record TicketChangeLogEntryDto : BaseEntityDto
{
    public long TicketId { get; init; }
    public ShortGuid? ActorStaffId { get; init; }
    public string? ActorStaffName { get; init; }
    public string? FieldChangesJson { get; init; }
    public string? Message { get; init; }
    public DateTime CreationTime { get; init; }

    public static TicketChangeLogEntryDto MapFrom(TicketChangeLogEntry entry)
    {
        return new TicketChangeLogEntryDto
        {
            Id = entry.Id,
            TicketId = entry.TicketId,
            ActorStaffId = entry.ActorStaffId,
            ActorStaffName = entry.ActorStaff?.FullName,
            FieldChangesJson = entry.FieldChangesJson,
            Message = entry.Message,
            CreationTime = entry.CreationTime
        };
    }
}

