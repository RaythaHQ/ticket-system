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
    /// Returns true if the current user can create/edit/delete system views.
    /// </summary>
    bool CanManageSystemViews();

    /// <summary>
    /// Returns true if the current user can edit a specific ticket.
    /// User can edit if they have CanManageTickets permission, are assigned to the ticket, or are a member of the ticket's team.
    /// </summary>
    Task<bool> CanEditTicketAsync(Guid? assigneeId, Guid? owningTeamId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Throws ForbiddenAccessException if user cannot manage system views.
    /// </summary>
    void RequireCanManageSystemViews();

    /// <summary>
    /// Throws ForbiddenAccessException if user cannot edit the specified ticket.
    /// </summary>
    Task RequireCanEditTicketAsync(Guid? assigneeId, Guid? owningTeamId, CancellationToken cancellationToken = default);
}

