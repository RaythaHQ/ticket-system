using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Teams;

/// <summary>
/// Team data transfer object.
/// </summary>
public record TeamDto : BaseEntityDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public bool RoundRobinEnabled { get; init; }
    public int MemberCount { get; init; }
    public int AssignableMemberCount { get; init; }
    public DateTime CreationTime { get; init; }

    public static TeamDto MapFrom(Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            RoundRobinEnabled = team.RoundRobinEnabled,
            MemberCount = team.Memberships?.Count ?? 0,
            AssignableMemberCount = team.Memberships?.Count(m => m.IsAssignable) ?? 0,
            CreationTime = team.CreationTime
        };
    }
}

