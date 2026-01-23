# Quickstart: Staff Notifications Center

**Feature**: 005-staff-notifications  
**Date**: 2026-01-23

## Overview

This feature adds a notification center for staff users that records all notification events and provides a central UI to browse, filter, and manage notifications. A badge in the sidebar shows the unread count.

## Prerequisites

- .NET 8 SDK
- PostgreSQL database
- Existing ticket-system development environment

## Database Setup

After implementing the migration, apply it:

```bash
cd src/App.Web
dotnet ef database update
```

## Key Files

### Domain Layer
- `src/App.Domain/Entities/Notification.cs` - New notification entity

### Application Layer
- `src/App.Application/Notifications/NotificationDto.cs` - DTOs
- `src/App.Application/Notifications/Commands/RecordNotification.cs` - Create notification
- `src/App.Application/Notifications/Commands/MarkNotificationAsRead.cs` - Mark single as read
- `src/App.Application/Notifications/Commands/MarkNotificationAsUnread.cs` - Mark single as unread
- `src/App.Application/Notifications/Commands/MarkAllNotificationsAsRead.cs` - Bulk mark as read
- `src/App.Application/Notifications/Queries/GetNotifications.cs` - Paginated list query
- `src/App.Application/Notifications/Queries/GetUnreadNotificationCount.cs` - Badge count query
- `src/App.Application/Common/Interfaces/IInAppNotificationService.cs` - Modified interface

### Infrastructure Layer
- `src/App.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs` - EF Core config
- `src/App.Infrastructure/Migrations/YYYYMMDD_AddNotifications.cs` - Database migration

### Web Layer
- `src/App.Web/Services/InAppNotificationService.cs` - Modified to record notifications
- `src/App.Web/Areas/Staff/Pages/Shared/RouteNames.cs` - New route constants
- `src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml` - Badge in sidebar
- `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml` - Notification list page
- `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml.cs` - Page model

## Usage

### Viewing Notifications

1. Log in as a staff user
2. Click "My Notifications" in the left sidebar (below Dashboard)
3. By default, you see unread notifications sorted newest first

### Filtering Notifications

**By Read Status**:
- Click "Unread" (default), "Read", or "All" tabs

**By Notification Type**:
- Use the dropdown to filter by type (e.g., "Ticket Assigned", "SLA Approaching")

**Sorting**:
- Click the sort toggle to switch between newest first (↓) and oldest first (↑)

### Marking Notifications

**Single Notification**:
- Click the ○/● icon to toggle read/unread status
- Or click the notification to navigate to the related ticket (auto-marks as read)

**All Visible Notifications**:
- Click "Mark All as Read" button
- Only marks currently filtered notifications (not all notifications globally)

### Sidebar Badge

- Shows count of unread notifications
- Updates in real-time when notifications arrive or are marked
- Shows "99+" for 100+ notifications
- Hidden when count is 0

## Notification Types

| Type | Label | Trigger |
|------|-------|---------|
| `ticket_assigned` | Ticket Assigned | Ticket assigned directly to you |
| `ticket_assigned_team` | Ticket Assigned to Team | Ticket assigned to your team |
| `comment_added` | Comment Added | Someone comments on a ticket you're involved with |
| `status_changed` | Status Changed | Status changes on a ticket you're assigned to or following |
| `ticket_reopened` | Ticket Reopened | A closed ticket is reopened |
| `sla_approaching` | SLA Approaching | SLA due date is approaching on your ticket |
| `sla_breached` | SLA Breached | SLA has been breached on your ticket |

## Recording vs Delivery

**Key Distinction**:
- **Recording**: All notifications are ALWAYS recorded to the database
- **Delivery**: Email/in-app delivery respects user preferences

This means:
- Your notification center always shows ALL notifications relevant to you
- Even if you disabled email/in-app for a type, it still appears here
- The notification center is your complete audit trail

## Testing

### Manual Testing Checklist

1. **Basic Viewing**
   - Navigate to My Notifications
   - Verify notifications display with correct info
   - Verify pagination works

2. **Filtering**
   - Switch between Unread/Read/All tabs
   - Filter by notification type
   - Verify counts update correctly

3. **Mark as Read/Unread**
   - Mark a single notification as read
   - Verify it disappears from Unread view
   - Mark it as unread, verify it reappears

