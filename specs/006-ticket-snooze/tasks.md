# Tasks: Ticket Snooze

**Input**: Design documents from `/specs/006-ticket-snooze/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md

**Tests**: Tests are NOT explicitly requested in the specification. Test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Domain**: `src/App.Domain/`
- **Application**: `src/App.Application/`
- **Infrastructure**: `src/App.Infrastructure/`
- **Web**: `src/App.Web/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, domain entities, and database schema

- [x] T001 Add snooze fields to Ticket entity in `src/App.Domain/Entities/Ticket.cs` (SnoozedUntil, SnoozedAt, SnoozedById, SnoozedBy, SnoozedReason, UnsnoozedAt)
- [x] T002 Add IsSnoozed and IsRecentlyUnsnoozed computed properties to Ticket entity in `src/App.Domain/Entities/Ticket.cs`
- [x] T003 [P] Create TicketSnoozedEvent domain event in `src/App.Domain/Events/TicketSnoozedEvent.cs`
- [x] T004 [P] Create TicketUnsnoozedEvent domain event in `src/App.Domain/Events/TicketUnsnoozedEvent.cs`
- [x] T005 Update TicketConfiguration with snooze fields and partial index in `src/App.Infrastructure/Persistence/Configurations/TicketConfiguration.cs`
- [x] T006 Create EF Core migration for snooze schema changes (run `dotnet ef migrations add AddTicketSnooze`)
- [x] T007 [P] Create SnoozeConfiguration for env var settings in `src/App.Infrastructure/Configurations/SnoozeConfiguration.cs`
- [x] T008 [P] Create ISnoozeConfiguration interface in `src/App.Application/Common/Interfaces/ISnoozeConfiguration.cs`
- [x] T009 Register SnoozeConfiguration in `src/App.Infrastructure/ConfigureServices.cs`

