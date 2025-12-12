using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Contacts;

/// <summary>
/// Contact change log entry data transfer object.
/// </summary>
public record ContactChangeLogEntryDto : BaseEntityDto
{
    public long ContactId { get; init; }
    public ShortGuid? ActorStaffId { get; init; }
    public string? ActorStaffName { get; init; }
    public string? FieldChangesJson { get; init; }
    public string? Message { get; init; }
    public DateTime CreationTime { get; init; }

    public static ContactChangeLogEntryDto MapFrom(ContactChangeLogEntry entry)
    {
        return new ContactChangeLogEntryDto
        {
            Id = entry.Id,
            ContactId = entry.ContactId,
            ActorStaffId = entry.ActorStaffId,
            ActorStaffName = entry.ActorStaff?.FullName,
            FieldChangesJson = entry.FieldChangesJson,
            Message = entry.Message,
            CreationTime = entry.CreationTime
        };
    }
}

