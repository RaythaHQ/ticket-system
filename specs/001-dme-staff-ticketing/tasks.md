# Tasks: DME Staff Ticketing System

**Feature Branch**: `001-dme-staff-ticketing`  
**Input**: Design documents from `/specs/001-dme-staff-ticketing/`  
**Prerequisites**: plan.md ✓, spec.md ✓

**Tests**: Tests are NOT explicitly requested in the specification. Test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

---

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, base classes, and scaffolding

- [x] T001 Create numeric ID base entity in src/App.Domain/Common/BaseNumericEntity.cs
- [x] T002 [P] Create numeric ID auditable entity in src/App.Domain/Common/BaseNumericAuditableEntity.cs
- [x] T003 [P] Create numeric ID full auditable entity in src/App.Domain/Common/BaseNumericFullAuditableEntity.cs
- [x] T004 [P] Create numeric ID DTO base classes in src/App.Application/Common/Models/BaseNumericEntityDto.cs
- [x] T005 Add permission flags (CanManageTickets, ManageTeams, AccessReports) to src/App.Domain/Entities/User.cs
- [x] T006 Create ITicketPermissionService interface in src/App.Application/Common/Interfaces/ITicketPermissionService.cs
- [x] T007 Implement TicketPermissionService in src/App.Application/Common/Services/TicketPermissionService.cs
- [x] T008 Register TicketPermissionService in src/App.Application/ConfigureServices.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain entities, value objects, and persistence that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Value Objects

- [x] T009 [P] Create TicketStatus value object in src/App.Domain/ValueObjects/TicketStatus.cs
- [x] T010 [P] Create TicketPriority value object in src/App.Domain/ValueObjects/TicketPriority.cs
- [x] T011 [P] Create SlaStatus value object in src/App.Domain/ValueObjects/SlaStatus.cs
- [x] T012 [P] Create NotificationEventType value object in src/App.Domain/ValueObjects/NotificationEventType.cs

### Core Entities

- [x] T013 Create Ticket entity (numeric ID) in src/App.Domain/Entities/Ticket.cs
- [x] T014 [P] Create Contact entity (numeric ID) in src/App.Domain/Entities/Contact.cs
- [x] T015 [P] Create Team entity in src/App.Domain/Entities/Team.cs
- [x] T016 [P] Create TeamMembership entity in src/App.Domain/Entities/TeamMembership.cs
- [x] T017 [P] Create TicketChangeLogEntry entity in src/App.Domain/Entities/TicketChangeLogEntry.cs
- [x] T018 [P] Create TicketComment entity in src/App.Domain/Entities/TicketComment.cs
- [x] T019 [P] Create TicketAttachment entity in src/App.Domain/Entities/TicketAttachment.cs
- [x] T020 [P] Create ContactChangeLogEntry entity in src/App.Domain/Entities/ContactChangeLogEntry.cs
- [x] T021 [P] Create ContactComment entity in src/App.Domain/Entities/ContactComment.cs
- [x] T022 [P] Create SlaRule entity in src/App.Domain/Entities/SlaRule.cs
- [x] T023 [P] Create TicketView entity in src/App.Domain/Entities/TicketView.cs
- [x] T024 [P] Create NotificationPreference entity in src/App.Domain/Entities/NotificationPreference.cs

### Persistence Layer

- [x] T025 Add new DbSets to IAppDbContext in src/App.Application/Common/Interfaces/IRaythaDbContext.cs
- [x] T026 Implement new DbSets in src/App.Infrastructure/Persistence/AppDbContext.cs
- [x] T027 [P] Create TicketConfiguration (numeric ID, identity column) in src/App.Infrastructure/Persistence/Configurations/TicketConfiguration.cs
- [x] T028 [P] Create ContactConfiguration (numeric ID, identity column) in src/App.Infrastructure/Persistence/Configurations/ContactConfiguration.cs
- [x] T029 [P] Create TeamConfiguration in src/App.Infrastructure/Persistence/Configurations/TeamConfiguration.cs
- [x] T030 [P] Create TeamMembershipConfiguration in src/App.Infrastructure/Persistence/Configurations/TeamMembershipConfiguration.cs
- [x] T031 [P] Create TicketChangeLogEntryConfiguration in src/App.Infrastructure/Persistence/Configurations/TicketChangeLogEntryConfiguration.cs
- [x] T032 [P] Create TicketCommentConfiguration in src/App.Infrastructure/Persistence/Configurations/TicketCommentConfiguration.cs
- [x] T033 [P] Create TicketAttachmentConfiguration in src/App.Infrastructure/Persistence/Configurations/TicketAttachmentConfiguration.cs
- [x] T034 [P] Create ContactChangeLogEntryConfiguration in src/App.Infrastructure/Persistence/Configurations/ContactChangeLogEntryConfiguration.cs
- [x] T035 [P] Create ContactCommentConfiguration in src/App.Infrastructure/Persistence/Configurations/ContactCommentConfiguration.cs
- [x] T036 [P] Create SlaRuleConfiguration in src/App.Infrastructure/Persistence/Configurations/SlaRuleConfiguration.cs
- [x] T037 [P] Create TicketViewConfiguration in src/App.Infrastructure/Persistence/Configurations/TicketViewConfiguration.cs
- [x] T038 [P] Create NotificationPreferenceConfiguration in src/App.Infrastructure/Persistence/Configurations/NotificationPreferenceConfiguration.cs
- [x] T039 Create EF migration: dotnet ef migrations add AddTicketingSystem -p src/App.Infrastructure -s src/App.Web