**Checkpoint**: Database schema ready, domain entities prepared for snooze operations

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T010 Add snooze fields to TicketDto in `src/App.Application/Tickets/TicketDto.cs` (IsSnoozed, SnoozedUntil, SnoozedAt, SnoozedById, SnoozedByName, SnoozedReason, IsRecentlyUnsnoozed, UnsnoozedAt, CanSnooze, CannotSnoozeReason)
- [x] T011 Add snooze fields to TicketListItemDto in `src/App.Application/Tickets/TicketListItemDto.cs` (IsSnoozed, SnoozedUntil, IsRecentlyUnsnoozed)
- [x] T012 Update ticket query projections to include snooze fields in `src/App.Application/Tickets/Queries/GetTicket.cs`
- [x] T013 Update ticket list query projections to include snooze fields in `src/App.Application/Tickets/Queries/GetTickets.cs`
- [x] T014 [P] Create SnoozeEvaluationJob background service in `src/App.Infrastructure/BackgroundTasks/SnoozeEvaluationJob.cs`
- [x] T015 Register SnoozeEvaluationJob as hosted service in `src/App.Infrastructure/ConfigureServices.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Snooze Ticket Until Specific Time (Priority: P1) 🎯 MVP

**Goal**: Staff can snooze a ticket until a specific date/time, ticket auto-unsnoozes at scheduled time

**Independent Test**: Snooze a ticket with a future date, verify it's hidden from active queue, confirm it reappears at scheduled time

### Implementation for User Story 1

- [x] T016 [US1] Create SnoozeTicket command record in `src/App.Application/Tickets/Commands/SnoozeTicket.cs`
- [x] T017 [US1] Create SnoozeTicketResponseDto in `src/App.Application/Tickets/Commands/SnoozeTicket.cs`
- [x] T018 [US1] Implement SnoozeTicket.Validator with all validation rules (ticket exists, not closed, has assignee, future date, max duration) in `src/App.Application/Tickets/Commands/SnoozeTicket.cs`
- [x] T019 [US1] Implement SnoozeTicket.Handler (set snooze fields, add changelog entry, raise TicketSnoozedEvent) in `src/App.Application/Tickets/Commands/SnoozeTicket.cs`
- [x] T020 [US1] Implement auto-unsnooze logic in SnoozeEvaluationJob (query due tickets, clear snooze fields, set UnsnoozedAt, raise TicketUnsnoozedEvent) in `src/App.Infrastructure/BackgroundTasks/SnoozeEvaluationJob.cs`
- [x] T021 [US1] Add snooze button and modal trigger to ticket detail page in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T022 [US1] Create snooze modal partial with custom datetime picker in `src/App.Web/Areas/Staff/Pages/Tickets/_SnoozeModal.cshtml`
- [x] T023 [US1] Add OnPostSnoozeAsync handler to ticket detail page model in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs`
- [x] T024 [US1] Add snooze indicator (badge with unsnooze time) to ticket list view in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml`
- [x] T025 [US1] Add "recently unsnoozed" visual indicator styling in `src/App.Web/wwwroot/css/staff-layout.css`

**Checkpoint**: User Story 1 complete - Staff can snooze tickets and they auto-unsnooze at scheduled time

---

## Phase 4: User Story 2 - Quick Snooze Presets (Priority: P2)

**Goal**: Staff can snooze tickets with one-click presets (Later Today, Tomorrow, In 3 Days, Next Week)

**Independent Test**: Snooze a ticket using each preset and verify correct unsnooze time is calculated

### Implementation for User Story 2

- [x] T026 [P] [US2] Create GetSnoozePresets query record in `src/App.Application/Tickets/Queries/GetSnoozePresets.cs` (implemented inline in modal)
- [x] T027 [P] [US2] Create SnoozePresetsDto and SnoozePresetDto in `src/App.Application/Tickets/Queries/GetSnoozePresets.cs` (implemented inline in modal)
- [x] T028 [US2] Implement GetSnoozePresets.Handler with preset calculation logic (Later Today, Tomorrow, In 3 Days, Next Week) in `src/App.Application/Tickets/Queries/GetSnoozePresets.cs` (implemented inline in modal)
- [x] T029 [US2] Add preset buttons to snooze modal in `src/App.Web/Areas/Staff/Pages/Tickets/_SnoozeModal.cshtml`
- [x] T030 [US2] Load presets via GetSnoozePresets query in ticket detail page model in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs` (implemented inline in modal)

**Checkpoint**: User Story 2 complete - Staff can quickly snooze tickets using presets

---

## Phase 5: User Story 3 - Manual Unsnooze (Priority: P3)

**Goal**: Staff can manually unsnooze a ticket before its scheduled time

**Independent Test**: Snooze a ticket, click unsnooze, verify ticket returns to active queue

### Implementation for User Story 3

- [x] T031 [US3] Create UnsnoozeTicket command record in `src/App.Application/Tickets/Commands/UnsnoozeTicket.cs`
- [x] T032 [US3] Create UnsnoozeTicketResponseDto in `src/App.Application/Tickets/Commands/UnsnoozeTicket.cs`
- [x] T033 [US3] Implement UnsnoozeTicket.Validator (ticket exists, is snoozed) in `src/App.Application/Tickets/Commands/UnsnoozeTicket.cs`
- [x] T034 [US3] Implement UnsnoozeTicket.Handler (clear snooze fields, set UnsnoozedAt, add changelog entry, raise TicketUnsnoozedEvent) in `src/App.Application/Tickets/Commands/UnsnoozeTicket.cs`
- [x] T035 [US3] Add unsnooze button to ticket detail page (visible when snoozed) in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T036 [US3] Add OnPostUnsnoozeAsync handler to ticket detail page model in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs`
- [x] T037 [US3] Add unsnooze button to ticket list snooze indicator in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml`

**Checkpoint**: User Story 3 complete - Staff can manually unsnooze tickets

---

## Phase 6: User Story 6 - Snooze Assignment Constraints (Priority: P3)

