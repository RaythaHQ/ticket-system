# Feature Specification: Staff Notifications Center

**Feature Branch**: `005-staff-notifications`  
**Created**: January 23, 2026  
**Status**: Draft  
**Input**: User description: "We need a new feature that is available to all staff users. On staff side, it should appear under Dashboard in the left side navigation called My Notifications. If you click this, it should have an awesome UI that lets you browse all of your notifications in Created At descending order by default, but you can swap to ascending order. It should default to only showing Unread notifications, but you should be able to filter to All, Read, and different notification types. The notification types are the same ones that you can toggle on and off as a user to receive such notification messages for email and in-app. However, whether you opt to receive a notification or not, all your notifications will appear in this UI even if you chose not to receive an email or in-app message. You should be able to mark a notification as read, or mark all as read, or mark a read one as unread. The UI should be absolutely awesome and intuitive. The left side navigation bar should show a little bubble with the number of unread notifications."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Unread Notifications (Priority: P1)

As a staff user, I want to see all my unread notifications in a dedicated page so that I can quickly review what needs my attention without switching between different screens or relying on email.

**Why this priority**: This is the core value proposition - giving staff immediate visibility into all pending notifications in one central location. Without this, the entire feature has no purpose.

**Independent Test**: Can be fully tested by navigating to My Notifications and seeing a list of unread notifications. Delivers immediate value by consolidating notification awareness.

**Acceptance Scenarios**:

1. **Given** I am logged in as a staff user and I have unread notifications, **When** I navigate to "My Notifications" from the left sidebar, **Then** I see a list of my unread notifications sorted by created date in descending order (newest first)

2. **Given** I am logged in as a staff user and I have no unread notifications, **When** I navigate to "My Notifications", **Then** I see an empty state indicating I have no unread notifications with a friendly message

3. **Given** I am viewing My Notifications, **When** I look at each notification item, **Then** I can see the notification type, a summary message, the related ticket (if applicable), and the timestamp

---

### User Story 2 - Filter and Sort Notifications (Priority: P2)

As a staff user, I want to filter my notifications by read status and notification type, and change the sort order, so that I can quickly find specific notifications I'm looking for.

**Why this priority**: Filtering enhances discoverability and becomes essential as notification volume grows. While viewing is possible without filtering, it becomes cumbersome with many notifications.

**Independent Test**: Can be tested by applying filters and verifying the list updates correctly. Delivers value by reducing cognitive load when reviewing notifications.

**Acceptance Scenarios**:

1. **Given** I am on My Notifications, **When** I change the filter from "Unread" to "All", **Then** I see both read and unread notifications, with visual distinction between them

2. **Given** I am on My Notifications, **When** I filter by "Read", **Then** I see only previously read notifications

3. **Given** I am on My Notifications, **When** I filter by a specific notification type (e.g., "Ticket Assigned"), **Then** I see only notifications of that type

4. **Given** I am viewing notifications sorted by newest first, **When** I click to change sort order to ascending, **Then** the list re-orders to show oldest notifications first

5. **Given** I have applied filters, **When** I click a "Clear filters" option, **Then** the view resets to the default (Unread only, newest first)

---

### User Story 3 - Mark Notification as Read (Priority: P2)

As a staff user, I want to mark individual notifications as read so that I can track which notifications I've already reviewed.

**Why this priority**: Reading and marking as read is fundamental to notification management. This is closely tied to P1 but represents a distinct action capability.

**Independent Test**: Can be tested by marking a notification as read and verifying it disappears from the Unread filter view. Delivers value by enabling notification tracking.

**Acceptance Scenarios**:

1. **Given** I am viewing an unread notification, **When** I mark it as read (via button, checkbox, or action), **Then** the notification is visually marked as read and no longer appears when filtering by "Unread"

2. **Given** I click on a notification to view its details or navigate to the related ticket, **When** the action completes, **Then** the notification is automatically marked as read

