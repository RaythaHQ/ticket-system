# Tasks: Advanced Views

**Input**: Design documents from `/specs/003-advanced-views/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: Not explicitly requested in spec - test tasks omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Domain**: `src/App.Domain/`
- **Application**: `src/App.Application/`
- **Infrastructure**: `src/App.Infrastructure/`
- **Web**: `src/App.Web/`
- **Tests**: `tests/App.Application.UnitTests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Database migration and foundational data structures

- [x] T001 Add SortLevelsJson column to TicketView entity in `src/App.Domain/Entities/TicketView.cs`
- [x] T002 Add SortLevels computed property with JSON deserialization and legacy migration fallback in `src/App.Domain/Entities/TicketView.cs`
- [x] T003 Create EF Core migration for SortLevelsJson column in `src/App.Infrastructure/Migrations/`
- [ ] T004 Run migration to update database schema (requires database connection)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Create ViewSortLevel record in `src/App.Application/TicketViews/TicketViewDto.cs`
- [x] T006 Extend ViewFilterCondition record with DateType, RelativeDateUnit, RelativeDateValue, RelativeDatePreset properties in `src/App.Application/TicketViews/TicketViewDto.cs`
- [x] T007 [P] Create FilterAttributeDefinition.cs with attribute registry and operator definitions in `src/App.Application/TicketViews/FilterAttributeDefinition.cs`
- [x] T008 [P] Create OperatorDefinitions static class with StringOperators, DateOperators, BooleanOperators, NumericOperators, SelectionOperators, PriorityOperators, UserOperators in `src/App.Application/TicketViews/OperatorDefinitions.cs`
- [x] T009 [P] Create RelativeDatePresets static class in `src/App.Application/TicketViews/RelativeDatePresets.cs`
- [x] T010 [P] Create ColumnDefinition record for display columns in `src/App.Application/TicketViews/ColumnDefinition.cs`
- [x] T011 Update CreateTicketView.Command to accept SortLevels and enhanced Conditions in `src/App.Application/TicketViews/Commands/CreateTicketView.cs`
- [x] T012 Update UpdateTicketView.Command to accept SortLevels and enhanced Conditions in `src/App.Application/TicketViews/Commands/UpdateTicketView.cs`
- [x] T013 Update TicketViewDto to expose SortLevels property mapped from entity in `src/App.Application/TicketViews/TicketViewDto.cs`
- [x] T014 Add validation rules for ViewConditions (max 20 filters, valid Logic) to CreateTicketView.Validator in `src/App.Application/TicketViews/Commands/CreateTicketView.cs`
- [x] T015 Add validation rules for ViewConditions to UpdateTicketView.Validator in `src/App.Application/TicketViews/Commands/UpdateTicketView.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Advanced Filter Builder (Priority: P1) 🎯 MVP

**Goal**: Staff users can create views with complex filtering logic using AND/OR conditions, type-appropriate operators, and various attribute types including strings, dates, booleans, numerics, status/priority selection, and user selection.

**Independent Test**: Create a view with multiple filter conditions (e.g., "Status is Open AND Priority is higher than Normal AND Created At is within last 7 days"), save it, then navigate to the ticket list and verify only matching tickets appear.

### Backend Implementation for US1

- [x] T016 [US1] Implement string operators (eq, neq, contains, not_contains, starts_with, ends_with, is_empty, is_not_empty) in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T017 [US1] Implement date operators (is, is_within, is_before, is_after, is_on_or_before, is_on_or_after, is_empty, is_not_empty) in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T018 [US1] Implement relative date resolution (today, yesterday, this_week, last_week, this_month, last_month, days_ago, days_from_now) using organization timezone in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T019 [US1] Implement boolean operators (is_true, is_false) for SlaBreached, HasContact, HasAttachments in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T020 [US1] Implement numeric operators (eq, neq, gt, lt, gte, lte) for TicketId, ContactId in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T021 [US1] Implement selection operators (is, is_not, is_any_of, is_none_of) for Status with __OPEN__/__CLOSED__ meta-groups in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T022 [US1] Implement priority comparison operators (is, is_not, gt, lt, gte, lte) using TicketPriority.SortOrder in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T023 [US1] Implement user operators (is, is_not, is_any_of, is_none_of, is_empty, is_not_empty) for AssigneeId, CreatedByStaffId in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T024 [US1] Implement contact field filters (Contact.FirstName, Contact.LastName, Contact.EmailAddress, Contact.PhoneNumber, Contact.Organization) with string operators in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [x] T025 [US1] Implement AND/OR logic switching in ApplyFilters method using PredicateBuilder pattern in ViewFilterBuilder in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [ ] T026 [US1] Update GetTickets query handler to layer top-bar filters on top of view base filters using AND logic in `src/App.Application/Tickets/Queries/GetTickets.cs`

### UI Components for US1

- [x] T027 [P] [US1] Create filter-builder.js with add/remove condition logic and cascading selects in `src/App.Web/wwwroot/admin/js/shared/filter-builder.js`
- [x] T028 [US1] Create _FilterBuilder.cshtml partial with AND/OR toggle, condition rows, attribute/operator/value selects in `src/App.Web/Areas/Staff/Pages/Shared/_Partials/_FilterBuilder.cshtml`
- [x] T029 [US1] Add filter attribute data (operators, value types) as data attributes on attribute options in _FilterBuilder.cshtml
- [x] T030 [US1] Implement type-specific value inputs (text, date picker with flatpickr, dropdown, multi-select, user searchable dropdown) in _FilterBuilder.cshtml
- [x] T031 [US1] Add relative date preset dropdown that appears when "is within" operator is selected in _FilterBuilder.cshtml
- [x] T032 [US1] Add status options with "Open (all non-closed)" and "Closed (resolved/cancelled)" meta-groups in _FilterBuilder.cshtml
- [x] T033 [US1] Add user dropdown with staff members including suspended ones with "(deactivated)" suffix in _FilterBuilder.cshtml
- [x] T034 [US1] Update Staff Views/Create.cshtml to use _FilterBuilder partial replacing existing filter dropdowns in `src/App.Web/Areas/Staff/Pages/Views/Create.cshtml`
- [x] T035 [US1] Update Staff Views/Create.cshtml.cs to handle new Conditions form structure with array binding in `src/App.Web/Areas/Staff/Pages/Views/Create.cshtml.cs`
- [x] T036 [US1] Update Staff Views/Edit.cshtml to use _FilterBuilder partial in `src/App.Web/Areas/Staff/Pages/Views/Edit.cshtml`
- [x] T037 [US1] Update Staff Views/Edit.cshtml.cs to load existing conditions into _FilterBuilder in `src/App.Web/Areas/Staff/Pages/Views/Edit.cshtml.cs`
- [ ] T038 [US1] Add inline validation and error display for filter conditions in _FilterBuilder.cshtml
- [ ] T039 [US1] Include SortableJS in Staff area layout for drag-drop support in `src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml`

**Checkpoint**: User Story 1 (Advanced Filter Builder) is fully functional and testable independently

---

## Phase 4: User Story 2 - Multi-Level Sort Order (Priority: P2)

**Goal**: Users can configure multiple sort levels (e.g., "Priority desc, Created At asc") and the ticket list displays the view's sort as the first pill option.

**Independent Test**: Create a view with multi-level sorting, navigate to ticket list, verify tickets are sorted correctly and the view's sort appears as first pill. Click another sort pill to override, then click view's pill to restore.

### Backend Implementation for US2

- [x] T040 [US2] Update GetTickets query handler to apply multi-level sorting from view.SortLevels in `src/App.Application/Tickets/Queries/GetTickets.cs`
- [x] T041 [US2] Implement ThenBy/ThenByDescending chaining for multi-level sort in GetTickets in `src/App.Application/Tickets/Queries/GetTickets.cs`
- [ ] T042 [US2] Add sortBy=view query parameter handling to restore view's default sort in Tickets/Index.cshtml.cs in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs`