### Staff Area Scaffolding

- [x] T040 Create Staff area folder structure in src/App.Web/Areas/Staff/Pages/
- [x] T041 Create _ViewImports.cshtml in src/App.Web/Areas/Staff/Pages/_ViewImports.cshtml
- [x] T042 [P] Create _ViewStart.cshtml in src/App.Web/Areas/Staff/Pages/_ViewStart.cshtml
- [x] T043 Create BaseStaffPageModel in src/App.Web/Areas/Staff/Pages/Shared/BaseStaffPageModel.cs
- [x] T044 [P] Create _Layout.cshtml for Staff area in src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml
- [x] T045 [P] Create _StaffNav.cshtml partial in src/App.Web/Areas/Staff/Pages/Shared/_StaffNav.cshtml

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Base Staff Creates and Comments on Tickets (Priority: P1) 🎯 MVP

**Goal**: Staff can create tickets, link to contacts, add comments, and view ticket lists

**Independent Test**: Create a ticket, link a contact, add a comment, verify ticket appears in views

### Domain Events

- [x] T046 [P] [US1] Create TicketCreatedEvent in src/App.Domain/Events/TicketCreatedEvent.cs
- [x] T047 [P] [US1] Create TicketCommentAddedEvent in src/App.Domain/Events/TicketCommentAddedEvent.cs

### Application Layer - DTOs

- [x] T048 [P] [US1] Create TicketDto in src/App.Application/Tickets/TicketDto.cs
- [x] T049 [P] [US1] Create TicketListItemDto in src/App.Application/Tickets/TicketListItemDto.cs
- [x] T050 [P] [US1] Create TicketCommentDto in src/App.Application/Tickets/TicketCommentDto.cs
- [x] T051 [P] [US1] Create TicketChangeLogEntryDto in src/App.Application/Tickets/TicketChangeLogEntryDto.cs

### Application Layer - Commands

- [x] T052 [US1] Create CreateTicket command with validator and handler in src/App.Application/Tickets/Commands/CreateTicket.cs
- [x] T053 [P] [US1] Create AddTicketComment command in src/App.Application/Tickets/Commands/AddTicketComment.cs

### Application Layer - Queries

- [x] T054 [P] [US1] Create GetTicketById query in src/App.Application/Tickets/Queries/GetTicketById.cs
- [x] T055 [P] [US1] Create GetTickets query in src/App.Application/Tickets/Queries/GetTickets.cs
- [x] T056 [P] [US1] Create GetTicketComments query in src/App.Application/Tickets/Queries/GetTicketComments.cs
- [x] T057 [P] [US1] Create GetTicketChangeLog query in src/App.Application/Tickets/Queries/GetTicketChangeLog.cs

### Staff UI - Tickets

- [x] T058 [US1] Create Ticket List page in src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml
- [x] T059 [US1] Create Ticket List PageModel in src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs
- [x] T060 [P] [US1] Create Ticket Create page in src/App.Web/Areas/Staff/Pages/Tickets/Create.cshtml
- [x] T061 [P] [US1] Create Ticket Create PageModel in src/App.Web/Areas/Staff/Pages/Tickets/Create.cshtml.cs
- [x] T062 [US1] Create Ticket Details page in src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml
- [x] T063 [US1] Create Ticket Details PageModel in src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs
- [x] T064 [P] [US1] Create _TicketComments.cshtml partial in src/App.Web/Areas/Staff/Pages/Tickets/_TicketComments.cshtml
- [x] T065 [P] [US1] Create _TicketChangeLog.cshtml partial in src/App.Web/Areas/Staff/Pages/Tickets/_TicketChangeLog.cshtml

**Checkpoint**: User Story 1 complete - staff can create tickets and add comments

---

## Phase 4: User Story 2 - Staff with Manage Permission Modifies and Reassigns Tickets (Priority: P1)

**Goal**: Staff with "Can Manage Tickets" permission can modify status, priority, assignee, and close/reopen tickets

**Independent Test**: Modify ticket attributes, reassign, close/reopen, verify change log captures all changes

