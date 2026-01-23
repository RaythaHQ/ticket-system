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

    // Impersonation properties
    /// <summary>
    /// Gets whether the current session is impersonating another user.
    /// </summary>
    bool IsImpersonating { get; }

    /// <summary>
    /// Gets the original admin's user ID when impersonating. Null if not impersonating.
    /// </summary>
    ShortGuid? OriginalUserId { get; }

    /// <summary>
    /// Gets the original admin's email address when impersonating. Null if not impersonating.
    /// </summary>
    string? OriginalUserEmail { get; }

    /// <summary>
    /// Gets the original admin's full name when impersonating. Null if not impersonating.
    /// </summary>
    string? OriginalUserFullName { get; }

    /// <summary>
    /// Gets when the impersonation session started. Null if not impersonating.
    /// </summary>
    DateTime? ImpersonationStartedAt { get; }
}
