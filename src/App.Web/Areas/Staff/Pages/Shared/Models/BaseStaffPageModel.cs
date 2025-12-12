using App.Application.Common.Interfaces;
using App.Application.Common.Security;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Shared.Models;

/// <summary>
/// Base page model for all Staff area pages.
/// Requires admin access. Permission checks for specific actions
/// are handled at the individual page or command level.
/// </summary>
[Area("Staff")]
[Authorize(Policy = RaythaClaimTypes.IsAdmin)]
public abstract class BaseStaffPageModel : BasePageModel
{
    private ITicketPermissionService? _ticketPermissionService;

    /// <summary>
    /// Gets the ticket permission service for checking ticketing-specific permissions.
    /// </summary>
    protected ITicketPermissionService TicketPermissionService =>
        _ticketPermissionService ??=
            HttpContext.RequestServices.GetRequiredService<ITicketPermissionService>();
}