### Domain Events

- [x] T066 [P] [US2] Create TicketStatusChangedEvent in src/App.Domain/Events/TicketStatusChangedEvent.cs
- [x] T067 [P] [US2] Create TicketAssignedEvent in src/App.Domain/Events/TicketAssignedEvent.cs
- [x] T068 [P] [US2] Create TicketClosedEvent in src/App.Domain/Events/TicketClosedEvent.cs
- [x] T069 [P] [US2] Create TicketReopenedEvent in src/App.Domain/Events/TicketReopenedEvent.cs

### Application Layer - Commands

- [x] T070 [US2] Create UpdateTicket command with permission check in src/App.Application/Tickets/Commands/UpdateTicket.cs
- [x] T071 [P] [US2] Create ChangeTicketStatus command in src/App.Application/Tickets/Commands/ChangeTicketStatus.cs
- [x] T072 [P] [US2] Create AssignTicket command in src/App.Application/Tickets/Commands/AssignTicket.cs
- [x] T073 [P] [US2] Create CloseTicket command in src/App.Application/Tickets/Commands/CloseTicket.cs
- [x] T074 [P] [US2] Create ReopenTicket command in src/App.Application/Tickets/Commands/ReopenTicket.cs

### Staff UI - Ticket Edit

- [x] T075 [US2] Create Ticket Edit page in src/App.Web/Areas/Staff/Pages/Tickets/Edit.cshtml
- [x] T076 [US2] Create Ticket Edit PageModel with permission checks in src/App.Web/Areas/Staff/Pages/Tickets/Edit.cshtml.cs
- [x] T077 [US2] Update Ticket Details page to show/hide edit controls based on CanManageTickets permission

**Checkpoint**: User Story 2 complete - ticket lifecycle management functional

---

## Phase 5: User Story 3 - Contact Management and Ticket Association (Priority: P1)

**Goal**: Staff can create/edit contacts, search by phone/email/name, view associated tickets

**Independent Test**: Create contact, search by phone in various formats, link to ticket, view contact's tickets

### Application Layer - Utilities

- [x] T078 [US3] Create PhoneNumberNormalizer utility in src/App.Application/Contacts/Utils/PhoneNumberNormalizer.cs

### Application Layer - DTOs

- [x] T079 [P] [US3] Create ContactDto in src/App.Application/Contacts/ContactDto.cs
- [x] T080 [P] [US3] Create ContactListItemDto in src/App.Application/Contacts/ContactListItemDto.cs
- [x] T081 [P] [US3] Create ContactCommentDto in src/App.Application/Contacts/ContactCommentDto.cs
- [x] T082 [P] [US3] Create ContactChangeLogEntryDto in src/App.Application/Contacts/ContactChangeLogEntryDto.cs

### Application Layer - Commands

- [x] T083 [US3] Create CreateContact command with phone normalization in src/App.Application/Contacts/Commands/CreateContact.cs
- [x] T084 [P] [US3] Create UpdateContact command with change logging in src/App.Application/Contacts/Commands/UpdateContact.cs
- [x] T085 [P] [US3] Create AddContactComment command in src/App.Application/Contacts/Commands/AddContactComment.cs

### Application Layer - Queries

- [x] T086 [P] [US3] Create GetContactById query in src/App.Application/Contacts/Queries/GetContactById.cs
- [x] T087 [P] [US3] Create GetContacts query in src/App.Application/Contacts/Queries/GetContacts.cs
- [x] T088 [US3] Create SearchContacts query with phone normalization in src/App.Application/Contacts/Queries/SearchContacts.cs
- [x] T089 [P] [US3] Create GetContactTickets query in src/App.Application/Contacts/Queries/GetContactTickets.cs
- [x] T090 [P] [US3] Create GetContactChangeLog query in src/App.Application/Contacts/Queries/GetContactChangeLog.cs

### Staff UI - Contacts

- [x] T091 [US3] Create Contact List page in src/App.Web/Areas/Staff/Pages/Contacts/Index.cshtml
- [x] T092 [US3] Create Contact List PageModel with search in src/App.Web/Areas/Staff/Pages/Contacts/Index.cshtml.cs
- [x] T093 [P] [US3] Create Contact Create page in src/App.Web/Areas/Staff/Pages/Contacts/Create.cshtml
- [x] T094 [P] [US3] Create Contact Create PageModel in src/App.Web/Areas/Staff/Pages/Contacts/Create.cshtml.cs
- [x] T095 [US3] Create Contact Details page in src/App.Web/Areas/Staff/Pages/Contacts/Details.cshtml
- [x] T096 [US3] Create Contact Details PageModel in src/App.Web/Areas/Staff/Pages/Contacts/Details.cshtml.cs
- [x] T097 [P] [US3] Create Contact Edit page in src/App.Web/Areas/Staff/Pages/Contacts/Edit.cshtml
- [x] T098 [P] [US3] Create Contact Edit PageModel in src/App.Web/Areas/Staff/Pages/Contacts/Edit.cshtml.cs
- [x] T099 [P] [US3] Create _ContactTickets.cshtml partial in src/App.Web/Areas/Staff/Pages/Contacts/_ContactTickets.cshtml
- [x] T100 [P] [US3] Create _ContactChangeLog.cshtml partial in src/App.Web/Areas/Staff/Pages/Contacts/_ContactChangeLog.cshtml
- [x] T101 [P] [US3] Create _ContactComments.cshtml partial in src/App.Web/Areas/Staff/Pages/Contacts/_ContactComments.cshtml

