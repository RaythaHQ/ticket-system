namespace App.Web.Areas.Public.Pages.Shared;

/// <summary>
/// Centralized constants for Razor Page routes throughout the public area.
/// Eliminates magic strings and provides compile-time safety for page navigation.
/// </summary>
public static class RouteNames
{
    /// <summary>
    /// Route constants for dashboard pages.
    /// </summary>
    public static class Dashboard
    {
        public const string Index = "/Dashboard/Index";
    }

    /// <summary>
    /// Route constants for user profile pages.
    /// </summary>
    public static class Profile
    {
        public const string Index = "/Profile/Index";
        public const string ChangePassword = "/Profile/ChangePassword";
    }

    /// <summary>
    /// Route constants for login and authentication pages.
    /// </summary>
    public static class Login
    {
        public const string LoginRedirect = "/Login/LoginRedirect";
        public const string LoginWithEmailAndPassword = "/Login/LoginWithEmailAndPassword";
        public const string LoginWithMagicLink = "/Login/LoginWithMagicLink";
        public const string LoginWithMagicLinkSent = "/Login/LoginWithMagicLinkSent";
        public const string LoginWithMagicLinkComplete = "/Login/LoginWithMagicLinkComplete";
        public const string LoginWithSso = "/Login/LoginWithSso";
        public const string ForgotPassword = "/Login/ForgotPassword";
        public const string ForgotPasswordSent = "/Login/ForgotPasswordSent";
        public const string ForgotPasswordComplete = "/Login/ForgotPasswordComplete";
        public const string Logout = "/Login/Logout";
    }
}