3. **Given** I have marked a notification as read, **When** I filter by "Read", **Then** I can see that notification in the list

---

### User Story 4 - Mark Notification as Unread (Priority: P3)

As a staff user, I want to mark a read notification as unread so that I can flag it for follow-up or indicate I haven't fully addressed it yet.

**Why this priority**: This is an undo/reversal feature that provides flexibility but is not essential for core notification management.

**Independent Test**: Can be tested by marking a read notification as unread and verifying it reappears in the Unread filter. Delivers value by enabling notification re-prioritization.

**Acceptance Scenarios**:

1. **Given** I am viewing a read notification, **When** I mark it as unread, **Then** the notification becomes visually unread and appears when filtering by "Unread"

2. **Given** I mark a notification as unread, **When** I look at the notification badge in the sidebar, **Then** the unread count increases by one

---

### User Story 5 - Mark All as Read (Priority: P3)

As a staff user, I want to mark all visible notifications as read in one action so that I can quickly clear my notification queue when I've reviewed them.

**Why this priority**: Bulk action is a convenience feature that saves time but isn't required for basic functionality.

**Independent Test**: Can be tested by clicking "Mark All as Read" and verifying all visible notifications become read. Delivers value by enabling efficient inbox zero workflows.

**Acceptance Scenarios**:

1. **Given** I have multiple unread notifications visible, **When** I click "Mark All as Read", **Then** all currently visible notifications are marked as read

2. **Given** I have filtered notifications by type, **When** I click "Mark All as Read", **Then** only the filtered notifications (currently visible) are marked as read, not all notifications

3. **Given** I mark all as read, **When** I check the sidebar badge, **Then** the unread count updates to reflect the remaining unread notifications (zero if no filters applied)

---

### User Story 6 - Sidebar Notification Badge (Priority: P1)

As a staff user, I want to see an unread notification count badge next to "My Notifications" in the left sidebar so that I'm always aware of pending notifications without navigating away from my current task.

**Why this priority**: The badge provides passive awareness - a key UX pattern for notifications. It's essential for the feature to be useful in daily workflows.

**Independent Test**: Can be tested by logging in with unread notifications and verifying the badge appears with the correct count. Delivers immediate visual feedback.

**Acceptance Scenarios**:

1. **Given** I am logged in and have 5 unread notifications, **When** I view the left sidebar, **Then** I see "My Notifications" with a badge showing "5"

2. **Given** I have 0 unread notifications, **When** I view the left sidebar, **Then** the badge is hidden (not showing "0")

3. **Given** I mark a notification as read while on another page, **When** the sidebar updates, **Then** the badge count decreases by one

4. **Given** I have 100+ unread notifications, **When** I view the sidebar, **Then** the badge displays "99+" to prevent UI overflow

---

### User Story 7 - All Notifications Recorded Regardless of Preferences (Priority: P1)

As a staff user, I want all my notifications to appear in My Notifications even if I've opted out of email or in-app delivery, so that I have a complete historical record of events relevant to me.

**Why this priority**: This is a core architectural requirement - notifications must be recorded independently of delivery preferences. Without this, the notification center would be incomplete.

**Independent Test**: Can be tested by disabling email/in-app for a notification type, triggering that notification, and verifying it still appears in My Notifications.

**Acceptance Scenarios**:

1. **Given** I have disabled email and in-app notifications for "Ticket Assigned" in my preferences, **When** a ticket is assigned to me, **Then** the notification still appears in My Notifications

2. **Given** I have all notification delivery preferences disabled, **When** I navigate to My Notifications, **Then** I still see all notifications that would have been sent to me

---

### Edge Cases