**Checkpoint**: User Story 3 complete - contact management with phone search functional

---

## Phase 6: User Story 9 - Team Management with Manage Teams Permission (Priority: P2)

**Goal**: Staff with "Manage Teams" permission can create/edit teams, manage membership, configure round-robin

**Independent Test**: Create team, add members, toggle is_assignable, enable round-robin

**Note**: This is moved before US4-6 because teams are needed for ticket views and round-robin

### Application Layer - DTOs

- [ ] T102 [P] [US9] Create TeamDto in src/App.Application/Teams/TeamDto.cs
- [ ] T103 [P] [US9] Create TeamMembershipDto in src/App.Application/Teams/TeamMembershipDto.cs

### Application Layer - Commands

- [ ] T104 [US9] Create CreateTeam command with permission check in src/App.Application/Teams/Commands/CreateTeam.cs
- [ ] T105 [P] [US9] Create UpdateTeam command in src/App.Application/Teams/Commands/UpdateTeam.cs
- [ ] T106 [P] [US9] Create DeleteTeam command in src/App.Application/Teams/Commands/DeleteTeam.cs
- [ ] T107 [P] [US9] Create AddTeamMember command in src/App.Application/Teams/Commands/AddTeamMember.cs
- [ ] T108 [P] [US9] Create RemoveTeamMember command in src/App.Application/Teams/Commands/RemoveTeamMember.cs
- [ ] T109 [P] [US9] Create SetMemberAssignable command in src/App.Application/Teams/Commands/SetMemberAssignable.cs
- [ ] T110 [P] [US9] Create ToggleRoundRobin command in src/App.Application/Teams/Commands/ToggleRoundRobin.cs

### Application Layer - Queries

- [ ] T111 [P] [US9] Create GetTeamById query in src/App.Application/Teams/Queries/GetTeamById.cs
- [ ] T112 [P] [US9] Create GetTeams query in src/App.Application/Teams/Queries/GetTeams.cs
- [ ] T113 [P] [US9] Create GetTeamMembers query in src/App.Application/Teams/Queries/GetTeamMembers.cs

### Admin UI - Teams

- [ ] T114 [US9] Create Teams List page in src/App.Web/Areas/Admin/Pages/Teams/Index.cshtml
- [ ] T115 [US9] Create Teams List PageModel with permission check in src/App.Web/Areas/Admin/Pages/Teams/Index.cshtml.cs
- [ ] T116 [P] [US9] Create Team Create page in src/App.Web/Areas/Admin/Pages/Teams/Create.cshtml
- [ ] T117 [P] [US9] Create Team Create PageModel in src/App.Web/Areas/Admin/Pages/Teams/Create.cshtml.cs
- [ ] T118 [P] [US9] Create Team Edit page in src/App.Web/Areas/Admin/Pages/Teams/Edit.cshtml
- [ ] T119 [P] [US9] Create Team Edit PageModel in src/App.Web/Areas/Admin/Pages/Teams/Edit.cshtml.cs
- [ ] T120 [P] [US9] Create Team Delete page in src/App.Web/Areas/Admin/Pages/Teams/Delete.cshtml
- [ ] T121 [P] [US9] Create Team Delete PageModel in src/App.Web/Areas/Admin/Pages/Teams/Delete.cshtml.cs
- [ ] T122 [US9] Create Team Members List page in src/App.Web/Areas/Admin/Pages/Teams/Members/Index.cshtml
- [ ] T123 [US9] Create Team Members List PageModel in src/App.Web/Areas/Admin/Pages/Teams/Members/Index.cshtml.cs
- [ ] T124 [P] [US9] Create Add Member page in src/App.Web/Areas/Admin/Pages/Teams/Members/Add.cshtml
- [ ] T125 [P] [US9] Create Add Member PageModel in src/App.Web/Areas/Admin/Pages/Teams/Members/Add.cshtml.cs

**Checkpoint**: User Story 9 complete - team management with permissions functional

---

## Phase 7: User Story 4 - Using Views to Filter and Manage Ticket Lists (Priority: P2)

