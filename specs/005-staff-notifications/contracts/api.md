# API Contracts: Staff Notifications Center

**Feature**: 005-staff-notifications  
**Date**: 2026-01-23

## Commands

### RecordNotification

**File**: `src/App.Application/Notifications/Commands/RecordNotification.cs`

Creates a notification record for a user. Called internally by the notification delivery service.

#### Command

```csharp
public record Command : IRequest<CommandResponseDto<Guid>>
{
    /// <summary>
    /// The user ID to create the notification for.
    /// </summary>
    public Guid RecipientUserId { get; init; }

    /// <summary>
    /// The notification event type (from NotificationEventType).
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Short title for display.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Full notification message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Optional URL to navigate to.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Optional related ticket ID.
    /// </summary>
    public long? TicketId { get; init; }
}
```

#### Validation Rules

| Rule | Error Message |
|------|---------------|
| `RecipientUserId` must be non-empty | "Recipient user ID is required." |
| `EventType` must be valid | "Invalid notification event type." |
| `Title` max 200 chars | "Title cannot exceed 200 characters." |
| `Message` max 1000 chars | "Message cannot exceed 1000 characters." |

#### Response

```csharp
CommandResponseDto<Guid>
{
    Success = true,
    Result = notificationId // The created notification ID
}
```

#### Side Effects

1. Creates `Notification` entity with `IsRead = false`
2. Broadcasts unread count update to user via SignalR

---

### MarkNotificationAsRead

**File**: `src/App.Application/Notifications/Commands/MarkNotificationAsRead.cs`

Marks a single notification as read.

#### Command

```csharp
public record Command : IRequest<CommandResponseDto<bool>>
{
    /// <summary>
    /// The notification ID to mark as read.
    /// </summary>
    public ShortGuid Id { get; init; }
}
```

#### Validation Rules

| Rule | Error Message |
|------|---------------|
| `Id` must exist | "Notification not found." |
| Current user must be recipient | "You do not have permission to access this notification." |

#### Response

```csharp
CommandResponseDto<bool>
{
    Success = true,
    Result = true
}
```

#### Side Effects

1. Sets `Notification.IsRead = true`
2. Sets `Notification.ReadAt = DateTime.UtcNow`
3. Broadcasts unread count update to user via SignalR

---

### MarkNotificationAsUnread

**File**: `src/App.Application/Notifications/Commands/MarkNotificationAsUnread.cs`

Marks a single notification as unread.

#### Command

```csharp
public record Command : IRequest<CommandResponseDto<bool>>
{
    /// <summary>
    /// The notification ID to mark as unread.
    /// </summary>
    public ShortGuid Id { get; init; }
}
```

#### Validation Rules

| Rule | Error Message |
|------|---------------|
| `Id` must exist | "Notification not found." |
| Current user must be recipient | "You do not have permission to access this notification." |

#### Response

```csharp
CommandResponseDto<bool>
{
    Success = true,
    Result = true
}
```

#### Side Effects

1. Sets `Notification.IsRead = false`
2. Sets `Notification.ReadAt = null`
3. Broadcasts unread count update to user via SignalR

---

### MarkAllNotificationsAsRead

**File**: `src/App.Application/Notifications/Commands/MarkAllNotificationsAsRead.cs`

Marks all notifications matching the current filter as read.

#### Command

```csharp
public record Command : IRequest<CommandResponseDto<int>>
{
    /// <summary>
    /// Filter by read status: "all", "unread", "read".
    /// Default: "unread" (only unread notifications).
    /// </summary>
    public string? FilterStatus { get; init; } = "unread";

    /// <summary>
    /// Filter by notification type (NotificationEventType.DeveloperName).
    /// If null/empty, applies to all types.
    /// </summary>
    public string? FilterType { get; init; }
}
```

#### Validation Rules

| Rule | Error Message |
|------|---------------|
| `FilterStatus` must be valid | "Invalid filter status. Use 'all', 'unread', or 'read'." |
| `FilterType` if provided, must be valid | "Invalid notification type." |

#### Response

```csharp
CommandResponseDto<int>
{
    Success = true,
    Result = 25 // Number of notifications marked as read
}
```

#### Side Effects

1. Updates all matching `Notification` records: `IsRead = true`, `ReadAt = DateTime.UtcNow`
2. Broadcasts unread count update to user via SignalR

---

## Queries

### GetNotifications

**File**: `src/App.Application/Notifications/Queries/GetNotifications.cs`

Returns paginated notifications for the current user.

#### Query

```csharp
public record Query : GetPagedEntitiesInputDto, 
    IRequest<IQueryResponseDto<ListResultDto<NotificationListItemDto>>>
{
    public override int PageSize { get; init; } = 25;
    public override string OrderBy { get; init; } = "CreatedAt desc";

    /// <summary>
    /// Filter by read status: "all", "unread", "read".
    /// Default: "unread".
    /// </summary>
    public string FilterStatus { get; init; } = "unread";

    /// <summary>
    /// Filter by notification type (NotificationEventType.DeveloperName).
    /// If null/empty, returns all types.
    /// </summary>
    public string? FilterType { get; init; }
}
```

