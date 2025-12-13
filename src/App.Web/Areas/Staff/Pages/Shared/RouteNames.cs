namespace App.Web.Areas.Staff.Pages.Shared;

/// <summary>
/// Centralized constants for Razor Page routes throughout the staff area.
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
    /// Route constants for ticket management pages.
    /// </summary>
    public static class Tickets
    {
        public const string Index = "/Tickets/Index";
        public const string Create = "/Tickets/Create";
        public const string Edit = "/Tickets/Edit";
        public const string Details = "/Tickets/Details";
    }

    /// <summary>
    /// Route constants for contact management pages.
    /// </summary>
    public static class Contacts
    {
        public const string Index = "/Contacts/Index";
        public const string Create = "/Contacts/Create";
        public const string Edit = "/Contacts/Edit";
        public const string Details = "/Contacts/Details";
    }

    /// <summary>
    /// Route constants for ticket view management pages.
    /// </summary>
    public static class Views
    {
        public const string Index = "/Views/Index";
        public const string Create = "/Views/Create";
        public const string Edit = "/Views/Edit";
    }

    /// <summary>
    /// Route constants for export pages.
    /// </summary>
    public static class Exports
    {
        public const string Status = "/Exports/Status";
    }

    /// <summary>
    /// Route constants for error pages.
    /// </summary>
    public static class Error
    {
        public const string Index = "/Error";
    }
}

