# Feature Specification: Advanced Views

**Feature Branch**: `003-advanced-views`  
**Created**: 2025-12-13  
**Status**: Draft  
**Input**: User description: "Enhanced custom views with advanced filtering (ANY/ALL condition builders), multi-level sorting, and drag-drop column selection for both staff and admin system views"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Advanced Filter Builder (Priority: P1)

A staff user or administrator needs to create custom views with complex filtering logic to find exactly the tickets they need. Currently, filters are limited to single-value dropdowns. Users need the ability to build conditions with AND/OR logic, supporting multiple operators per attribute type.

**Why this priority**: Advanced filtering is the core value proposition. Without flexible filtering, views are limited in usefulness for real-world workflows where users need to find tickets matching complex criteria (e.g., "Open tickets assigned to me OR unassigned tickets with Urgent priority created in the last 7 days").

**Independent Test**: Can be fully tested by creating a view with multiple filter conditions using AND/OR logic and verifying the correct tickets appear in the list.

**Acceptance Scenarios**:

1. **Given** a user is on the create/edit view page, **When** they click "Add condition", **Then** they see a row with attribute selector, operator selector, and value input appropriate to the attribute type.

2. **Given** a filter builder with multiple conditions, **When** the user selects "Match ALL conditions" or "Match ANY condition", **Then** the logic mode is clearly indicated and applied when querying tickets.

3. **Given** a string attribute like Title is selected, **When** the user views operator options, **Then** they see: equals, not equals, contains, does not contain, starts with, does not start with, ends with, does not end with, is empty, is not empty.

4. **Given** a date attribute like Created At is selected, **When** the user views operator options, **Then** they see: is, is within, is before, is after, is on or before, is on or after, is empty, is not empty.

5. **Given** a date operator like "is within" is selected, **When** the user views value options, **Then** they see relative date options: today, yesterday, this week, last week, this month, last month, number of days ago, number of days from now, exact date.

6. **Given** a Status attribute is selected, **When** the user views options, **Then** they can select from all status values AND from meta-options "Open" (all non-closed statuses) and "Closed" (closed/cancelled statuses).

7. **Given** a Priority attribute is selected with operator "greater than", **When** the user selects "Normal", **Then** the view shows tickets with Urgent and High priority (higher importance).

8. **Given** a boolean attribute like "SLA Breached" is selected, **When** the user views value options, **Then** they see "Yes" and "No" as selectable values.

9. **Given** a user attribute like "Assignee" or "Created By" is selected, **When** the user views options, **Then** they see a searchable dropdown of staff members including suspended ones (with "(deactivated)" suffix).

10. **Given** a view with saved filter conditions, **When** a user browses the ticket list using that view, **Then** the view's base filter is applied, and any additional filters from the top filter bar are LAYERED on top (combined with AND logic).

---

### User Story 2 - Multi-Level Sort Order (Priority: P2)

Users need views to have a defined default sort order with multiple levels. For example, "Sort by Priority descending, then by Created Date ascending." The list view should show this as the default sort option while still allowing temporary override.

**Why this priority**: Sorting is essential for usability but builds on the filtering foundation. A well-sorted view lets users immediately see what's most important.

**Independent Test**: Can be tested by creating a view with multi-level sorting and verifying tickets appear in the correct order, then overriding with another sort and returning to the view's default.

**Acceptance Scenarios**:

1. **Given** a user is on the create/edit view page, **When** they access the sort configuration, **Then** they can add a primary sort field with ascending/descending direction.

2. **Given** a primary sort is configured, **When** the user clicks "Add sort level", **Then** they can configure additional sort fields in priority order.

3. **Given** a view has multi-level sorting configured as "Priority desc, Created At asc", **When** viewing the ticket list, **Then** the sort pills show the view's sort as the first option formatted as "Priority ↓, Created ↑".

4. **Given** a user is on a view with custom sort order, **When** they click a different sort option (e.g., "Newest"), **Then** that sort OVERRIDES the view's default temporarily.

5. **Given** a user has overridden the sort order, **When** they click the view's default sort pill, **Then** the original multi-level sort order is restored.

6. **Given** the sort configuration UI, **When** a user adds sort levels, **Then** they can reorder them via drag-and-drop and remove individual levels.

---

### User Story 3 - Customizable Column Selection and Ordering (Priority: P3)

Users need to select which columns appear in their view and control the order in which they appear. The search box should only search across the columns that are displayed.

**Why this priority**: Column customization enhances focus and reduces visual clutter but is usable even with default columns. This is additive to filtering and sorting.

