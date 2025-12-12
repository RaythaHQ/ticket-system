using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Teams;

/// <summary>
/// Team membership data transfer object.
/// </summary>
public record TeamMembershipDto : BaseEntityDto
{
    public ShortGuid TeamId { get; init; }
    public string TeamName { get; init; } = null!;
    public ShortGuid StaffAdminId { get; init; }
    public string StaffAdminName { get; init; } = null!;
    public string StaffAdminEmail { get; init; } = null!;
    public bool IsAssignable { get; init; }
    public DateTime? LastAssignedAt { get; init; }
    public DateTime CreationTime { get; init; }

    public static TeamMembershipDto MapFrom(TeamMembership membership)
    {
        return new TeamMembershipDto
        {
            Id = membership.Id,
            TeamId = membership.TeamId,
            TeamName = membership.Team?.Name ?? "",
            StaffAdminId = membership.StaffAdminId,
            StaffAdminName = membership.StaffAdmin?.FullName ?? "",
            StaffAdminEmail = membership.StaffAdmin?.EmailAddress ?? "",
            IsAssignable = membership.IsAssignable,
            LastAssignedAt = membership.LastAssignedAt,
            CreationTime = membership.CreationTime
        };
    }
}