**Goal**: Staff can use default views, create custom views, search within views (visible columns only)

**Independent Test**: Select different views, verify filters apply, search respects visible columns

### Application Layer - DTOs

- [ ] T126 [P] [US4] Create TicketViewDto in src/App.Application/TicketViews/TicketViewDto.cs

### Application Layer - Services

- [ ] T127 [US4] Create ViewFilterBuilder service in src/App.Application/TicketViews/Services/ViewFilterBuilder.cs

### Application Layer - Commands

- [ ] T128 [US4] Create CreateTicketView command in src/App.Application/TicketViews/Commands/CreateTicketView.cs
- [ ] T129 [P] [US4] Create UpdateTicketView command in src/App.Application/TicketViews/Commands/UpdateTicketView.cs
- [ ] T130 [P] [US4] Create DeleteTicketView command in src/App.Application/TicketViews/Commands/DeleteTicketView.cs

### Application Layer - Queries

- [ ] T131 [P] [US4] Create GetTicketViewById query in src/App.Application/TicketViews/Queries/GetTicketViewById.cs
- [ ] T132 [US4] Create GetTicketViews query (system + personal views) in src/App.Application/TicketViews/Queries/GetTicketViews.cs
- [ ] T133 [US4] Create GetDefaultViews query (system views seeding) in src/App.Application/TicketViews/Queries/GetDefaultViews.cs

### Update Ticket List to Support Views

- [ ] T134 [US4] Update GetTickets query to support view filters and column-limited search in src/App.Application/Tickets/Queries/GetTickets.cs
- [ ] T135 [US4] Update Ticket List PageModel to support view selection in src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs
- [ ] T136 [US4] Update Ticket List page with view selector and column-aware search in src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml

**Checkpoint**: User Story 4 complete - views and filtered search functional

---

## Phase 8: User Story 6 - Round Robin Auto-Assignment (Priority: P2)

**Goal**: When a ticket is assigned to a team with round-robin enabled, system auto-assigns to next eligible member

**Independent Test**: Configure team with eligible members, create ticket for team, verify auto-assignment

### Application Layer - Services

- [ ] T137 [US6] Create IRoundRobinService interface in src/App.Application/Common/Interfaces/IRoundRobinService.cs
- [ ] T138 [US6] Create RoundRobinService implementation in src/App.Application/Teams/Services/RoundRobinService.cs
- [ ] T139 [US6] Register RoundRobinService in src/App.Infrastructure/ConfigureServices.cs

### Application Layer - Queries

- [ ] T140 [US6] Create GetNextRoundRobinAssignee query in src/App.Application/Teams/Queries/GetNextRoundRobinAssignee.cs

### Integration

- [ ] T141 [US6] Update CreateTicket command to call RoundRobinService when owning_team_id is set
- [ ] T142 [US6] Update AssignTicket command to trigger round-robin when team changes

**Checkpoint**: User Story 6 complete - round-robin auto-assignment functional

---

## Phase 9: User Story 5 - SLA Assignment and Breach Monitoring (Priority: P2)

**Goal**: Tickets automatically receive SLA based on conditions, background job monitors breach status

**Independent Test**: Create SLA rule, create matching ticket, verify SLA assigned, observe status changes

### Domain Events

- [ ] T143 [P] [US5] Create SlaApproachingBreachEvent in src/App.Domain/Events/SlaApproachingBreachEvent.cs
- [ ] T144 [P] [US5] Create SlaBreachedEvent in src/App.Domain/Events/SlaBreachedEvent.cs

### Application Layer - DTOs

- [ ] T145 [P] [US5] Create SlaRuleDto in src/App.Application/SlaRules/SlaRuleDto.cs

### Application Layer - Services

- [ ] T146 [US5] Create SlaEvaluationService in src/App.Application/SlaRules/Services/SlaEvaluationService.cs

### Application Layer - Queries

- [ ] T147 [P] [US5] Create GetSlaRuleById query in src/App.Application/SlaRules/Queries/GetSlaRuleById.cs
- [ ] T148 [P] [US5] Create GetSlaRules query in src/App.Application/SlaRules/Queries/GetSlaRules.cs
- [ ] T149 [US5] Create EvaluateSlaForTicket query in src/App.Application/SlaRules/Queries/EvaluateSlaForTicket.cs

### Background Job

- [ ] T150 [US5] Create SlaEvaluationJob in src/App.Application/BackgroundTasks/Jobs/SlaEvaluationJob.cs
- [ ] T151 [US5] Register SlaEvaluationJob as scheduled task in src/App.Infrastructure/BackgroundTasks/QueuedHostedService.cs

### Integration