**Goal**: Enforce that snoozed tickets always have an individual assignee

**Independent Test**: Try to unassign or team-assign a snoozed ticket and verify it auto-unsnoozes first

### Implementation for User Story 6

- [x] T038 [US6] Add auto-unsnooze logic to AssignTicket.Handler (unsnooze if new assignee is null or team-only) in `src/App.Application/Tickets/Commands/AssignTicket.cs`
- [x] T039 [US6] Add auto-unsnooze logic to UpdateTicket.Handler for assignee changes in `src/App.Application/Tickets/Commands/UpdateTicket.cs`
- [x] T040 [US6] Add validation to SnoozeTicket.Validator to reject snooze if ticket has no individual assignee in `src/App.Application/Tickets/Commands/SnoozeTicket.cs`
- [x] T041 [US6] Add CannotSnoozeReason logic to ticket DTO mapping for UI feedback in `src/App.Application/Tickets/TicketDto.cs`
- [x] T042 [US6] Display CannotSnoozeReason message in snooze modal when snooze is blocked in `src/App.Web/Areas/Staff/Pages/Tickets/_SnoozeModal.cshtml`

**Checkpoint**: User Story 6 complete - Snoozed tickets always have accountable assignee

---

## Phase 7: User Story 7 - Snoozed Ticket Filtering in Views (Priority: P3)

**Goal**: Staff can control whether snoozed tickets appear in their views

**Independent Test**: Toggle "Show snoozed" checkbox in views and verify correct ticket visibility

### Implementation for User Story 7

