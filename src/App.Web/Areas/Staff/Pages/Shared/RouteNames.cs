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
        public const string ChangeId = "/Contacts/ChangeId";
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
    /// Route constants for activity log pages.
    /// </summary>
    public static class ActivityLog
    {
        public const string Index = "/ActivityLog/Index";
    }

    /// <summary>
    /// Route constants for error pages.
    /// </summary>
    public static class Error
    {
        public const string Index = "/Error";
    }

    /// <summary>
    /// Route constants for notification pages.
    /// </summary>
    public static class Notifications
    {
        public const string Index = "/Notifications/Index";
    }

    /// <summary>
    /// Route constants for task management pages.
    /// </summary>
    public static class Tasks
    {
        public const string Index = "/Tasks/Index";
        public const string Reports = "/Tasks/Reports";
    }

    /// <summary>
    /// Route constants for wiki pages.
    /// </summary>
    public static class Wiki
    {
        public const string Index = "/Wiki/Index";
        public const string Article = "/Wiki/Article";
        public const string Create = "/Wiki/Create";
        public const string Edit = "/Wiki/Edit";
    }

    /// <summary>
    /// Route constants for scheduler pages.
    /// </summary>
    public static class Scheduler
    {
        public const string Index = "/Scheduler/Index";
        public const string StaffSchedule = "/Scheduler/StaffSchedule";
        public const string AllAppointments = "/Scheduler/AllAppointments";
        public const string Create = "/Scheduler/Create";
        public const string Details = "/Scheduler/Details";
        public const string Edit = "/Scheduler/Edit";
    }
}