- [ ] T152 [US5] Update CreateTicket to evaluate and assign SLA after creation
- [ ] T153 [US5] Update UpdateTicket to re-evaluate SLA when applicable fields change
- [ ] T154 [P] [US5] Create _TicketSlaInfo.cshtml partial in src/App.Web/Areas/Staff/Pages/Tickets/_TicketSlaInfo.cshtml
- [ ] T155 [US5] Update Ticket Details page to display SLA information

**Checkpoint**: User Story 5 complete - SLA assignment and monitoring functional

---

## Phase 10: User Story 11 - System Administrator SLA Configuration (Priority: P3)

**Goal**: System administrators can create/edit SLA rules with conditions, targets, and business hours

**Independent Test**: Create SLA rule, verify it applies to matching tickets

### Application Layer - Commands

- [ ] T156 [US11] Create CreateSlaRule command in src/App.Application/SlaRules/Commands/CreateSlaRule.cs
- [ ] T157 [P] [US11] Create UpdateSlaRule command in src/App.Application/SlaRules/Commands/UpdateSlaRule.cs
- [ ] T158 [P] [US11] Create DeactivateSlaRule command in src/App.Application/SlaRules/Commands/DeactivateSlaRule.cs
- [ ] T159 [P] [US11] Create ReorderSlaRules command in src/App.Application/SlaRules/Commands/ReorderSlaRules.cs

### Admin UI - SLA Rules

- [ ] T160 [US11] Create SLA Rules List page in src/App.Web/Areas/Admin/Pages/SlaRules/Index.cshtml
- [ ] T161 [US11] Create SLA Rules List PageModel in src/App.Web/Areas/Admin/Pages/SlaRules/Index.cshtml.cs
- [ ] T162 [P] [US11] Create SLA Rule Create page in src/App.Web/Areas/Admin/Pages/SlaRules/Create.cshtml
- [ ] T163 [P] [US11] Create SLA Rule Create PageModel in src/App.Web/Areas/Admin/Pages/SlaRules/Create.cshtml.cs
- [ ] T164 [P] [US11] Create SLA Rule Edit page in src/App.Web/Areas/Admin/Pages/SlaRules/Edit.cshtml
- [ ] T165 [P] [US11] Create SLA Rule Edit PageModel in src/App.Web/Areas/Admin/Pages/SlaRules/Edit.cshtml.cs

**Checkpoint**: User Story 11 complete - SLA configuration UI functional

---

## Phase 11: User Story 8 - Notification Preferences and Delivery (Priority: P3)

**Goal**: Staff can configure notification preferences, system sends email notifications for events

**Independent Test**: Configure preferences, trigger events, verify notifications sent correctly

### Email Templates

- [ ] T166 [P] [US8] Create email_ticket_assigned.liquid in src/App.Domain/Entities/DefaultTemplates/email_ticket_assigned.liquid
- [ ] T167 [P] [US8] Create email_ticket_assignedtoteam.liquid in src/App.Domain/Entities/DefaultTemplates/email_ticket_assignedtoteam.liquid
- [ ] T168 [P] [US8] Create email_ticket_commentadded.liquid in src/App.Domain/Entities/DefaultTemplates/email_ticket_commentadded.liquid
- [ ] T169 [P] [US8] Create email_ticket_statuschanged.liquid in src/App.Domain/Entities/DefaultTemplates/email_ticket_statuschanged.liquid
- [ ] T170 [P] [US8] Create email_ticket_closed.liquid in src/App.Domain/Entities/DefaultTemplates/email_ticket_closed.liquid
- [ ] T171 [P] [US8] Create email_ticket_reopened.liquid in src/App.Domain/Entities/DefaultTemplates/email_ticket_reopened.liquid
- [ ] T172 [P] [US8] Create email_sla_approaching.liquid in src/App.Domain/Entities/DefaultTemplates/email_sla_approaching.liquid
- [ ] T173 [P] [US8] Create email_sla_breached.liquid in src/App.Domain/Entities/DefaultTemplates/email_sla_breached.liquid

### Update BuiltInEmailTemplate

- [ ] T174 [US8] Add ticket notification templates to BuiltInEmailTemplate in src/App.Domain/Entities/EmailTemplate.cs

### RenderModels

- [ ] T175 [P] [US8] Create TicketAssigned_RenderModel in src/App.Application/Tickets/RenderModels/TicketAssigned_RenderModel.cs
- [ ] T176 [P] [US8] Create TicketCommentAdded_RenderModel in src/App.Application/Tickets/RenderModels/TicketCommentAdded_RenderModel.cs
- [ ] T177 [P] [US8] Create TicketStatusChanged_RenderModel in src/App.Application/Tickets/RenderModels/TicketStatusChanged_RenderModel.cs
- [ ] T178 [P] [US8] Create SlaApproaching_RenderModel in src/App.Application/Tickets/RenderModels/SlaApproaching_RenderModel.cs
- [ ] T179 [P] [US8] Create SlaBreach_RenderModel in src/App.Application/Tickets/RenderModels/SlaBreach_RenderModel.cs

