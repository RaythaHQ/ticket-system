# Tasks: SLA Extension Controls

**Input**: Design documents from `/specs/004-sla-extension-controls/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api.md ✅

**Tests**: Not explicitly requested in feature specification. Tests are documented in quickstart.md for reference but not included as tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Database changes and configuration models that all user stories depend on

- [x] T001 Add `SlaExtensionCount` property to Ticket entity in `src/App.Domain/Entities/Ticket.cs`
- [x] T002 Create database migration for SlaExtensionCount column in `src/App.Infrastructure/Migrations/`
- [x] T003 [P] Create `SlaExtensionSettings` configuration model in `src/App.Application/Common/Models/SlaExtensionSettings.cs`
- [x] T004 Apply database migration and verify column exists

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Add `SlaExtensionCount` property to `TicketDto` in `src/App.Application/Tickets/TicketDto.cs`
- [x] T006 Update `TicketDto.GetProjection()` to include SlaExtensionCount mapping in `src/App.Application/Tickets/TicketDto.cs`
- [x] T007 Add `CalculateDefaultExtensionHours()` method signature to `ISlaService` interface in `src/App.Application/Common/Interfaces/ISlaService.cs`
- [x] T008 Add `CalculateExtendedDueDate()` method signature to `ISlaService` interface in `src/App.Application/Common/Interfaces/ISlaService.cs`
- [x] T009 Implement `CalculateDefaultExtensionHours()` with business day logic in `src/App.Application/SlaRules/Services/SlaService.cs`
- [x] T010 Implement `CalculateExtendedDueDate()` in `src/App.Application/SlaRules/Services/SlaService.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Extend SLA by Hours (Priority: P1) 🎯 MVP

**Goal**: Staff can extend a ticket's SLA due date by a specified number of hours with smart defaults and live preview

**Independent Test**: Open a ticket with an SLA, click "Extend," enter hours, verify due date moves forward by that amount and change log entry is created

### Implementation for User Story 1

- [x] T011 [US1] Create `ExtendTicketSla.Command` with Id and ExtensionHours properties in `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`
- [x] T012 [US1] Create `ExtendTicketSla.Validator` with basic validation (hours > 0, ticket exists, not closed) in `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`
- [x] T013 [US1] Create `ExtendTicketSla.Handler` that updates SlaDueAt, increments SlaExtensionCount, creates change log in `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`
- [x] T014 [US1] Add `OnPostExtendSla` handler to page model in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs`
- [x] T015 [US1] Add `OnGetPreviewSlaExtension` handler for live preview API in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs`
- [x] T016 [US1] Add `SlaExtensionInfo` property to Details page model for extension state in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs`
- [x] T017 [US1] Update SLA card with inline extension UI (collapsed/expanded states) in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T018 [US1] Add extension form with hours input, default value, and Apply button in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T019 [US1] Add JavaScript for live preview with debounced API calls in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T020 [US1] Add CSS styles for inline extension UI states in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`

**Checkpoint**: User Story 1 complete - staff can extend SLA by hours with live preview. Basic functionality works for all users without limits.

---

## Phase 4: User Story 2 - Permission-Based Extension Limits (Priority: P2)

**Goal**: Non-privileged users are restricted to configurable extension count and hours limits, while privileged users have unlimited access

**Independent Test**: Log in as user without "Manage Tickets" permission, extend SLA once, verify second attempt is blocked

### Implementation for User Story 2

