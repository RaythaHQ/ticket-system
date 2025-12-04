using System.Linq.Expressions;
using CSharpVitamins;
using App.Application.Common.Models;
using App.Domain.Entities;

namespace App.Application.Roles;

public record RoleDto : BaseAuditableEntityDto
{
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public IEnumerable<string> SystemPermissions { get; init; } = null!;

    public static Expression<Func<Role, RoleDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static RoleDto GetProjection(Role entity)
    {
        return new RoleDto
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            SystemPermissions = BuiltInSystemPermission
                .From(entity.SystemPermissions)
                .Select(p => p.DeveloperName),
        };
    }
}
