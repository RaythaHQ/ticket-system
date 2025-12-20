using System;
using CSharpVitamins;

namespace App.Application.Common.Interfaces;

public interface ICurrentUser
{
    ShortGuid? UserId { get; }
    
    /// <summary>
    /// Gets the user's ID as a nullable Guid, properly handling empty GUIDs as null.
    /// Use this instead of UserId?.Guid to avoid FK violations when user is not authenticated.
    /// </summary>
    Guid? UserIdAsGuid { get; }
    string FirstName { get; }
    string LastName { get; }
    string FullName { get; }
    string EmailAddress { get; }
    DateTime? LastModificationTime { get; }
    bool IsAuthenticated { get; }
    string SsoId { get; }
    string AuthenticationScheme { get; }
    string RemoteIpAddress { get; }
    bool IsAdmin { get; }
    public string[] Roles { get; }
    public string[] UserGroups { get; }
    public string[] SystemPermissions { get; }
}