- [x] T021 [US2] Update `ExtendTicketSla.Validator` to check extension count limit for non-privileged users in `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`
- [x] T022 [US2] Update `ExtendTicketSla.Validator` to check max hours limit for non-privileged users in `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`
- [x] T023 [US2] Update `ExtendTicketSla.Handler` to bypass limits for users with CanManageTickets permission in `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`
- [x] T024 [US2] Update inline extension UI to disable form when limit reached for non-privileged users in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T025 [US2] Add limit-reached explanation message to UI in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T026 [US2] Add max hours enforcement in JavaScript preview validation in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`

**Checkpoint**: User Story 2 complete - extension limits enforced for non-privileged users, privileged users unlimited

---

## Phase 5: User Story 3 - Extension Status Display (Priority: P3)

**Goal**: UI clearly shows extension count, remaining extensions, and extension capability based on permissions

**Independent Test**: View a ticket that has been extended once and verify UI shows "1 of 1 extensions used"

### Implementation for User Story 3

- [x] T027 [P] [US3] Create `GetSlaExtensionInfo.Query` with TicketId property in `src/App.Application/Tickets/Queries/GetSlaExtensionInfo.cs`
- [x] T028 [P] [US3] Create `SlaExtensionInfoDto` record with all extension state fields in `src/App.Application/Tickets/Queries/GetSlaExtensionInfo.cs`
- [x] T029 [US3] Create `GetSlaExtensionInfo.Handler` that returns extension state and capabilities in `src/App.Application/Tickets/Queries/GetSlaExtensionInfo.cs`
- [x] T030 [US3] Update Details page to call GetSlaExtensionInfo query and populate SlaExtensionInfo property in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs`
- [x] T031 [US3] Add extension status badge showing "X of Y extensions used" in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T032 [US3] Add "Unlimited" badge for users with Manage Tickets permission in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T033 [US3] Add tooltip/explanation when extend option is disabled in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`

**Checkpoint**: User Story 3 complete - all extension state visible in UI

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, refinements, and validation

- [x] T034 Handle edge case: ticket with no SLA rule assigned (allow extension to create ad-hoc due date) in `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`
- [x] T035 Handle edge case: SLA already breached (allow extension, update status to ON_TRACK) in `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`
- [x] T036 Update SLA card to show SLA options even when no SLA rule assigned in `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml`
- [x] T037 Validate extension behavior when organization timezone is null (fallback to UTC) in `src/App.Application/SlaRules/Services/SlaService.cs`
- [x] T038 Run quickstart.md manual testing checklist validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on T001 (entity change) - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion
- **User Story 2 (Phase 4)**: Depends on User Story 1 completion (extends its command)
- **User Story 3 (Phase 5)**: Depends on Foundational completion (can run parallel to US1/US2)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 1: Setup ─────────────────────────────────────────────┐
         │                                                   │
         ▼                                                   │
Phase 2: Foundational ──────────────────────────────────────┤
         │                                                   │
         ├──────────────────────────┬───────────────────────┤
         ▼                          │                       │
Phase 3: User Story 1 (P1) 🎯 MVP   │                       │
         │                          ▼                       │
         ▼                   Phase 5: User Story 3 (P3)     │
Phase 4: User Story 2 (P2)          │                       │
         │                          │                       │
         └──────────────────────────┴───────────────────────┤
                                                            ▼
                                              Phase 6: Polish
```

- **User Story 1 (P1)**: Foundation → US1 (core extension)
- **User Story 2 (P2)**: Foundation → US1 → US2 (adds limits to US1's command)
- **User Story 3 (P3)**: Foundation → US3 (can run parallel to US1/US2, different files)

### Parallel Opportunities

**Within Phase 1 (Setup)**:
- T003 can run parallel to T001, T002

**Within Phase 2 (Foundational)**:
- T005, T006 (TicketDto) can run parallel to T007, T008, T009, T010 (ISlaService)

**Within Phase 3 (US1)**:
- T011, T012, T013 (command) can run parallel to T017, T018, T19, T020 (UI) initially, but handlers (T014-T016) need command first

**Within Phase 5 (US3)**:
- T027, T028 can run in parallel (Query and DTO setup)

**Cross-Phase Parallel**:
- Phase 5 (US3) can run in parallel with Phase 3 (US1) and Phase 4 (US2) since they touch different files
- US3 creates new query file, doesn't modify ExtendTicketSla command

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Parallel Group A: TicketDto changes
Task T005: Add SlaExtensionCount to TicketDto
Task T006: Update TicketDto.GetProjection()

# Parallel Group B: ISlaService changes (can run same time as Group A)
Task T007: Add CalculateDefaultExtensionHours signature
Task T008: Add CalculateExtendedDueDate signature
Task T009: Implement CalculateDefaultExtensionHours
Task T010: Implement CalculateExtendedDueDate
```

---

## Parallel Example: Phase 3 (User Story 1)

```bash
# Sequential: Command implementation first
Task T011: Create ExtendTicketSla.Command
Task T012: Create ExtendTicketSla.Validator
Task T013: Create ExtendTicketSla.Handler

# Then parallel: Page handlers and UI
Task T014: OnPostExtendSla handler
Task T015: OnGetPreviewSlaExtension handler
Task T016: SlaExtensionInfo property

# Parallel: UI components (can start after T016)
Task T017: Inline extension UI structure
Task T018: Extension form
Task T019: JavaScript for live preview
Task T020: CSS styles
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T010)
3. Complete Phase 3: User Story 1 (T011-T020)
4. **STOP and VALIDATE**: Test basic extension functionality
5. Deploy/demo if ready - core value delivered!

### Incremental Delivery

1. **MVP Milestone**: Setup + Foundational + US1 = Basic extension works
2. **Limit Control Milestone**: + US2 = Permission-based limits enforced
3. **Full UX Milestone**: + US3 = Complete status visibility
4. **Production Ready**: + Polish = Edge cases handled

### Parallel Team Strategy

With 2 developers after Foundational phase:
- **Developer A**: User Story 1 → User Story 2 (command-focused)
- **Developer B**: User Story 3 (query + UI status) → Polish

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- US2 depends on US1 because it modifies the same command
- US3 is independent and can run in parallel with US1/US2
- Entity change (T001) must complete before any other work
- All handlers need the command (T013) to exist first
- JavaScript preview (T019) needs the preview handler (T015) first

