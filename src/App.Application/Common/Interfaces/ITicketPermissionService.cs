namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for checking ticketing system permissions for the current user.
/// </summary>
public interface ITicketPermissionService
{
    /// <summary>
    /// Returns true if the current user can modify ticket attributes, reassign, and close/reopen tickets.
    /// </summary>
    bool CanManageTickets();

    /// <summary>
    /// Returns true if the current user can create/edit/delete teams and manage membership.
    /// </summary>
    bool CanManageTeams();

    /// <summary>
    /// Returns true if the current user can access team-level and organization-level reports.
    /// </summary>
    bool CanAccessReports();

    /// <summary>
    /// Throws ForbiddenAccessException if user cannot manage tickets.
    /// </summary>
    void RequireCanManageTickets();

    /// <summary>
    /// Throws ForbiddenAccessException if user cannot manage teams.
    /// </summary>
    void RequireCanManageTeams();

    /// <summary>
    /// Throws ForbiddenAccessException if user cannot access reports.
    /// </summary>
    void RequireCanAccessReports();
}