4. **Mark All as Read**
   - With filter applied, click Mark All as Read
   - Verify only filtered notifications are marked
   - Verify badge updates

5. **Sidebar Badge**
   - Verify badge shows correct count
   - Mark notifications as read, verify badge decrements
   - Verify badge hides when count is 0
   - Verify 99+ display for 100+ notifications

6. **Click-through**
   - Click a notification with ticket reference
   - Verify navigation to ticket
   - Verify notification is marked as read

7. **Real-time Updates**
   - Open two browser tabs
   - Mark notification as read in one
   - Verify badge updates in the other

8. **Notification Recording**
   - Disable email/in-app for a notification type in preferences
   - Trigger that notification type
   - Verify notification still appears in My Notifications

### Unit Tests to Write

```csharp
// RecordNotification.Command Tests
[Fact] public async Task Handle_ValidCommand_CreatesNotification()
[Fact] public async Task Handle_ValidCommand_SetsIsReadFalse()
[Fact] public async Task Handle_InvalidEventType_ReturnsValidationError()
[Fact] public async Task Handle_TitleTooLong_ReturnsValidationError()

// MarkNotificationAsRead.Command Tests
[Fact] public async Task Handle_OwnNotification_MarksAsRead()
[Fact] public async Task Handle_OwnNotification_SetsReadAt()
[Fact] public async Task Handle_NotFound_ReturnsError()
[Fact] public async Task Handle_OtherUserNotification_ReturnsForbidden()

// MarkNotificationAsUnread.Command Tests
[Fact] public async Task Handle_OwnNotification_MarksAsUnread()
[Fact] public async Task Handle_OwnNotification_ClearsReadAt()

// MarkAllNotificationsAsRead.Command Tests
[Fact] public async Task Handle_NoFilter_MarksAllUnread()
[Fact] public async Task Handle_TypeFilter_MarksOnlyMatchingType()
[Fact] public async Task Handle_ReturnsCount()

// GetNotifications.Query Tests
[Fact] public async Task Handle_DefaultFilter_ReturnsOnlyUnread()
[Fact] public async Task Handle_AllFilter_ReturnsAllNotifications()
[Fact] public async Task Handle_TypeFilter_ReturnsOnlyMatchingType()
[Fact] public async Task Handle_Pagination_WorksCorrectly()
[Fact] public async Task Handle_SortAscending_ReturnsOldestFirst()
[Fact] public async Task Handle_OnlyReturnsCurrentUserNotifications()

// GetUnreadNotificationCount.Query Tests
[Fact] public async Task Handle_ReturnsCorrectCount()
[Fact] public async Task Handle_ZeroNotifications_ReturnsZero()
[Fact] public async Task Handle_OnlyCountsUnread()
[Fact] public async Task Handle_OnlyCountsCurrentUser()
```

## API Reference

See [contracts/api.md](./contracts/api.md) for detailed API documentation.

## Troubleshooting

### Notifications Not Appearing

1. Check that notification event handlers are being triggered
2. Verify `InAppNotificationService.RecordNotificationAsync` is being called
3. Check database for notification records: `SELECT * FROM "Notifications" WHERE "RecipientUserId" = 'your-user-id'`

### Badge Not Updating

1. Ensure SignalR connection is established
2. Check browser console for WebSocket errors
3. Verify `NotificationHub` is sending `ReceiveUnreadCountUpdate` events
4. Test with page refresh (should show correct count on load)

### Wrong Count in Badge

1. Check index is created: `IX_Notifications_RecipientUserId_IsRead_CreatedAt`
2. Verify query is filtering by current user ID
3. Check for data inconsistencies (IsRead = true but ReadAt = null)

### Slow Performance

1. Check query execution plan for notification list
2. Verify indexes are being used
3. Consider reducing page size if user has many notifications
4. Monitor for N+1 queries when loading ticket titles

## Architecture Notes

- **Recording Point**: Notifications are recorded in `InAppNotificationService` when sending, not in event handlers
- **User Scoping**: All queries automatically scope to current user via `ICurrentUser`
- **Delivery vs Recording**: Recording is always enabled; delivery respects `NotificationPreference`
- **Badge Updates**: SignalR broadcasts to user's group on every create/read/unread operation
- **Pagination**: Default 25 per page; uses existing `GetPagedEntitiesInputDto` pattern
- **UI Pattern**: Follows Staff area patterns with `.staff-card`, `.staff-table`, `.staff-badge`