**Independent Test**: Can be tested by selecting specific columns in a view, reordering them, and verifying the table displays only those columns in the specified order.

**Acceptance Scenarios**:

1. **Given** a user is on the create/edit view page, **When** they access column configuration, **Then** they see a list of all available columns with checkboxes and drag handles.

2. **Given** the column configuration UI, **When** a user drags a column to a new position, **Then** the order updates visually with smooth animation.

3. **Given** a view with specific columns selected, **When** viewing the ticket list, **Then** only those columns appear in the table in the configured order.

4. **Given** a view with "Ticket ID", "Title", and "Contact Name" columns, **When** a user types in the search box, **Then** search results include tickets where any of those three fields contain the search term.

5. **Given** a column configuration includes "Contact ID", **When** viewing the list, **Then** the Contact ID column values are clickable links to the contact detail page.

6. **Given** a column configuration includes "Ticket ID" or "Title", **When** viewing the list, **Then** those column values are clickable links to the ticket detail page with proper back-to-list URL handling.

---

### User Story 4 - Admin System Views Management (Priority: P4)

Administrators need the same advanced view configuration capabilities for system-wide views that are available to all users. The admin panel's System Views section must have feature parity with user-created views.

**Why this priority**: System views are important for organization-wide consistency but depend on the core view enhancement features being implemented first.

**Independent Test**: Can be tested by an admin creating/editing a system view with advanced filters, multi-level sorting, and column ordering, then verifying staff users see these system views with correct configuration.

**Acceptance Scenarios**:

1. **Given** an admin is on the System Views create/edit page, **When** they configure the view, **Then** they have access to the same advanced filter builder, multi-level sorting, and column ordering as staff views.

2. **Given** a system view is marked as "default", **When** any staff user first loads the ticket list, **Then** they see the default system view applied.

3. **Given** a system view exists, **When** staff users view the ticket list, **Then** they see system views in their view selector and can apply them.

---

### Edge Cases

- What happens when a filter references a deleted status or priority? → The filter condition is ignored with a visual indicator that the filter contains invalid values.
- What happens when filtering on "Created By" and the referenced admin no longer exists? → Show "(Deleted User)" and continue to filter correctly by ID.
- How does search behave when no searchable columns are selected? → Display a message that search is unavailable for this view configuration.
- What happens when dragging columns on touch devices? → Touch-friendly drag handles with appropriate hit targets.
- How are relative date filters evaluated? → Relative dates are evaluated at query time based on the user's configured timezone.
- What happens when a view has 0 conditions but user adds filters from top bar? → Only top bar filters apply (view shows all tickets, filtered by bar).

## Requirements *(mandatory)*

### Functional Requirements

#### Filter Builder

- **FR-001**: System MUST support filter conditions with AND logic (match ALL conditions) or OR logic (match ANY condition) at the top level.
- **FR-002**: System MUST allow users to add, remove, and edit individual filter conditions.
- **FR-003**: System MUST present type-appropriate operators for each attribute:
  - **String attributes**: =, ≠, contains, does not contain, starts with, does not start with, ends with, does not end with, is empty, is not empty
  - **Date attributes**: is (exact date), is within, is before, is after, is on or before, is on or after, is empty, is not empty
  - **Boolean attributes**: is true, is false
  - **Numeric/ID attributes**: =, ≠, >, <, ≥, ≤, is empty, is not empty
  - **Status/Priority selection**: is, is not, is any of, is none of
  - **Priority comparison**: greater than, less than, greater than or equal, less than or equal (based on importance order)
  - **User selection**: is, is not, is any of, is none of, is empty (unassigned), is not empty (assigned)
- **FR-004**: System MUST present type-appropriate value inputs:
  - String fields: text input
  - Date fields with "is within": dropdown with relative options (today, yesterday, this week, last week, this month, last month, X days ago, X days from now, exact date picker)
  - Date fields with exact: date picker
  - Status: multi-select dropdown with all statuses plus "Open" and "Closed" meta-groups
  - Priority: dropdown with all priorities
  - User: searchable dropdown with all staff (showing "(deactivated)" suffix for suspended users)
  - Boolean: Yes/No toggle or dropdown
- **FR-005**: System MUST support these filterable attributes:
  - Ticket ID (numeric)
  - Contact ID (numeric)
  - Status (selection with meta-groups)
  - Assignee (user selection)
  - Priority (selection with comparison operators)
  - Created By (user selection)
  - Created At (date)
  - Updated At (date)
  - Closed At (date)
  - SLA Breached (boolean)
  - SLA Due At (date)
  - Has Contact (boolean)
  - Has Attachments (boolean)
  - Title (string)
  - Description (string)
  - Category (string)
  - Tags (string)
  - Contact First Name (string)
  - Contact Last Name (string)
  - Contact Email (string)
  - Contact Phone (string)
  - Contact Organization (string)