- [x] T043 [US7] Update GetBuiltInViewConditionsAsync to add IsSnoozed=false filter to all built-in views except "All Tickets" in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs`
- [x] T044 [US7] Add "snoozed" built-in view key with IsSnoozed=true condition in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs`
- [x] T045 [US7] Add "Show snoozed" checkbox filter to ticket list view header in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml`
- [x] T046 [US7] Add snooze filter dropdown to "All Tickets" view (All, Only snoozed, Exclude snoozed) in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml`
- [x] T047 [US7] Handle showSnoozed and snoozeFilter query parameters in ticket list page model in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs`
- [x] T048 [US7] Add "Snoozed" to built-in views navigation sidebar in `src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml`

**Checkpoint**: User Story 7 complete - Staff can filter snoozed tickets in views

---

## Phase 8: User Story 4 - SLA Pause During Snooze (Priority: P4)

**Goal**: SLA timers pause while a ticket is snoozed (configurable)

**Independent Test**: Snooze a ticket with SLA, let time pass, unsnooze, verify SLA due date extended

### Implementation for User Story 4

- [x] T049 [US4] Add PauseSlaOnSnooze field to OrganizationSettings entity in `src/App.Domain/Entities/OrganizationSettings.cs`
- [x] T050 [US4] Add migration for PauseSlaOnSnooze column in `src/App.Infrastructure/Migrations/20260130195748_AddPauseSlaOnSnooze.cs`
- [x] T051 [US4] Update UnsnoozeTicket.Handler to extend SlaDueAt by snooze duration when PauseSlaOnSnooze is true in `src/App.Application/Tickets/Commands/UnsnoozeTicket.cs`
- [x] T052 [US4] Update SnoozeEvaluationJob to extend SlaDueAt on auto-unsnooze in `src/App.Infrastructure/BackgroundTasks/SnoozeEvaluationJob.cs`
- [x] T053 [US4] Display "Paused" SLA status indicator for snoozed tickets in ticket detail in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T054 [US4] Add PauseSlaOnSnooze toggle to organization settings admin page in `src/App.Web/Areas/Admin/Pages/Configuration/Index.cshtml`
- [x] T055 [US4] Add PauseSlaOnSnooze to EditConfiguration command in `src/App.Application/OrganizationSettings/Commands/EditConfiguration.cs`

**Checkpoint**: User Story 4 complete - SLA pauses during snooze (configurable)

---

## Phase 9: User Story 5 - Snooze Notifications (Priority: P4)

**Goal**: Proper notification delivery on unsnooze (respecting actor/recipient rules)

**Independent Test**: Trigger various snooze/unsnooze scenarios and verify correct notification behavior

### Implementation for User Story 5

- [x] T056 [US5] Add TICKET_UNSNOOZED constant and TicketUnsnoozed property to NotificationEventType in `src/App.Domain/ValueObjects/NotificationEventType.cs`
- [x] T057 [US5] Update NotificationEventType.SupportedTypes to include TicketUnsnoozed in `src/App.Domain/ValueObjects/NotificationEventType.cs`
- [x] T058 [P] [US5] Add TicketUnsnoozedEmail property to BuiltInEmailTemplate in `src/App.Domain/Entities/EmailTemplate.cs`
- [x] T059 [P] [US5] Create TicketUnsnoozed email template content in `src/App.Domain/Entities/DefaultTemplates/email_ticket_unsnoozed.liquid`
- [x] T060 [US5] Create TicketUnsnoozedEventHandler_SendNotification event handler in `src/App.Application/Tickets/EventHandlers/TicketUnsnoozedEventHandler_SendNotification.cs`
- [x] T061 [US5] Implement notification logic: auto-unsnooze notifies assignee+followers; manual unsnooze notifies others (not actor) in `src/App.Application/Tickets/EventHandlers/TicketUnsnoozedEventHandler_SendNotification.cs`
- [ ] T062 [US5] Add data migration to enable snooze notifications for all existing users (both email and in-app)
- [x] T063 [US5] ticket_unsnoozed auto-discovered in notification preferences UI from NotificationEventType.SupportedTypes
- [x] T064 [US5] Skip snooze notification if ticket is closed (check in event handler) in `src/App.Application/Tickets/EventHandlers/TicketUnsnoozedEventHandler_SendNotification.cs`

**Checkpoint**: User Story 5 complete - Notifications sent on unsnooze respecting all rules

---

## Phase 10: User Story 8 - Is Snoozed View Condition (Priority: P4)

**Goal**: "Is Snoozed" available as filter condition for system and custom views

**Independent Test**: Create a custom view with "Is Snoozed = Yes" condition and verify only snoozed tickets appear

### Implementation for User Story 8

- [x] T065 [US8] Add IsSnoozed to FilterAttributes.All in `src/App.Application/TicketViews/FilterAttributeDefinition.cs`
- [x] T066 [US8] Add issnoozed case to ViewFilterBuilder.BuildFilterBody() switch statement in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T067 [US8] Verify IsSnoozed appears in admin system views condition dropdown in `src/App.Web/Areas/Admin/Pages/TicketViews/` (auto-discovered from FilterAttributes.All)
- [x] T068 [US8] Verify IsSnoozed appears in staff custom views condition dropdown in `src/App.Web/Areas/Staff/Pages/Tickets/Views/` (auto-discovered from FilterAttributes.All)

**Checkpoint**: User Story 8 complete - "Is Snoozed" condition available in all view builders

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T069 Snooze routes use page handlers (OnPostSnooze/OnPostUnsnooze) - no new route constants needed
- [x] T070 Add auto-cancel snooze when ticket status changes to closed/resolved in ChangeTicketStatus command in `src/App.Application/Tickets/Commands/ChangeTicketStatus.cs`
- [x] T071 Update .env.example with SNOOZE_MAX_DURATION_DAYS=90 in `.env.example`
- [x] T072 Add snooze columns (IsSnoozed, SnoozedUntil) to ColumnRegistry in `src/App.Application/TicketViews/ColumnDefinition.cs`
- [ ] T073 Run full validation of all acceptance scenarios from spec.md
- [ ] T074 Code cleanup and ensure consistent error messages for snooze operations

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-10)**: All depend on Foundational phase completion
  - User stories can proceed in priority order (P1 → P2 → P3 → P4)
  - Some P3 stories can run in parallel (US3, US6, US7)
  - P4 stories can run in parallel (US4, US5, US8)
- **Polish (Phase 11)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Priority | Can Start After | Notes |
|-------|----------|-----------------|-------|
| US1 | P1 | Phase 2 | MVP - Core snooze functionality |
| US2 | P2 | Phase 2 | Presets enhance US1 but independently testable |
| US3 | P3 | Phase 2 | Manual unsnooze |
| US6 | P3 | Phase 2 | Assignment constraints (can parallel with US3, US7) |
| US7 | P3 | Phase 2 | View filtering (can parallel with US3, US6) |
| US4 | P4 | US1, US3 | SLA pause requires snooze/unsnooze working |
| US5 | P4 | US1, US3 | Notifications require snooze/unsnooze events |
| US8 | P4 | Phase 2 | View condition (can parallel with US4, US5) |

### Within Each User Story

- Domain events before handlers
- Validators before handlers
- Handlers before UI
- Backend complete before frontend components

### Parallel Opportunities

- **Phase 1**: T003, T004, T007, T008 can run in parallel
- **Phase 2**: T014 can run in parallel with DTO updates
- **Phase 3 (US1)**: Backend (T016-T020) then UI (T021-T025)
- **Phase 4 (US2)**: T026, T027 can run in parallel
- **Phase 9 (US5)**: T058, T059 can run in parallel
- **P3 Stories**: US3, US6, US7 can be worked in parallel by different developers
- **P4 Stories**: US4, US5, US8 can be worked in parallel by different developers

---

## Parallel Example: User Story 1 (MVP)

```bash
# Phase 1 parallel tasks:
Task: "Create TicketSnoozedEvent in src/App.Domain/Events/TicketSnoozedEvent.cs"
Task: "Create TicketUnsnoozedEvent in src/App.Domain/Events/TicketUnsnoozedEvent.cs"
Task: "Create SnoozeConfiguration in src/App.Infrastructure/Configurations/SnoozeConfiguration.cs"
Task: "Create ISnoozeConfiguration in src/App.Application/Common/Interfaces/ISnoozeConfiguration.cs"

