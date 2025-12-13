# Research: Advanced Views

**Feature Branch**: `003-advanced-views`  
**Date**: 2025-12-13

## Research Topics

### 1. AND/OR Filter Logic Implementation

**Decision**: Implement top-level AND/OR logic (not nested groups) using a `Logic` property in `ViewConditions`.

**Rationale**: 
- The spec requires "Match ALL conditions" or "Match ANY condition" at the top level
- Nested groups (e.g., "(A AND B) OR (C AND D)") add significant complexity without clear user demand
- Top-level logic covers 95%+ of real-world use cases
- Simpler UX: single toggle instead of complex group management
- Can extend to nested groups in future if needed

**Alternatives Considered**:
1. **Full nested groups**: Too complex for initial implementation; would require recursive JSON structure and complex UI
2. **SQL-like query builder**: Over-engineered for ticket filtering use case
3. **Pre-defined filter templates**: Too limiting for power users

**Implementation Approach**:
```csharp
public record ViewConditions
{
    public string Logic { get; init; } = "AND"; // "AND" or "OR"
    public List<ViewFilterCondition> Filters { get; init; } = new();
}
```

When `Logic = "OR"`, the `ViewFilterBuilder` will combine filter expressions with `||` instead of `&&`.

---

### 2. Relative Date Filter Evaluation

**Decision**: Evaluate relative dates at query time using the organization's timezone setting.

**Rationale**:
- Filters like "last 7 days" must be evaluated when the query runs, not when saved
- Organization timezone ensures consistent behavior across users
- Follows Airtable/Notion pattern that users expect

