# Implementation Plan: Staff Notifications Center

**Branch**: `005-staff-notifications` | **Date**: 2026-01-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-staff-notifications/spec.md`

## Summary

This feature adds a comprehensive notification center for staff users, accessible via "My Notifications" in the left sidebar. It persists all notification events (regardless of user delivery preferences), displays them with filtering (read/unread, notification type) and sorting (ascending/descending by date), and allows marking notifications as read/unread individually or in bulk. A badge in the sidebar shows the unread count.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core Razor Pages, MediatR (CQRS), FluentValidation, Entity Framework Core, SignalR  
**Storage**: PostgreSQL with EF Core migrations  
**Testing**: xUnit, FluentAssertions (existing test infrastructure)  
**Target Platform**: Linux server (Railway deployment)  
**Project Type**: Web application with Clean Architecture layers  
**Performance Goals**: Page load <2 seconds; filtering <1 second; unread count queries <50ms  
**Constraints**: No JavaScript frameworks; vanilla JS for progressive enhancement only; leverage existing SignalR infrastructure  
**Scale/Scope**: 10,000+ notifications per user; 7 notification types

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance | Notes |
|-----------|------------|-------|
| Clean Architecture & Dependency Rule | ✅ Pass | New entity in App.Domain; commands/queries in App.Application; UI in App.Web |
| CQRS & Mediator-Driven Use Cases | ✅ Pass | New queries (GetNotifications, GetUnreadCount) and commands (MarkAsRead, MarkAsUnread, MarkAllAsRead) |
| Razor Pages First, Minimal JavaScript | ✅ Pass | Server-rendered list with JS for real-time badge updates only |
| Explicit Data Access & Performance | ✅ Pass | Indexed queries with AsNoTracking, proper pagination |
| Security, Testing & Observability | ✅ Pass | FluentValidation, user-scoped queries, audit logging |
| BuiltIn Value Objects Pattern | ✅ Pass | Uses existing NotificationEventType value object |
| Staff Area UI Pattern | ✅ Pass | Uses `.staff-card`, `.staff-table`, `.staff-badge` patterns |
| GUID vs ShortGuid Pattern | ✅ Pass | Domain uses Guid, DTOs use ShortGuid |
| Alert/Message Display Pattern | ✅ Pass | Uses existing SetSuccessMessage/SetErrorMessage |
| Route Constants | ✅ Pass | Will add RouteNames.Notifications for new page |

**Gate Status**: ✅ PASS - No violations

## Project Structure

### Documentation (this feature)

```text
specs/005-staff-notifications/
├── plan.md              # This file
├── research.md          # Phase 0 output - technical decisions
├── data-model.md        # Phase 1 output - entity design
├── quickstart.md        # Phase 1 output - developer guide
├── contracts/           # Phase 1 output - API contracts
│   └── api.md           # Command/Query definitions
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── App.Domain/
│   └── Entities/
│       └── Notification.cs              # NEW: Notification entity
│
├── App.Application/
│   ├── Common/
│   │   └── Interfaces/
│   │       └── IInAppNotificationService.cs  # MODIFY: Add RecordNotificationAsync
│   │
│   └── Notifications/                   # NEW: Feature folder
│       ├── NotificationDto.cs           # NEW: DTO for notification
│       ├── Commands/
│       │   ├── MarkNotificationAsRead.cs     # NEW
│       │   ├── MarkNotificationAsUnread.cs   # NEW
│       │   ├── MarkAllNotificationsAsRead.cs # NEW
│       │   └── RecordNotification.cs         # NEW: Called by event handlers
│       └── Queries/
│           ├── GetNotifications.cs           # NEW: Paginated list with filters
│           └── GetUnreadNotificationCount.cs # NEW: For sidebar badge
│
├── App.Infrastructure/
│   ├── Persistence/
│   │   └── Configurations/
│   │       └── NotificationConfiguration.cs  # NEW: EF Core config
│   └── Migrations/
│       └── YYYYMMDD_AddNotifications.cs      # NEW: Database migration
│
└── App.Web/
    ├── Services/
    │   └── InAppNotificationService.cs   # MODIFY: Implement RecordNotificationAsync
    │
    └── Areas/Staff/Pages/
        ├── Shared/
        │   ├── RouteNames.cs             # MODIFY: Add Notifications routes
        │   └── _Layout.cshtml            # MODIFY: Add My Notifications link with badge
        │
        └── Notifications/                # NEW
            ├── Index.cshtml              # NEW: Notification list page
            └── Index.cshtml.cs           # NEW: Page model
```

**Structure Decision**: Follows existing Clean Architecture patterns. New `Notifications` feature folder in Application layer with Commands and Queries subfolders. New Razor Page under Staff area.

## Design Decisions

### Notification Recording Architecture

Notifications are recorded at the point of delivery (in `InAppNotificationService`) rather than in individual event handlers. This:
1. Centralizes recording logic
2. Ensures all notifications (regardless of delivery preferences) are captured
3. Minimizes changes to existing event handlers

### Sidebar Badge Update Strategy

**Option 1 (Selected)**: Server-side render with SignalR real-time updates
- Badge count rendered server-side on page load
- SignalR pushes updates when notifications change
- Uses existing `NotificationHub` infrastructure

**Option 2 (Rejected)**: Client-side polling
- More network traffic
- Less responsive

### Filter Default Behavior

- Default filter: Unread only
- Default sort: CreatedAt descending (newest first)
- Filters persist in URL query parameters for shareability

### Mark All As Read Scope

"Mark All As Read" operates on the current filter context, not all notifications globally. This matches user expectations when working with filtered views.

## Complexity Tracking

> No violations to justify - design follows existing patterns.

## Phase 1 Deliverables

- [x] plan.md (this file)
- [x] research.md
- [x] data-model.md
- [x] contracts/api.md
- [x] quickstart.md
