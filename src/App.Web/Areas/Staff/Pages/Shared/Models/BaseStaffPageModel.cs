using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Interfaces;
using App.Web.Areas.Shared.Models;

namespace App.Web.Areas.Staff.Pages.Shared.Models;

/// <summary>
/// Base page model for all Staff area pages.
/// Requires authenticated user. Permission checks for specific actions
/// are handled at the individual page or command level.
/// </summary>
[Area("Staff")]
[Authorize]
public abstract class BaseStaffPageModel : BasePageModel
{
    private ITicketPermissionService? _ticketPermissionService;
    private IAppDbContext? _db;

    /// <summary>
    /// Gets the ticket permission service for checking ticketing-specific permissions.
    /// </summary>
    protected ITicketPermissionService TicketPermissionService =>
        _ticketPermissionService ??= HttpContext.RequestServices.GetRequiredService<ITicketPermissionService>();

    /// <summary>
    /// Gets the database context for direct queries.
    /// </summary>
    protected IAppDbContext Db =>
        _db ??= HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
}