### URL Builder

- [ ] T180 [US8] Add StaffTicketDetailsUrl and StaffContactDetailsUrl to IRelativeUrlBuilder in src/App.Application/Common/Interfaces/IRelativeUrlBuilder.cs
- [ ] T181 [US8] Implement new URL methods in src/App.Web/Services/RelativeUrlBuilder.cs

### Event Handlers

- [ ] T182 [US8] Create TicketAssignedEventHandler in src/App.Application/Tickets/EventHandlers/TicketAssignedEventHandler.cs
- [ ] T183 [P] [US8] Create TicketCommentAddedEventHandler in src/App.Application/Tickets/EventHandlers/TicketCommentAddedEventHandler.cs
- [ ] T184 [P] [US8] Create TicketStatusChangedEventHandler in src/App.Application/Tickets/EventHandlers/TicketStatusChangedEventHandler.cs
- [ ] T185 [P] [US8] Create TicketClosedEventHandler in src/App.Application/Tickets/EventHandlers/TicketClosedEventHandler.cs
- [ ] T186 [P] [US8] Create TicketReopenedEventHandler in src/App.Application/Tickets/EventHandlers/TicketReopenedEventHandler.cs
- [ ] T187 [P] [US8] Create SlaApproachingBreachEventHandler in src/App.Application/Tickets/EventHandlers/SlaApproachingBreachEventHandler.cs
- [ ] T188 [P] [US8] Create SlaBreachedEventHandler in src/App.Application/Tickets/EventHandlers/SlaBreachedEventHandler.cs

### Notification Preferences UI

- [ ] T189 [P] [US8] Create NotificationPreferenceDto in src/App.Application/NotificationPreferences/NotificationPreferenceDto.cs
- [ ] T190 [P] [US8] Create GetNotificationPreferences query in src/App.Application/NotificationPreferences/Queries/GetNotificationPreferences.cs
- [ ] T191 [US8] Create UpdateNotificationPreferences command in src/App.Application/NotificationPreferences/Commands/UpdateNotificationPreferences.cs
- [ ] T192 [US8] Add notification settings section to user Profile page in src/App.Web/Areas/Admin/Pages/Profile/Index.cshtml

**Checkpoint**: User Story 8 complete - notification system fully functional

---

## Phase 12: User Story 7 - Viewing Dashboards and Metrics (Priority: P3)

**Goal**: Staff can view personal dashboard with ticket metrics, resolved counts, close time

**Independent Test**: View dashboard with various ticket states, verify counts are accurate

### Application Layer - DTOs

- [ ] T193 [P] [US7] Create UserDashboardMetricsDto in src/App.Application/Tickets/UserDashboardMetricsDto.cs

### Application Layer - Queries

- [ ] T194 [US7] Create GetUserDashboardMetrics query in src/App.Application/Tickets/Queries/GetUserDashboardMetrics.cs

### Staff UI - Dashboard

- [ ] T195 [US7] Create Staff Dashboard page in src/App.Web/Areas/Staff/Pages/Dashboard/Index.cshtml
- [ ] T196 [US7] Create Staff Dashboard PageModel in src/App.Web/Areas/Staff/Pages/Dashboard/Index.cshtml.cs

**Checkpoint**: User Story 7 complete - personal dashboard functional

---

## Phase 13: User Story 10 - Accessing Reports with Access Reports Permission (Priority: P2)

**Goal**: Staff with "Access Reports" permission can view team-level and org-wide reports, export data

**Independent Test**: Access reports with permission, verify team/org metrics, export CSV

### Application Layer - DTOs

- [ ] T197 [P] [US10] Create TeamReportDto in src/App.Application/Tickets/TeamReportDto.cs
- [ ] T198 [P] [US10] Create OrganizationReportDto in src/App.Application/Tickets/OrganizationReportDto.cs

### Application Layer - Queries

- [ ] T199 [US10] Create GetTeamReport query with permission check in src/App.Application/Tickets/Queries/GetTeamReport.cs
- [ ] T200 [P] [US10] Create GetOrganizationReport query in src/App.Application/Tickets/Queries/GetOrganizationReport.cs
- [ ] T201 [P] [US10] Create GetSlaComplianceReport query in src/App.Application/Tickets/Queries/GetSlaComplianceReport.cs

### Admin UI - Reports