- What happens when a user has thousands of notifications? **Pagination is applied with a reasonable page size (e.g., 25-50 items per page) and smooth loading.**
- How does the system handle notification clicks for tickets that have been deleted? **The notification displays but indicates the ticket no longer exists; clicking does not navigate anywhere.**
- What happens if a notification type is deprecated or removed in the future? **Existing notifications of that type remain visible with their original type label; they can still be filtered using "All" or "Read/Unread" filters.**
- What happens when real-time notification arrives while viewing the page? **The notification list updates dynamically (if feasible) or shows a "New notifications available - refresh" indicator.**
- How are notifications handled for staff users who are deactivated? **Deactivated users cannot log in, so their notifications are inaccessible but preserved in the system.**

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST record a notification entry for each notification event regardless of the user's email/in-app delivery preferences
- **FR-002**: System MUST display a "My Notifications" link in the staff sidebar navigation, positioned directly below "Dashboard"
- **FR-003**: System MUST display an unread notification count badge next to "My Notifications" in the sidebar when the user has one or more unread notifications
- **FR-004**: System MUST hide the badge (not show "0") when the user has no unread notifications
- **FR-005**: System MUST display "99+" when unread count exceeds 99 to prevent UI overflow
- **FR-006**: System MUST display notifications sorted by created date in descending order (newest first) by default
- **FR-007**: System MUST allow users to toggle sort order between ascending and descending
- **FR-008**: System MUST default to showing only unread notifications
- **FR-009**: System MUST allow filtering by read status: All, Unread, Read
- **FR-010**: System MUST allow filtering by notification type (Ticket Assigned, Ticket Assigned to Team, Comment Added, Status Changed, Ticket Reopened, SLA Approaching, SLA Breached)
- **FR-011**: System MUST support combining read status and notification type filters
- **FR-012**: System MUST allow marking an individual notification as read
- **FR-013**: System MUST allow marking an individual notification as unread
- **FR-014**: System MUST allow marking all currently visible/filtered notifications as read
- **FR-015**: System MUST automatically mark a notification as read when the user clicks through to the related ticket
- **FR-016**: System MUST display each notification with: type label, summary message, timestamp, and related ticket reference (if applicable)
- **FR-017**: System MUST visually distinguish between read and unread notifications
- **FR-018**: System MUST update the sidebar badge count when notifications are marked as read or unread
- **FR-019**: System MUST paginate notifications when the list exceeds the page size
- **FR-020**: System MUST display an appropriate empty state message when no notifications match the current filter
- **FR-021**: System MUST make this feature available to all staff users (users who can access the Staff area)

### Key Entities *(include if feature involves data)*

- **Notification**: Represents a single notification event for a staff user. Contains the recipient user, notification type, read/unread status, timestamp, summary message, and optional reference to the related ticket.
- **User**: Extended to track notification-related settings. The existing `PlaySoundOnNotification` setting continues to apply to real-time in-app alerts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Staff users can access and view their notification history within 2 seconds of clicking "My Notifications"
- **SC-002**: Unread notification badge updates reflect the accurate count at all times
- **SC-003**: Staff users can mark any notification as read or unread in a single action
- **SC-004**: Staff users can filter notifications and see updated results within 1 second
- **SC-005**: 90% of staff users can successfully locate and use the notification center without guidance
- **SC-006**: All notification types (7 types) are properly recorded and filterable
- **SC-007**: Notifications are recorded regardless of user delivery preferences (100% capture rate)
- **SC-008**: System handles at least 10,000 notifications per user without performance degradation

## Assumptions

- **A-001**: The existing notification event types (7 types) from `NotificationEventType` will be used to categorize notifications
- **A-002**: The existing notification delivery system will be extended to also record notifications to the new storage, rather than being replaced
- **A-003**: Pagination will default to 25 notifications per page, which is a standard pattern for list views
- **A-004**: The notification summary message will be auto-generated based on the event type and ticket context
- **A-005**: Real-time badge updates will leverage the existing SignalR infrastructure
- **A-006**: The feature will be available to all users with access to the Staff area, requiring no additional permissions
- **A-007**: Notifications will be retained indefinitely unless a future retention policy is defined
