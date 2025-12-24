using App.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace App.Web.Services;

/// <summary>
/// Service to check if notifications should be suppressed for the current request.
/// Checks for X-API-SuppressNotifications or X-SuppressNotifications header.
/// </summary>
public class NotificationSuppressionService : INotificationSuppressionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationSuppressionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool ShouldSuppressNotifications()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context == null)
            return false;

        // Check for X-API-SuppressNotifications header (primary)
        var suppressHeader = context
            .Request.Headers["X-API-SuppressNotifications"]
            .FirstOrDefault();
        if (!string.IsNullOrEmpty(suppressHeader))
        {
            // Accept "true", "1", "yes" (case-insensitive)
            return suppressHeader.Equals("true", StringComparison.OrdinalIgnoreCase)
                || suppressHeader.Equals("1", StringComparison.OrdinalIgnoreCase)
                || suppressHeader.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        // Also check X-SuppressNotifications as an alternative
        suppressHeader = context.Request.Headers["X-SuppressNotifications"].FirstOrDefault();
        if (!string.IsNullOrEmpty(suppressHeader))
        {
            return suppressHeader.Equals("true", StringComparison.OrdinalIgnoreCase)
                || suppressHeader.Equals("1", StringComparison.OrdinalIgnoreCase)
                || suppressHeader.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
