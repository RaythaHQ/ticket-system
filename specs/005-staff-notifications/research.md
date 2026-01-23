# Research: Staff Notifications Center

**Feature**: 005-staff-notifications  
**Date**: 2026-01-23

## Technical Decisions

### 1. Notification Recording Strategy

**Decision**: Record notifications in `InAppNotificationService` before delivery, creating a single recording point for all notification types.

**Rationale**:
- Centralizes recording logic in one place
- All 6 existing notification event handlers already call `IInAppNotificationService`
- Guarantees notification is recorded regardless of delivery preferences
- Minimal changes to existing code

**Alternatives Considered**:
- Recording in each event handler: Requires modifying 6+ handlers, higher risk of missing recordings
- Separate domain event for recording: Adds complexity, notification creation becomes two-step process

**Implementation Approach**:
```csharp
// In IInAppNotificationService, add:
Task<Guid> RecordNotificationAsync(
    Guid recipientUserId,
    string eventType,
    string title,
    string message,
    string? url = null,
    long? ticketId = null,
    CancellationToken cancellationToken = default);
```

Event handlers continue to call `SendToUserAsync` which internally:
1. Records the notification to database (always)
2. Checks delivery preferences
3. Sends via SignalR only if `InAppEnabled` is true

### 2. Notification Entity Design

**Decision**: Simple flat entity with optional ticket reference, no complex relationships.

**Rationale**:
- Notifications are immutable after creation (except read status)
- Ticket reference is optional (some notifications may not relate to tickets)
- No need for notification categories/grouping for MVP
- Keeps queries simple and fast

**Entity Fields**:
| Field | Type | Purpose |
|-------|------|---------|
| `Id` | Guid | Primary key |
| `RecipientUserId` | Guid | FK to User |
| `EventType` | string | Notification type (from NotificationEventType) |
| `Title` | string | Short title for display |
| `Message` | string | Full notification message |
| `Url` | string? | Optional link to related resource |
| `TicketId` | long? | Optional FK to Ticket |
| `IsRead` | bool | Read/unread status |
| `CreatedAt` | DateTime | Timestamp for sorting |
| `ReadAt` | DateTime? | When marked as read |

### 3. Query Performance Strategy

**Decision**: Index on `(RecipientUserId, IsRead, CreatedAt DESC)` for optimal filtering.

**Rationale**:
- Primary query pattern: "Get my unread notifications, newest first"
- Secondary pattern: "Get all my notifications, newest first"
- Composite index covers both efficiently

**Query Pattern**:
```sql
-- Primary: Unread notifications for user
SELECT * FROM Notifications 
WHERE RecipientUserId = @userId AND IsRead = false 
ORDER BY CreatedAt DESC;

-- Secondary: All notifications for user
SELECT * FROM Notifications 
WHERE RecipientUserId = @userId 
ORDER BY CreatedAt DESC;
```

### 4. Sidebar Badge Implementation

**Decision**: Server-render initial count, update via SignalR on changes.

**Rationale**:
- Accurate count on page load (no flash of incorrect value)
- Real-time updates when notifications arrive or are marked read
- Uses existing SignalR infrastructure

**Implementation**:
1. Layout injects `INotificationCountService` for initial render
2. SignalR `NotificationHub` broadcasts count updates to user's group
3. Client JS updates badge via `ReceiveUnreadCountUpdate` event

**Badge Display Rules**:
- 0 notifications: Hide badge completely
- 1-99 notifications: Show exact count
- 100+ notifications: Show "99+"

### 5. Filter and Sort URL Strategy

**Decision**: All filters stored in URL query parameters.

**Rationale**:
- Enables sharing filtered views via URL
- Browser back/forward navigation works correctly
- No session state complexity
- Consistent with existing ticket list patterns

**URL Pattern**:
```
/staff/notifications?filter=unread&type=ticket_assigned&sort=desc&page=1
```

**Parameters**:
| Parameter | Values | Default |
|-----------|--------|---------|
| `filter` | `all`, `unread`, `read` | `unread` |
| `type` | Any NotificationEventType.DeveloperName | (none = all types) |
| `sort` | `asc`, `desc` | `desc` |
| `page` | 1-N | `1` |

### 6. Mark All As Read Behavior

**Decision**: "Mark All As Read" applies to currently filtered notifications, not all notifications globally.

**Rationale**:
- Matches user expectation when viewing filtered list
- Prevents accidental marking of unseen notifications
- More useful for workflows like "clear all SLA alerts"

**Command Structure**:
```csharp
public record Command
{
    public string? FilterStatus { get; init; }  // "unread", "read", "all"
    public string? FilterType { get; init; }    // NotificationEventType.DeveloperName
}
```

### 7. Notification Retention

**Decision**: No automatic retention/deletion for MVP. Notifications retained indefinitely.

**Rationale**:
- Per spec assumption A-007
- Simplifies initial implementation
- Can add retention policy later if storage becomes concern

**Future Consideration**:
- ENV variable for `NOTIFICATION_RETENTION_DAYS`
- Background task to purge old notifications
- Or: Archive read notifications after 90 days

### 8. Real-time Notification List Updates

**Decision**: Show "New notifications available" banner instead of auto-refresh.

**Rationale**:
- Prevents jarring list changes while user is reading
- User controls when to refresh
- Simpler to implement reliably

**Implementation**:
```javascript
// On receiving new notification via SignalR
if (currentPage === '/staff/notifications') {
    showBanner("New notifications available. Click to refresh.");
}
```

## Dependencies

### Existing Infrastructure Used

| Component | Usage |
|-----------|-------|
| `IInAppNotificationService` | Extended to record notifications |
| `NotificationHub` | Push badge updates and list refresh hints |
| `NotificationEventType` | Existing value object for notification types |
| `GetPagedEntitiesInputDto` | Base class for paginated query |
| `QueryableExtensions` | Pagination helpers |
| `BasePageModel` | Staff page base class |

### New Components Required

| Component | Location | Purpose |
|-----------|----------|---------|
| `Notification` | App.Domain/Entities | Entity for stored notifications |
| `NotificationDto` | App.Application/Notifications | DTO for API responses |
| `GetNotifications` | App.Application/Notifications/Queries | Paginated list query |
| `GetUnreadNotificationCount` | App.Application/Notifications/Queries | Badge count query |
| `MarkNotificationAsRead` | App.Application/Notifications/Commands | Mark single as read |
| `MarkNotificationAsUnread` | App.Application/Notifications/Commands | Mark single as unread |
| `MarkAllNotificationsAsRead` | App.Application/Notifications/Commands | Bulk mark as read |
| `RecordNotification` | App.Application/Notifications/Commands | Create notification record |
| `NotificationConfiguration` | App.Infrastructure/Persistence | EF Core config with indexes |
| `Index.cshtml` | App.Web/Areas/Staff/Pages/Notifications | List page |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| High notification volume per user | Medium | Pagination + efficient indexes; monitor query times |
| Badge count query performance | Medium | Cache count for 30s or use materialized view if needed |
| SignalR connection drops | Low | Client reconnects automatically; badge refreshes on page load |
| Deleted tickets referenced | Low | Handle null ticket gracefully in display |

## Open Questions (Resolved)

1. ~~Should clicking notification auto-mark as read?~~ → Yes, when navigating to related ticket
2. ~~Should bulk delete be supported?~~ → No, not in spec; can add later
3. ~~What about read/unread toggle?~~ → Yes, single button toggles state
4. ~~Should notification grouping be supported?~~ → No, flat list for MVP