### UI Components for US2

- [x] T043 [P] [US2] Create sort-configurator.js with add/remove sort level and SortableJS initialization in `src/App.Web/wwwroot/admin/js/shared/sort-configurator.js`
- [x] T044 [US2] Create _SortConfigurator.cshtml partial with add sort level, field select, direction toggle, drag handles, remove button in `src/App.Web/Areas/Staff/Pages/Shared/_Partials/_SortConfigurator.cshtml`
- [x] T045 [US2] Add hidden order inputs updated on drag-drop reorder in _SortConfigurator.cshtml
- [x] T046 [US2] Update Staff Views/Create.cshtml to use _SortConfigurator partial replacing single sort dropdown in `src/App.Web/Areas/Staff/Pages/Views/Create.cshtml`
- [x] T047 [US2] Update Staff Views/Create.cshtml.cs to handle SortLevels array binding in `src/App.Web/Areas/Staff/Pages/Views/Create.cshtml.cs`
- [x] T048 [US2] Update Staff Views/Edit.cshtml to use _SortConfigurator partial in `src/App.Web/Areas/Staff/Pages/Views/Edit.cshtml`
- [x] T049 [US2] Update Staff Views/Edit.cshtml.cs to load existing SortLevels into _SortConfigurator in `src/App.Web/Areas/Staff/Pages/Views/Edit.cshtml.cs`
- [ ] T050 [US2] Update Tickets/Index.cshtml to show view's sort as first pill formatted as "Field1 ↓, Field2 ↑" in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml`
- [ ] T051 [US2] Add sortBy=view link generation for view's default sort pill in Tickets/Index.cshtml
- [ ] T052 [US2] Highlight active sort pill (view's sort vs override sorts) in Tickets/Index.cshtml

**Checkpoint**: User Story 2 (Multi-Level Sorting) is fully functional and testable independently

---

## Phase 5: User Story 3 - Column Selection and Ordering (Priority: P3)

**Goal**: Users can select which columns appear and drag-drop to reorder them. Search only searches visible columns. Clickable links for Ticket ID, Title, and Contact ID.

**Independent Test**: Create a view with specific columns in custom order, navigate to ticket list, verify only those columns appear in that order. Type in search box and verify search respects column selection.

### Backend Implementation for US3

- [ ] T053 [US3] Update ApplyColumnSearch in ViewFilterBuilder to use view's VisibleColumns parameter in `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs`
- [ ] T054 [US3] Update GetTickets to pass view.VisibleColumns to ApplyColumnSearch in `src/App.Application/Tickets/Queries/GetTickets.cs`
- [ ] T055 [US3] Add validation for VisibleColumns (at least 1, max 20, valid column names) in CreateTicketView.Validator in `src/App.Application/TicketViews/Commands/CreateTicketView.cs`
- [ ] T056 [US3] Add validation for VisibleColumns in UpdateTicketView.Validator in `src/App.Application/TicketViews/Commands/UpdateTicketView.cs`

### UI Components for US3

- [x] T057 [P] [US3] Create _ColumnSelector.cshtml partial with checkboxes, drag handles, and SortableJS initialization in `src/App.Web/Areas/Staff/Pages/Shared/_Partials/_ColumnSelector.cshtml`
- [x] T058 [US3] Add visual distinction between selected and unselected columns (checkbox + opacity/color) in _ColumnSelector.cshtml
- [x] T059 [US3] Add hidden order inputs updated on drag-drop reorder in _ColumnSelector.cshtml
- [x] T060 [US3] Update Staff Views/Create.cshtml to use _ColumnSelector partial replacing checkbox list in `src/App.Web/Areas/Staff/Pages/Views/Create.cshtml`
- [x] T061 [US3] Update Staff Views/Create.cshtml.cs to handle ordered VisibleColumns array binding in `src/App.Web/Areas/Staff/Pages/Views/Create.cshtml.cs`
- [x] T062 [US3] Update Staff Views/Edit.cshtml to use _ColumnSelector partial in `src/App.Web/Areas/Staff/Pages/Views/Edit.cshtml`
- [x] T063 [US3] Update Staff Views/Edit.cshtml.cs to load existing VisibleColumns with order into _ColumnSelector in `src/App.Web/Areas/Staff/Pages/Views/Edit.cshtml.cs`
- [ ] T064 [US3] Update Tickets/Index.cshtml to render only columns in view.VisibleColumns in configured order in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml`
- [ ] T065 [US3] Update Tickets/Index.cshtml.cs to pass VisibleColumns to view for dynamic table rendering in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs`
- [ ] T066 [US3] Make Ticket ID and Title columns clickable links to ticket detail with proper backToListUrl in Tickets/Index.cshtml
- [ ] T067 [US3] Make Contact ID column clickable link to contact detail page in Tickets/Index.cshtml
- [ ] T068 [US3] Display message when no searchable columns selected (search unavailable for this view) in Tickets/Index.cshtml

**Checkpoint**: User Story 3 (Column Selection and Ordering) is fully functional and testable independently

---

## Phase 6: User Story 4 - Admin System Views Management (Priority: P4)

**Goal**: Administrators have the same advanced view configuration capabilities for system-wide views as staff users have for personal views.

**Independent Test**: As admin, create a system view with advanced filters, multi-level sorting, and custom columns. Verify staff users can see and apply this system view.

### UI Integration for US4

- [ ] T069 [US4] Copy _FilterBuilder.cshtml to Admin area or create shared partial reference in `src/App.Web/Areas/Admin/Pages/Tickets/SystemViews/`
- [ ] T070 [US4] Copy _SortConfigurator.cshtml to Admin area or create shared partial reference in `src/App.Web/Areas/Admin/Pages/Tickets/SystemViews/`
- [ ] T071 [US4] Copy _ColumnSelector.cshtml to Admin area or create shared partial reference in `src/App.Web/Areas/Admin/Pages/Tickets/SystemViews/`
- [ ] T072 [US4] Update Admin SystemViews/Create.cshtml to use _FilterBuilder, _SortConfigurator, _ColumnSelector partials in `src/App.Web/Areas/Admin/Pages/Tickets/SystemViews/Create.cshtml`
- [ ] T073 [US4] Update Admin SystemViews/Create.cshtml.cs to handle enhanced Conditions, SortLevels, VisibleColumns in `src/App.Web/Areas/Admin/Pages/Tickets/SystemViews/Create.cshtml.cs`
- [ ] T074 [US4] Update Admin SystemViews/Edit.cshtml to use _FilterBuilder, _SortConfigurator, _ColumnSelector partials in `src/App.Web/Areas/Admin/Pages/Tickets/SystemViews/Edit.cshtml`
- [ ] T075 [US4] Update Admin SystemViews/Edit.cshtml.cs to load existing view config into all three partials in `src/App.Web/Areas/Admin/Pages/Tickets/SystemViews/Edit.cshtml.cs`
- [ ] T076 [US4] Include filter-builder.js and sort-configurator.js in Admin area layout in `src/App.Web/Areas/Admin/Pages/Shared/_Layout.cshtml`
- [ ] T077 [US4] Ensure system views appear in staff view selector with system view indicator in `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml`

**Checkpoint**: User Story 4 (Admin System Views) is fully functional and testable independently

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T078 [P] Add structured logging for view filter evaluation in ViewFilterBuilder
- [ ] T079 [P] Handle edge case: filter references deleted status/priority - ignore with visual indicator
- [ ] T080 [P] Handle edge case: filter references deleted user - show "(Deleted User)" and filter by ID
- [ ] T081 [P] Add touch-friendly drag handle sizing for mobile devices in CSS
- [ ] T082 [P] Add smooth animation transitions for drag-drop operations in CSS
- [ ] T083 Performance optimization: ensure 20 conditions on 100k tickets queries in <2s
- [ ] T084 Update quickstart.md with any implementation learnings
- [ ] T085 Final testing: verify all 4 user stories work together without conflicts

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3 → P4)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Uses same ViewFilterBuilder but independent functionality
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Shares UI pages with US1/US2 but independent features
- **User Story 4 (P4)**: Depends on US1, US2, US3 UI components being available (uses shared partials)