- [ ] T202 [US10] Create Reports Index page in src/App.Web/Areas/Admin/Pages/Reports/Index.cshtml
- [ ] T203 [US10] Create Reports Index PageModel with permission check in src/App.Web/Areas/Admin/Pages/Reports/Index.cshtml.cs
- [ ] T204 [P] [US10] Create Team Report page in src/App.Web/Areas/Admin/Pages/Reports/TeamReport.cshtml
- [ ] T205 [P] [US10] Create Team Report PageModel in src/App.Web/Areas/Admin/Pages/Reports/TeamReport.cshtml.cs
- [ ] T206 [P] [US10] Create SLA Report page in src/App.Web/Areas/Admin/Pages/Reports/SlaReport.cshtml
- [ ] T207 [P] [US10] Create SLA Report PageModel in src/App.Web/Areas/Admin/Pages/Reports/SlaReport.cshtml.cs
- [ ] T208 [US10] Create ExportReport endpoint for CSV/PDF export in src/App.Web/Areas/Admin/Endpoints/ReportsEndpoints.cs

**Checkpoint**: User Story 10 complete - reports with export functional

---

## Phase 14: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T209 [P] Add navigation links in admin sidebar for Teams, SLA Rules, Reports
- [ ] T210 [P] Add navigation links in staff sidebar for Tickets, Contacts, Dashboard
- [ ] T211 Update UserConfiguration to include new permission flags in src/App.Infrastructure/Persistence/Configurations/UserConfiguration.cs
- [ ] T212 [P] Add seed data for default ticket views in database seeding
- [ ] T213 Code cleanup and remove any TODO comments
- [ ] T214 [P] Update README with ticketing system documentation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Setup - BLOCKS all user stories
- **Phase 3-13 (User Stories)**: All depend on Foundational phase completion
- **Phase 14 (Polish)**: Depends on all desired user stories being complete

### User Story Dependencies

| Story | Depends On | Can Parallelize With |
|-------|------------|---------------------|
| US1 (P1) | Foundational only | US2, US3 |
| US2 (P1) | US1 (extends ticket functionality) | US3 |
| US3 (P1) | Foundational only | US1 (after T058) |
| US9 (P2) | Foundational only | US1, US2, US3 |
| US4 (P2) | US1 (ticket list) | US5, US6, US9 |
| US6 (P2) | US9 (teams) | US4, US5 |
| US5 (P2) | US1 (tickets exist) | US4, US6 |
| US11 (P3) | US5 (SLA service) | US7, US8, US10 |
| US8 (P3) | US1, US2 (events exist) | US7, US10 |
| US7 (P3) | US1 (tickets for metrics) | US8, US10 |
| US10 (P2) | US5, US9 (SLA + teams) | US7, US8 |

### Parallel Opportunities Per Phase

**Phase 2 (Foundational)**:
```
Parallel Group 1: T009, T010, T011, T012 (value objects)
Parallel Group 2: T013-T024 (entities, after value objects)
Parallel Group 3: T027-T038 (configurations)
Parallel Group 4: T041-T045 (staff area scaffolding)
```

**Phase 3 (US1)**:
```
Parallel Group 1: T046, T047 (events)
Parallel Group 2: T048-T051 (DTOs)
Parallel Group 3: T054-T057 (queries)
Sequential: T052, T053 (commands after entities)
```

---

## Implementation Strategy

### MVP First (User Stories 1-3 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (Create/View/Comment on Tickets)
4. **VALIDATE**: Test ticket creation and commenting
5. Complete Phase 4: User Story 2 (Ticket Lifecycle Management)
6. Complete Phase 5: User Story 3 (Contact Management)
7. **STOP and DEMO**: Core ticketing and contact system complete

### Incremental Delivery

| Delivery | Includes | Value Delivered |
|----------|----------|-----------------|
| MVP | US1, US2, US3 | Core ticketing + contacts |
| Release 2 | + US4, US9 | Views + Team management |
| Release 3 | + US5, US6 | SLA + Round-robin |
| Release 4 | + US7, US8, US10, US11 | Dashboard, Notifications, Reports |

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 214 |
| **Setup Tasks** | 8 |
| **Foundational Tasks** | 37 |
| **US1 Tasks** | 20 |
| **US2 Tasks** | 12 |
| **US3 Tasks** | 24 |
| **US4 Tasks** | 11 |
| **US5 Tasks** | 13 |
| **US6 Tasks** | 6 |
| **US7 Tasks** | 4 |
| **US8 Tasks** | 27 |
| **US9 Tasks** | 24 |
| **US10 Tasks** | 12 |
| **US11 Tasks** | 10 |
| **Polish Tasks** | 6 |
| **Parallel Tasks [P]** | 127 (59%) |

**Suggested MVP Scope**: User Stories 1, 2, and 3 (Phases 1-5) = 101 tasks

---

## Notes

- [P] tasks can run in parallel (different files, no dependencies)
- [Story] labels enable tracking and independent testing
- Each user story checkpoint validates independently testable functionality
- Commit after each task or logical group
- All numeric ID handling (Ticket, Contact) uses `long` type and identity columns
- Email templates follow existing BuiltInEmailTemplate pattern with .liquid files

