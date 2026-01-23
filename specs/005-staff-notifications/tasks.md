# Tasks: Staff Notifications Center

**Input**: Design documents from `/specs/005-staff-notifications/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Not explicitly requested in spec - omitted per task generation rules.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, etc.)
- Include exact file paths in descriptions

## User Story Mapping

| Story | Priority | Title | Depends On |
|-------|----------|-------|------------|
| US7 | P1 | All Notifications Recorded | Foundational |
| US1 | P1 | View Unread Notifications | US7 |
| US6 | P1 | Sidebar Notification Badge | US7 |
| US2 | P2 | Filter and Sort Notifications | US1 |
| US3 | P2 | Mark Notification as Read | US1 |
| US4 | P3 | Mark Notification as Unread | US3 |
| US5 | P3 | Mark All as Read | US3 |

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and folder structure for the new feature

- [x] T001 Create `src/App.Application/Notifications/` folder structure with `Commands/` and `Queries/` subdirectories
- [x] T002 Create `src/App.Web/Areas/Staff/Pages/Notifications/` folder for Razor Pages
- [x] T003 [P] Add Notifications route constants to `src/App.Web/Areas/Staff/Pages/Shared/RouteNames.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Database entity and infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Create Notification entity in `src/App.Domain/Entities/Notification.cs` per data-model.md
- [x] T005 [P] Create NotificationConfiguration in `src/App.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs`
- [x] T006 [P] Add `DbSet<Notification> Notifications` to `src/App.Application/Common/Interfaces/IAppDbContext.cs`
- [x] T007 [P] Add `DbSet<Notification> Notifications` property to `src/App.Infrastructure/Persistence/AppDbContext.cs`
- [x] T008 Generate EF Core migration `AddNotifications` in `src/App.Infrastructure/Migrations/`
- [x] T009 Create NotificationDto and NotificationListItemDto in `src/App.Application/Notifications/NotificationDto.cs`

**Checkpoint**: Database schema ready - user story implementation can now begin

---

## Phase 3: User Story 7 - All Notifications Recorded (Priority: P1) 🎯 MVP Foundation

**Goal**: Record all notification events to database regardless of user delivery preferences

**Independent Test**: Trigger any notification event and verify a record is created in the Notifications table, even if user has disabled email/in-app delivery

### Implementation for User Story 7

- [x] T010 [US7] Create RecordNotification command in `src/App.Application/Notifications/Commands/RecordNotification.cs` with Validator and Handler
- [x] T011 [US7] Add `RecordNotificationAsync` method to `src/App.Application/Common/Interfaces/IInAppNotificationService.cs` interface
- [x] T012 [US7] Implement `RecordNotificationAsync` in `src/App.Web/Services/InAppNotificationService.cs` to record before delivery
- [x] T013 [US7] Modify `SendToUserAsync` in `src/App.Web/Services/InAppNotificationService.cs` to call RecordNotification before SignalR delivery
- [x] T014 [US7] Modify `SendToUsersAsync` in `src/App.Web/Services/InAppNotificationService.cs` to call RecordNotification for each user before SignalR delivery

**Checkpoint**: All notifications are now being recorded to the database. Verify by triggering ticket assignment and checking database.

---

## Phase 4: User Story 1 - View Unread Notifications (Priority: P1) 🎯 MVP Core

**Goal**: Staff users can see all their unread notifications in a dedicated page, sorted newest first

**Independent Test**: Log in as staff user, navigate to My Notifications, verify unread notifications display correctly with type, message, timestamp, and ticket reference

### Implementation for User Story 1

- [x] T015 [US1] Create GetNotifications query in `src/App.Application/Notifications/Queries/GetNotifications.cs` with Handler (unread filter, pagination, ordering)
- [x] T016 [US1] Create Notifications Index PageModel in `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml.cs` with OnGet handler
- [x] T017 [US1] Create Notifications Index Razor Page in `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml` with notification list UI
- [x] T018 [US1] Add "My Notifications" link to staff sidebar in `src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml` (below Dashboard)
- [x] T019 [US1] Style notification list with `.staff-card`, `.staff-table` patterns in Index.cshtml
- [x] T020 [US1] Implement empty state message when no notifications match the filter in Index.cshtml
- [x] T021 [US1] Add pagination UI using existing Staff area pagination pattern in Index.cshtml

**Checkpoint**: Staff can view their unread notifications at /staff/notifications. List displays correctly with pagination.

---

## Phase 5: User Story 6 - Sidebar Notification Badge (Priority: P1) 🎯 MVP Complete

**Goal**: Display unread notification count badge next to "My Notifications" in the sidebar, with real-time updates

**Independent Test**: Log in with unread notifications, verify badge shows correct count; mark notification as read, verify badge decrements; verify badge hidden when count is 0

### Implementation for User Story 6