#### Response

```csharp
ListResultDto<NotificationListItemDto>
{
    TotalCount = 150,
    Items = [
        {
            Id = "abc123",
            EventType = "ticket_assigned",
            EventTypeLabel = "Ticket Assigned",
            Title = "Ticket #1234 assigned to you",
            Url = "/staff/tickets/1234",
            TicketId = 1234,
            IsRead = false,
            CreatedAt = "2026-01-23T10:30:00Z",
            CreatedAtRelative = "5 minutes ago"
        },
        // ... more items
    ]
}
```

#### Example Requests

**Default (unread, newest first)**:
```
GET /staff/notifications
```

**All notifications**:
```
GET /staff/notifications?filterStatus=all
```

**Filter by type**:
```
GET /staff/notifications?filterType=ticket_assigned
```

**Combined filters with ascending sort**:
```
GET /staff/notifications?filterStatus=all&filterType=sla_approaching&orderBy=CreatedAt+asc
```

---

### GetUnreadNotificationCount

**File**: `src/App.Application/Notifications/Queries/GetUnreadNotificationCount.cs`

Returns the count of unread notifications for the current user. Used for sidebar badge.

#### Query

```csharp
public record Query : IRequest<IQueryResponseDto<int>> { }
```

#### Response

```csharp
QueryResponseDto<int>
{
    Success = true,
    Result = 5 // Number of unread notifications
}
```

---

## Page Handlers

### Notifications/Index.cshtml.cs

#### OnGet

**Route**: `GET /staff/notifications`

Displays the notification list with filters.

**Query Parameters**:
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `filterStatus` | string | `unread` | Filter: `all`, `unread`, `read` |
| `filterType` | string? | null | NotificationEventType filter |
| `orderBy` | string | `CreatedAt desc` | Sort order |
| `pageNumber` | int | 1 | Current page |
| `pageSize` | int | 25 | Items per page |

**Response**: Razor page with notification list

---

#### OnPostMarkAsRead

**Route**: `POST /staff/notifications?handler=MarkAsRead`

Marks a single notification as read.

**Form Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | ShortGuid | Notification ID |

**Response**: Redirect to notification list with current filters

---

#### OnPostMarkAsUnread

**Route**: `POST /staff/notifications?handler=MarkAsUnread`

Marks a single notification as unread.

**Form Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | ShortGuid | Notification ID |

**Response**: Redirect to notification list with current filters

---

#### OnPostMarkAllAsRead

**Route**: `POST /staff/notifications?handler=MarkAllAsRead`

Marks all visible (filtered) notifications as read.

**Form Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `filterStatus` | string | Current filter status |
| `filterType` | string? | Current type filter |

**Response**: Redirect to notification list with success message

---

#### OnGetUnreadCount

**Route**: `GET /staff/notifications?handler=UnreadCount`

Returns unread count as JSON (for AJAX updates).

**Response** (JSON):
```json
{
    "count": 5,
    "display": "5"
}
```

Or for 100+ notifications:
```json
{
    "count": 150,
    "display": "99+"
}
```

---

## SignalR Events

### NotificationHub Extensions

#### ReceiveUnreadCountUpdate

Sent to user when their unread count changes.

**Event Name**: `ReceiveUnreadCountUpdate`

**Payload**:
```json
{
    "count": 5,
    "display": "5"
}
```

**Client Handler**:
```javascript
connection.on("ReceiveUnreadCountUpdate", function(data) {
    updateBadge(data.display);
});
```

---

#### NewNotificationAvailable

Sent when a new notification is created while user is on the notifications page.

**Event Name**: `NewNotificationAvailable`

**Payload**:
```json
{
    "message": "New notifications available"
}
```

**Client Handler**:
```javascript
connection.on("NewNotificationAvailable", function(data) {
    showRefreshBanner(data.message);
});
```

---

## Notification Event Types

These are the existing types from `NotificationEventType` that will be used:

| Developer Name | Label | Description |
|----------------|-------|-------------|
| `ticket_assigned` | Ticket Assigned | Ticket assigned to user |
| `ticket_assigned_team` | Ticket Assigned to Team | Ticket assigned to user's team |
| `comment_added` | Comment Added | New comment on ticket |
| `status_changed` | Status Changed | Ticket status changed |
| `ticket_reopened` | Ticket Reopened | Closed ticket reopened |
| `sla_approaching` | SLA Approaching Breach | SLA due date approaching |
| `sla_breached` | SLA Breached | SLA due date passed |

---

## Error Codes

| Code | HTTP Status | Condition |
|------|-------------|-----------|
| `NOTIFICATION_NOT_FOUND` | 404 | Notification ID does not exist |
| `ACCESS_DENIED` | 403 | User is not the recipient of the notification |
| `INVALID_FILTER` | 400 | Invalid filter parameter value |
| `INVALID_EVENT_TYPE` | 400 | Unknown notification event type |

