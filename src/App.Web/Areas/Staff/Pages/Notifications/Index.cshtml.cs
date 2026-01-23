using App.Application.Notifications;
using App.Application.Notifications.Commands;
using App.Application.Notifications.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Shared.Models;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Notifications;

/// <summary>
/// Page model for the notifications center.
/// </summary>
public class Index : BaseStaffPageModel, IHasListView<NotificationListItemDto>
{
    /// <summary>
    /// Gets or sets the list view model containing paginated notification data.
    /// </summary>
    public ListViewModel<NotificationListItemDto> ListView { get; set; } =
        new(Enumerable.Empty<NotificationListItemDto>(), 0);

    /// <summary>
    /// Current filter status (all, unread, read).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string FilterStatus { get; set; } = "unread";

    /// <summary>
    /// Current filter by notification type.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FilterType { get; set; }

    /// <summary>
    /// Sort order (asc or desc). Default is desc (newest first).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Available notification types for the filter dropdown.
    /// </summary>
    public IEnumerable<NotificationEventType> NotificationTypes => NotificationEventType.SupportedTypes;

    /// <summary>
    /// Handles GET requests to display the paginated list of notifications.
    /// </summary>
    public async Task<IActionResult> OnGet(
        string filterStatus = "unread",
        string? filterType = null,
        string sortDirection = "desc",
        string search = "",
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default
    )
    {
        ViewData["Title"] = "My Notifications";
        ViewData["ActiveMenu"] = "Notifications";

        FilterStatus = filterStatus;
        FilterType = filterType;
        SortDirection = sortDirection;

        var orderBy = sortDirection.ToLower() == "asc"
            ? $"CreatedAt {SortOrder.ASCENDING}"
            : $"CreatedAt {SortOrder.DESCENDING}";

        var input = new GetNotifications.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
            FilterStatus = filterStatus,
            FilterType = filterType
        };

        var response = await Mediator.Send(input, cancellationToken);

        ListView = new ListViewModel<NotificationListItemDto>(
            response.Result.Items,
            response.Result.TotalCount
        )
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Page();
    }

    /// <summary>
    /// Gets the CSS class for read/unread visual distinction.
    /// </summary>
    public string GetNotificationRowClass(NotificationListItemDto notification)
    {
        return notification.IsRead ? "notification-read" : "notification-unread";
    }

    /// <summary>
    /// Gets the icon class for the notification type.
    /// </summary>
    public string GetNotificationIcon(string eventType)
    {
        return eventType switch
        {
            NotificationEventType.TICKET_ASSIGNED => "bi-person-check",
            NotificationEventType.TICKET_ASSIGNED_TEAM => "bi-people",
            NotificationEventType.COMMENT_ADDED => "bi-chat-dots",
            NotificationEventType.STATUS_CHANGED => "bi-arrow-repeat",
            NotificationEventType.TICKET_REOPENED => "bi-arrow-counterclockwise",
            NotificationEventType.SLA_APPROACHING => "bi-clock",
            NotificationEventType.SLA_BREACHED => "bi-exclamation-triangle",
            _ => "bi-bell"
        };
    }

    /// <summary>
    /// Gets the icon color class for the notification type.
    /// </summary>
    public string GetNotificationIconColor(string eventType)
    {
        return eventType switch
        {
            NotificationEventType.TICKET_ASSIGNED => "text-primary",
            NotificationEventType.TICKET_ASSIGNED_TEAM => "text-info",
            NotificationEventType.COMMENT_ADDED => "text-success",
            NotificationEventType.STATUS_CHANGED => "text-secondary",
            NotificationEventType.TICKET_REOPENED => "text-warning",
            NotificationEventType.SLA_APPROACHING => "text-warning",
            NotificationEventType.SLA_BREACHED => "text-danger",
            _ => "text-muted"
        };
    }

    /// <summary>
    /// Handles POST request to mark a notification as read.
    /// </summary>
    public async Task<IActionResult> OnPostMarkAsRead(
        string id,
        CancellationToken cancellationToken = default)
    {
        var response = await Mediator.Send(
            new MarkNotificationAsRead.Command { Id = new ShortGuid(id) },
            cancellationToken);

        if (!response.Success)
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(new
        {
            filterStatus = FilterStatus,
            filterType = FilterType,
            sortDirection = SortDirection
        });
    }

    /// <summary>
    /// Handles POST request to navigate to notification URL and mark as read.
    /// </summary>
    public async Task<IActionResult> OnPostNavigate(
        string id,
        string? url,
        CancellationToken cancellationToken = default)
    {
        // Mark as read
        await Mediator.Send(
            new MarkNotificationAsRead.Command { Id = new ShortGuid(id) },
            cancellationToken);

        // Navigate to URL or back to list
        if (!string.IsNullOrEmpty(url))
        {
            return Redirect(url);
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST request to mark a notification as unread.
    /// </summary>
    public async Task<IActionResult> OnPostMarkAsUnread(
        string id,
        CancellationToken cancellationToken = default)
    {
        var response = await Mediator.Send(
            new MarkNotificationAsUnread.Command { Id = new ShortGuid(id) },
            cancellationToken);

        if (!response.Success)
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(new
        {
            filterStatus = FilterStatus,
            filterType = FilterType,
            sortDirection = SortDirection
        });
    }

    /// <summary>
    /// Handles POST request to mark all visible notifications as read.
    /// </summary>
    public async Task<IActionResult> OnPostMarkAllAsRead(
        CancellationToken cancellationToken = default)
    {
        var response = await Mediator.Send(
            new MarkAllNotificationsAsRead.Command
            {
                FilterStatus = FilterStatus,
                FilterType = FilterType
            },
            cancellationToken);

        if (response.Success)
        {
            var count = response.Result;
            SetSuccessMessage($"Marked {count} notification{(count == 1 ? "" : "s")} as read.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(new
        {
            filterStatus = FilterStatus,
            filterType = FilterType,
            sortDirection = SortDirection
        });
    }
}