- [x] T022 [US6] Create GetUnreadNotificationCount query in `src/App.Application/Notifications/Queries/GetUnreadNotificationCount.cs` with Handler
- [x] T023 [US6] Create NotificationBadgeViewComponent in `src/App.Web/Areas/Staff/Pages/Shared/Components/NotificationBadge/` for server-side badge render (implemented inline in _Layout.cshtml)
- [x] T024 [US6] Integrate NotificationBadgeViewComponent into sidebar in `src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml`
- [x] T025 [US6] Add badge CSS styles (hidden at 0, show "99+" for 100+) to `src/App.Web/wwwroot/css/staff-layout.css`
- [x] T026 [US6] Add SignalR `ReceiveUnreadCountUpdate` event broadcast to `src/App.Web/Services/InAppNotificationService.cs` after recording notifications
- [x] T027 [US6] Add client-side JS for real-time badge updates in `src/App.Web/wwwroot/js/notifications.js`

**Checkpoint**: MVP Complete - Sidebar shows accurate unread count with real-time updates via SignalR.

---

## Phase 6: User Story 2 - Filter and Sort Notifications (Priority: P2)

**Goal**: Filter notifications by read status and notification type, toggle sort order

**Independent Test**: Apply filter by "Read" status, verify only read notifications show; filter by type "Ticket Assigned", verify only that type shows; change sort to ascending, verify oldest first

### Implementation for User Story 2

- [x] T028 [US2] Extend GetNotifications query in `src/App.Application/Notifications/Queries/GetNotifications.cs` with FilterStatus and FilterType parameters
- [x] T029 [US2] Add filter dropdowns (Unread/Read/All, notification types) to `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml`
- [x] T030 [US2] Add sort toggle button (asc/desc) to `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml`
- [x] T031 [US2] Update PageModel OnGet in `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml.cs` to accept filter/sort parameters
- [x] T032 [US2] Add "Clear filters" link that resets to defaults in `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml`
- [x] T033 [US2] Add visual distinction between read and unread notifications (opacity, checkmark icon) in Index.cshtml

**Checkpoint**: Users can filter by status (All/Unread/Read), filter by type, and toggle sort order. Filters persist in URL.

---

## Phase 7: User Story 3 - Mark Notification as Read (Priority: P2)

**Goal**: Mark individual notifications as read, auto-mark when clicking through to ticket

**Independent Test**: Click "mark as read" on unread notification, verify it moves to "Read" filter; click notification link, verify auto-marked as read

### Implementation for User Story 3

- [x] T034 [US3] Create MarkNotificationAsRead command in `src/App.Application/Notifications/Commands/MarkNotificationAsRead.cs` with Validator and Handler
- [x] T035 [US3] Add OnPostMarkAsRead handler to `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml.cs`
- [x] T036 [US3] Add "Mark as Read" button/action to each unread notification in `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml`
- [x] T037 [US3] Broadcast unread count update via SignalR after marking as read in MarkNotificationAsRead Handler
- [x] T038 [US3] Add click-through handler that marks as read when navigating to ticket URL in Index.cshtml.cs (OnPostNavigate handler)

**Checkpoint**: Users can mark notifications as read individually. Badge updates correctly.

---

## Phase 8: User Story 4 - Mark Notification as Unread (Priority: P3)

**Goal**: Mark a previously read notification as unread to flag for follow-up

**Independent Test**: View a read notification, click "mark as unread", verify it appears in Unread filter and badge increases

### Implementation for User Story 4

- [x] T039 [US4] Create MarkNotificationAsUnread command in `src/App.Application/Notifications/Commands/MarkNotificationAsUnread.cs` with Validator and Handler
- [x] T040 [US4] Add OnPostMarkAsUnread handler to `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml.cs`
- [x] T041 [US4] Add "Mark as Unread" button/action to each read notification in `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml`
- [x] T042 [US4] Broadcast unread count update via SignalR after marking as unread in MarkNotificationAsUnread Handler

**Checkpoint**: Users can toggle notification read status. Provides flexibility for follow-up flagging.

---

## Phase 9: User Story 5 - Mark All as Read (Priority: P3)

**Goal**: Mark all visible (filtered) notifications as read in one action

**Independent Test**: With 10 unread notifications showing, click "Mark All as Read", verify all 10 become read and badge updates

### Implementation for User Story 5

- [x] T043 [US5] Create MarkAllNotificationsAsRead command in `src/App.Application/Notifications/Commands/MarkAllNotificationsAsRead.cs` with Validator and Handler
- [x] T044 [US5] Add OnPostMarkAllAsRead handler to `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml.cs`
- [x] T045 [US5] Add "Mark All as Read" button to notification list header in `src/App.Web/Areas/Staff/Pages/Notifications/Index.cshtml`
- [x] T046 [US5] Ensure command respects current filter context (only marks visible notifications) in Handler
- [x] T047 [US5] Add success message showing count of marked notifications using SetSuccessMessage pattern
- [x] T048 [US5] Broadcast unread count update via SignalR after bulk marking in MarkAllNotificationsAsRead Handler

