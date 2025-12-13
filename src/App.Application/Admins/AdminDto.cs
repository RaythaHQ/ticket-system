using System.Linq.Expressions;
using App.Application.Common.Models;
using App.Application.Roles;
using App.Domain.Entities;

namespace App.Application.Admins;

public record AdminDto : BaseFullAuditableEntityDto
{
    public bool IsActive { get; init; }
    public DateTime? LastLoggedInTime { get; init; }

    //base profile
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string FullName
    {
        get { return FirstName + " " + LastName; }
    }
    public bool IsAdmin { get; init; }

    public IEnumerable<RoleDto> Roles { get; init; } = new List<RoleDto>();
    
    // Custom attributes
    public string? CustomAttribute1 { get; init; }
    public string? CustomAttribute2 { get; init; }
    public string? CustomAttribute3 { get; init; }
    public string? CustomAttribute4 { get; init; }
    public string? CustomAttribute5 { get; init; }

    public static Expression<Func<User, AdminDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static AdminDto GetProjection(User entity)
    {
        return new AdminDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            EmailAddress = entity.EmailAddress,
            LastLoggedInTime = entity.LastLoggedInTime,
            IsActive = entity.IsActive,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            Roles = entity.Roles.AsQueryable().Select(RoleDto.GetProjection()),
            IsAdmin = entity.IsAdmin,
            CustomAttribute1 = entity.CustomAttribute1,
            CustomAttribute2 = entity.CustomAttribute2,
            CustomAttribute3 = entity.CustomAttribute3,
            CustomAttribute4 = entity.CustomAttribute4,
            CustomAttribute5 = entity.CustomAttribute5,
        };
    }
}