- **FR-006**: System MUST layer top-bar filters on top of view's base filters using AND logic.
- **FR-007**: System MUST preserve the ability for existing quick filters (status, priority, assignee dropdowns) to work with advanced views.

#### Multi-Level Sorting

- **FR-008**: System MUST allow configuration of multiple sort levels with field and direction for each.
- **FR-009**: System MUST support the same attributes for sorting as are available for filtering.
- **FR-010**: System MUST display the view's sort order as the first option in the sort pills, formatted as "Field1 ↑/↓, Field2 ↑/↓".
- **FR-011**: System MUST allow users to temporarily override the view's sort by selecting other sort pills.
- **FR-012**: System MUST allow users to return to the view's default sort by clicking the view sort pill.
- **FR-013**: System MUST allow reordering of sort levels via drag-and-drop in the configuration UI.

#### Column Selection and Ordering

- **FR-014**: System MUST allow users to select which columns appear in the view from available attributes.
- **FR-015**: System MUST allow users to reorder selected columns via drag-and-drop.
- **FR-016**: System MUST display columns in the configured order on the ticket list.
- **FR-017**: System MUST limit search to only the columns selected in the view.
- **FR-018**: System MUST make Contact ID column values clickable links to contact detail pages.
- **FR-019**: System MUST make Ticket ID and Title column values clickable links to ticket detail pages with proper back-to-list URL handling.
- **FR-020**: Available columns MUST include the same attributes as filtering plus: Assignee Name, Team Name, Contact Name, SLA Status.

#### Admin System Views

- **FR-021**: System MUST provide the same advanced view configuration capabilities in the Admin System Views section.
- **FR-022**: System MUST allow admins to mark system views as default for all users.
- **FR-023**: System views MUST appear in staff users' view selectors.

#### UX Requirements

- **FR-024**: Filter builder UI MUST clearly show the AND/OR logic being applied.
- **FR-025**: Drag-and-drop interactions MUST provide visual feedback during drag operations.
- **FR-026**: Filter conditions MUST validate inputs and show errors inline.
- **FR-027**: Date picker MUST respect the user's configured timezone.
- **FR-028**: Column selection MUST show currently selected columns visually distinct from unselected.

### Key Entities

- **TicketView**: Enhanced to store complex filter conditions (JSON structure with nested AND/OR groups), multi-level sort configuration, and ordered column list.
- **ViewConditions**: Updated to support nested condition groups with logic operators.
- **ViewFilterCondition**: Extended with additional operator types and value formats for dates, users, and comparisons.
- **ViewSortLevel**: New structure to represent a single level in multi-level sorting (field, direction, order).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create views with up to 20 filter conditions using AND/OR logic in under 2 minutes.
- **SC-002**: Date-based filters with relative dates (e.g., "within last 7 days") correctly return tickets based on the evaluation time.
- **SC-003**: Multi-level sorting correctly orders tickets first by primary sort, then by secondary, and so on.
- **SC-004**: Search functionality respects column selection, only searching displayed columns.
- **SC-005**: Column reordering via drag-and-drop provides immediate visual feedback with smooth animations.
- **SC-006**: All filter attribute types (string, date, boolean, numeric, selection, user) have appropriate operators and value inputs.
- **SC-007**: Staff users and admins have feature parity for view configuration capabilities.
- **SC-008**: Views with invalid filter references (deleted statuses, users) gracefully degrade and display appropriate indicators.
- **SC-009**: Top-bar filters successfully layer with view base filters without conflicts.
- **SC-010**: 95% of users can configure a complex view (5+ filters, 2+ sort levels, custom columns) without assistance.

## Assumptions

- The existing `TicketView` entity structure can be extended with additional JSON fields without requiring major schema changes.
- The current `ViewFilterBuilder` service can be extended to support the new operator types and nested logic.
- Drag-and-drop functionality will be implemented using minimal JavaScript, potentially with a lightweight library, in line with the Razor Pages First principle.
- Timezone handling for relative dates will use the organization's configured timezone from `OrganizationSettings`.
- Performance is acceptable for views with up to 20 filter conditions on datasets of 100,000+ tickets.
- The existing Staff area UI patterns (staff-card, staff-table, etc.) will be used for consistency.