**Checkpoint**: Users can efficiently clear notification queue. Bulk action respects current filters.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final refinements and edge case handling

- [x] T049 [P] Handle deleted ticket references gracefully (show "Ticket deleted" message, disable navigation) in Index.cshtml
- [x] T050 [P] Add "New notifications available - refresh" banner when real-time notification arrives while viewing list
- [x] T051 [P] Add keyboard accessibility (Enter/Space to toggle read status) to notification list
- [ ] T052 Verify all notification types (7 types) are recording correctly by triggering each type
- [x] T053 [P] Add relative timestamps ("5 minutes ago") using existing OrganizationTimeZoneConverter pattern
- [ ] T054 Run quickstart.md manual testing checklist to validate all acceptance scenarios
- [ ] T055 Performance test: Verify page loads in <2 seconds with 10,000+ notifications per user

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **US7 (Phase 3)**: Depends on Foundational - Recording infrastructure
- **US1 (Phase 4)**: Depends on US7 - Core list view
- **US6 (Phase 5)**: Depends on US7 - Sidebar badge
- **US2 (Phase 6)**: Depends on US1 - Filtering extends list view
- **US3 (Phase 7)**: Depends on US1 - Read marking extends list
- **US4 (Phase 8)**: Depends on US3 - Unread is reverse of read
- **US5 (Phase 9)**: Depends on US3 - Bulk is extension of single
- **Polish (Phase 10)**: Depends on all user stories

### User Story Dependencies

```
Foundational
     │
     ▼
    US7 (Record All Notifications) ─────────┐
     │                                       │
     ├──────────────┐                        │
     ▼              ▼                        │
    US1 ◄────────► US6                       │
  (View)         (Badge)                     │
     │                                       │
     ├──────────────┤                        │
     ▼              ▼                        │
    US2            US3                       │
 (Filter)        (Read)                      │
                   │                         │
          ┌───────┴───────┐                  │
          ▼               ▼                  │
         US4             US5                 │
       (Unread)      (Mark All)              │
                                             │
     All stories depend on ──────────────────┘
```

### Parallel Opportunities

**Phase 2 (Foundational)** - Can run in parallel:
- T005, T006, T007 (different files)

**Phase 5 (US6)** - After US7:
- T022, T023 can start in parallel with US1 tasks

**Phase 6-9** - After US1 complete:
- US2, US3 can start in parallel (different commands)

**Phase 10 (Polish)** - Can run in parallel:
- T049, T050, T051, T053 (independent enhancements)

---

## Parallel Example: Foundational Phase

```bash
# Launch all foundational tasks marked [P] together:
Task: "Create NotificationConfiguration in src/App.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs"
Task: "Add DbSet<Notification> to IAppDbContext.cs"
Task: "Add DbSet<Notification> to AppDbContext.cs"
```

## Parallel Example: After Foundational

```bash
# US7 must complete first, then US1 and US6 can run in parallel:
# Stream 1: Core list view (US1)
Task: "Create GetNotifications query"
Task: "Create Index PageModel"
Task: "Create Index Razor Page"

# Stream 2: Sidebar badge (US6)
Task: "Create GetUnreadNotificationCount query"
Task: "Create NotificationBadgeViewComponent"
```

---

## Implementation Strategy

### MVP First (P1 Stories Only)

1. Complete Phase 1: Setup ✓
2. Complete Phase 2: Foundational ✓
3. Complete Phase 3: US7 (Recording) ✓
4. Complete Phase 4: US1 (View Notifications) ✓
5. Complete Phase 5: US6 (Sidebar Badge) ✓
6. **STOP and VALIDATE**: Test MVP independently
7. Deploy/demo - users can now see their notification history

### Incremental Delivery

| Increment | Phases | Value Delivered |
|-----------|--------|-----------------|
| MVP | 1-5 (US7, US1, US6) | View notifications + badge |
| +Filtering | 6 (US2) | Search and organize |
| +Read | 7 (US3) | Track reviewed items |
| +Unread | 8 (US4) | Flag for follow-up |
| +Bulk | 9 (US5) | Inbox zero workflow |
| +Polish | 10 | Edge cases, accessibility |

---

## Task Summary

| Phase | Story | Task Count | Parallelizable |
|-------|-------|------------|----------------|
| Setup | - | 3 | 1 |
| Foundational | - | 6 | 4 |
| US7 | P1 | 5 | 0 |
| US1 | P1 | 7 | 0 |
| US6 | P1 | 6 | 0 |
| US2 | P2 | 6 | 0 |
| US3 | P2 | 5 | 0 |
| US4 | P3 | 4 | 0 |
| US5 | P3 | 6 | 0 |
| Polish | - | 7 | 5 |
| **Total** | | **55** | **10** |

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- MVP (US7 + US1 + US6) delivers core value with ~21 tasks