**Alternatives Considered**:
1. **User-specific timezone**: More complex, inconsistent results across team members
2. **UTC only**: Confusing for end users ("last 7 days" wouldn't match expectations)
3. **Store as absolute dates**: Would require nightly recalculation jobs

**Implementation Approach**:
Store relative date type and value in the filter condition:
```csharp
public record ViewFilterCondition
{
    public string Field { get; init; } = null!;
    public string Operator { get; init; } = "equals";
    public string? Value { get; init; }
    public List<string>? Values { get; init; }
    public string? DateType { get; init; }  // "exact", "relative", "range"
    public string? RelativeDateUnit { get; init; }  // "days", "weeks", "months"
    public int? RelativeDateValue { get; init; }  // e.g., -7 for "7 days ago"
}
```

At query time, the `ViewFilterBuilder` converts relative dates to absolute dates using the organization timezone.

---

### 3. Priority Comparison Operators

**Decision**: Use `TicketPriority.SortOrder` for priority comparison (higher SortOrder = higher importance).

**Rationale**:
- Existing `TicketPriority` value object already has `SortOrder` (Urgent=4, High=3, Normal=2, Low=1)
- "Greater than Normal" logically means Urgent or High
- Consistent with user mental model of priority importance

**Alternatives Considered**:
1. **Alphabetical comparison**: Confusing (High < Low alphabetically)
2. **Numeric value in database**: Already have SortOrder for this purpose
3. **Separate importance field**: Redundant with SortOrder

**Implementation Approach**:
```csharp
case "priority_gt":
    var threshold = TicketPriority.From(filter.Value).SortOrder;
    return query.Where(t => TicketPriority.From(t.Priority).SortOrder > threshold);
```

Note: For database efficiency, we may need to store `PrioritySortOrder` as a computed column or use raw SQL for these comparisons.

---

### 4. Multi-Level Sort JSON Structure

**Decision**: Store sort levels as JSON array with order, field, and direction.

**Rationale**:
- Allows unlimited sort levels (practical limit: 5-6)
- Single JSON column instead of multiple nullable columns
- Order is explicit rather than implicit
- Consistent with the pattern used for `VisibleColumnsJson`

**Alternatives Considered**:
1. **Separate columns per level**: Limited to fixed number (currently 2)
2. **Comma-separated string**: Harder to parse and validate
3. **Normalized table**: Over-engineered for this use case

**Implementation Approach**:
```csharp
public record ViewSortLevel
{
    public int Order { get; init; }
    public string Field { get; init; } = null!;
    public string Direction { get; init; } = "asc"; // "asc" or "desc"
}

// In TicketView entity:
public string? SortLevelsJson { get; set; }

[NotMapped]
public List<ViewSortLevel> SortLevels
{
    get => string.IsNullOrEmpty(SortLevelsJson)
        ? new List<ViewSortLevel>()
        : JsonSerializer.Deserialize<List<ViewSortLevel>>(SortLevelsJson) ?? new();
    set => SortLevelsJson = JsonSerializer.Serialize(value);
}
```

---

### 5. Drag-and-Drop Library Selection

**Decision**: Use SortableJS (already included in the codebase).

**Rationale**:
- Already available at `/admin/lib/sortablejs/Sortable.min.js`
- Battle-tested, touch-friendly, accessible
- No additional dependencies required
- Works well with minimal JavaScript approach

**Alternatives Considered**:
1. **Native HTML5 Drag and Drop**: Poor mobile support, complex implementation
2. **jQuery UI Sortable**: Would add jQuery dependency
3. **dnd-kit or react-beautiful-dnd**: Requires React, violates Razor Pages First

**Implementation Approach**:
- Include SortableJS in Staff area (already in Admin)
- Initialize on column selector and sort configurator containers
- Use hidden input fields to track order on form submission

---

### 6. Filter Builder UI Pattern

**Decision**: Dynamic form rows with attribute → operator → value cascading selects.

**Rationale**:
- Follows established patterns (Airtable, Notion, Jira)
- Progressive disclosure: operator and value controls adapt to selected attribute
- Server-side rendering with minimal JavaScript for adding/removing rows

**Alternatives Considered**:
1. **Natural language query**: Too complex to parse reliably
2. **Fixed filter form**: Not flexible enough for 22+ attributes
3. **Code/query editor**: Too technical for non-developer users

**Implementation Approach**:
- Razor partial `_FilterBuilder.cshtml` renders condition rows
- JavaScript handles adding/removing rows and cascading selects
- Form posts array of conditions: `Filters[0].Field`, `Filters[0].Operator`, etc.
- Server validates and serializes to JSON

---

### 7. Status Meta-Groups ("Open" / "Closed")

**Decision**: Implement as special filter values that expand to status lists at query time.

**Rationale**:
- Users commonly want "all open tickets" without listing each status
- "Open" = Open, In Progress, Pending; "Closed" = Resolved, Closed
- Matches existing filter bar behavior in ticket list

**Alternatives Considered**:
1. **Separate checkbox**: Adds UI complexity
2. **StatusType field on Ticket**: Database schema change for limited benefit
3. **Pre-defined saved filters**: Less flexible

**Implementation Approach**:
```csharp
// In filter builder:
if (filter.Value == "__OPEN__")
{
    return query.Where(t => t.Status != TicketStatus.CLOSED && t.Status != TicketStatus.RESOLVED);
}
if (filter.Value == "__CLOSED__")
{
    return query.Where(t => t.Status == TicketStatus.CLOSED || t.Status == TicketStatus.RESOLVED);
}
```

---

### 8. Search Column Limitation

**Decision**: Extend existing `ApplyColumnSearch` method to use view's `VisibleColumns`.

**Rationale**:
- Already have `ApplyColumnSearch` in `ViewFilterBuilder` that accepts column list
- Just need to pass the view's configured columns instead of defaults
- No architectural changes needed

**Alternatives Considered**:
1. **Separate search fields list**: Redundant with visible columns
2. **Full-text search index**: Over-engineered for current scale
3. **Disable search when few columns**: Confusing UX

**Implementation Approach**:
Update ticket list query handler to pass `view.VisibleColumns` to `ApplyColumnSearch`:
```csharp
var query = _viewFilterBuilder.ApplyFilters(baseQuery, view.Conditions);
query = _viewFilterBuilder.ApplyColumnSearch(query, searchTerm, view.VisibleColumns);
```

---

## Resolved Unknowns Summary

| Topic | Decision |
|-------|----------|
| AND/OR Logic | Top-level only, single toggle |
| Relative Dates | Evaluate at query time using org timezone |
| Priority Comparison | Use SortOrder (Urgent=4 > Low=1) |
| Multi-Level Sort | JSON array with Order/Field/Direction |
| Drag-Drop Library | SortableJS (already available) |
| Filter Builder UI | Dynamic rows with cascading selects |
| Status Meta-Groups | Special values expanding to status lists |
| Search Columns | Use view's VisibleColumns |

All research complete. Ready for Phase 1 design.