### Within Each User Story

- Backend before UI (service layer supports frontend)
- Backend tasks can often run in parallel (different operators)
- UI partials before page integration
- Page Create before Edit (establish pattern)

### Parallel Opportunities

- All Foundational tasks marked [P] can run in parallel (T007-T010)
- All Backend filter operators (T016-T024) can run in parallel
- filter-builder.js (T027) and sort-configurator.js (T043) can run in parallel
- All Polish tasks marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members (except US4 which depends on US1-US3 partials)

---

## Parallel Example: User Story 1 Backend

```bash
# Launch all operator implementations together (different filter types):
Task: T016 "Implement string operators in ViewFilterBuilder"
Task: T017 "Implement date operators in ViewFilterBuilder"
Task: T019 "Implement boolean operators in ViewFilterBuilder"
Task: T020 "Implement numeric operators in ViewFilterBuilder"
Task: T021 "Implement selection operators in ViewFilterBuilder"
Task: T022 "Implement priority comparison operators in ViewFilterBuilder"
Task: T023 "Implement user operators in ViewFilterBuilder"
Task: T024 "Implement contact field filters in ViewFilterBuilder"

# These all modify ViewFilterBuilder but different switch cases - minimal conflict
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T015)
3. Complete Phase 3: User Story 1 (T016-T039)
4. **STOP and VALIDATE**: Test advanced filter builder independently
5. Deploy/demo if ready - users can create views with AND/OR filters

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy (MVP! Advanced filtering)
3. Add User Story 2 → Test independently → Deploy (Multi-level sorting)
4. Add User Story 3 → Test independently → Deploy (Column customization)
5. Add User Story 4 → Test independently → Deploy (Admin parity)
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 backend (T016-T026)
   - Developer B: User Story 1 UI (T027-T039)
3. After US1:
   - Developer A: User Story 2
   - Developer B: User Story 3
4. User Story 4 can proceed after US1-US3 partials are ready

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- SortableJS already available in codebase - no new dependencies
- ViewFilterBuilder uses switch expression pattern - easy to add operators
- JSON columns already established pattern - follows existing VisibleColumnsJson approach

