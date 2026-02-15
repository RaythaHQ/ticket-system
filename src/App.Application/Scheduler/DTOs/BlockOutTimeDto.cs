using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Scheduler.DTOs;

/// <summary>
/// DTO for block-out time display in calendar views.
/// </summary>
public record BlockOutTimeDto
{
    public ShortGuid Id { get; init; }
    public ShortGuid StaffMemberId { get; init; }
    public string StaffMemberName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public bool IsAllDay { get; init; }
    public string? Reason { get; init; }
    public DateTime CreationTime { get; init; }

    public static BlockOutTimeDto MapFrom(StaffBlockOutTime entity)
    {
        return new BlockOutTimeDto
        {
            Id = entity.Id,
            StaffMemberId = entity.StaffMemberId,
            StaffMemberName = entity.StaffMember?.User != null
                ? entity.StaffMember.User.FirstName + " " + entity.StaffMember.User.LastName
                : string.Empty,
            Title = entity.Title,
            StartTimeUtc = entity.StartTimeUtc,
            EndTimeUtc = entity.EndTimeUtc,
            IsAllDay = entity.IsAllDay,
            Reason = entity.Reason,
            CreationTime = entity.CreationTime,
        };
    }
}
