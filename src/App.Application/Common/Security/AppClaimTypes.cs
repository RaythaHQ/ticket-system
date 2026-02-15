namespace App.Application.Common.Security;

public static class AppClaimTypes
{
    public const string LastModificationTime = "LastModificationTime";
    public const string IsAdmin = "IsAdmin";
    public const string SsoId = "SsoId";
    public const string SystemPermissions = "SystemPermissions";
    public const string AuthenticationScheme = "AuthenticationScheme";
    public const string UserGroups = "groups"; //https://www.rfc-editor.org/rfc/rfc9068.html

    // Impersonation claims
    public const string IsImpersonating = "IsImpersonating";
    public const string OriginalUserId = "OriginalUserId";
    public const string OriginalUserEmail = "OriginalUserEmail";
    public const string OriginalUserFullName = "OriginalUserFullName";
    public const string ImpersonationStartedAt = "ImpersonationStartedAt";
}