# Then after handlers complete, parallel UI tasks:
Task: "Add snooze button to ticket detail page in src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml"
Task: "Add snooze indicator to ticket list in src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test snooze/auto-unsnooze independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test → Deploy (MVP!)
3. Add User Story 2 → Test → Deploy (Presets)
4. Add User Stories 3, 6, 7 → Test → Deploy (Unsnooze + Constraints + Views)
5. Add User Stories 4, 5, 8 → Test → Deploy (SLA + Notifications + View Condition)
6. Polish phase → Final release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (MVP)
   - After MVP: Developer A: User Story 2 (Presets)
3. Once US1 done:
   - Developer A: User Story 3 (Unsnooze)
   - Developer B: User Story 6 (Assignment Constraints)
   - Developer C: User Story 7 (View Filtering)
4. Once core stories done:
   - Developer A: User Story 4 (SLA)
   - Developer B: User Story 5 (Notifications)
   - Developer C: User Story 8 (View Condition)

---

## Summary

| Metric | Value |
|--------|-------|
| **Total Tasks** | 74 |
| **Setup Phase** | 9 tasks |
| **Foundational Phase** | 6 tasks |
| **User Story 1 (P1)** | 10 tasks |
| **User Story 2 (P2)** | 5 tasks |
| **User Story 3 (P3)** | 7 tasks |
| **User Story 6 (P3)** | 5 tasks |
| **User Story 7 (P3)** | 6 tasks |
| **User Story 4 (P4)** | 7 tasks |
| **User Story 5 (P4)** | 9 tasks |
| **User Story 8 (P4)** | 4 tasks |
| **Polish Phase** | 6 tasks |
| **Parallel Opportunities** | 15+ tasks marked [P] |
| **MVP Scope** | Phases 1-3 (25 tasks) |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
