# Quickstart: Advanced Views

**Feature Branch**: `003-advanced-views`  
**Date**: 2025-12-13

## Overview

This guide covers implementing the Advanced Views feature: enhanced filtering with AND/OR logic, multi-level sorting, and drag-drop column selection.

## Prerequisites

- .NET 8 SDK
- PostgreSQL (or Docker for local dev)
- Existing ticket-system codebase on `003-advanced-views` branch

## Quick Start

```bash
# Ensure you're on the feature branch
git checkout 003-advanced-views

# Restore dependencies
dotnet restore

# Run database migrations (after implementing)
dotnet ef database update --project src/App.Infrastructure --startup-project src/App.Web

# Run the application
dotnet run --project src/App.Web
```

## Implementation Order

### Phase 1: Data Model & Backend (P1 - Filtering)

1. **Add SortLevelsJson to TicketView entity** (`src/App.Domain/Entities/TicketView.cs`)
2. **Create migration** for new column
3. **Extend ViewConditions & ViewFilterCondition** (`src/App.Application/TicketViews/TicketViewDto.cs`)
4. **Add FilterAttributes registry** (`src/App.Application/TicketViews/FilterAttributeDefinition.cs`)
5. **Enhance ViewFilterBuilder** with new operators and AND/OR logic

### Phase 2: UI Components (P1-P3)

1. **Create _FilterBuilder.cshtml partial** - Dynamic filter condition rows
2. **Create _SortConfigurator.cshtml partial** - Multi-level sort configuration
3. **Create _ColumnSelector.cshtml partial** - Drag-drop column selection
4. **Add minimal JavaScript** for dynamic rows and SortableJS initialization

### Phase 3: Page Integration

1. **Update Staff Views/Create.cshtml** - Use new partials
2. **Update Staff Views/Edit.cshtml** - Use new partials
3. **Update Admin SystemViews/Create.cshtml** - Use new partials
4. **Update Admin SystemViews/Edit.cshtml** - Use new partials

### Phase 4: List View Integration

1. **Update Tickets/Index.cshtml.cs** - Apply view sort, handle sort pill override
2. **Update Tickets/Index.cshtml** - Display view's sort pill, column rendering

## Key Files to Modify

### Domain Layer

```
src/App.Domain/Entities/TicketView.cs
- Add: SortLevelsJson property
- Add: SortLevels computed property with JSON deserialization
```

### Application Layer

```
src/App.Application/TicketViews/
├── TicketViewDto.cs          # Add ViewSortLevel, enhance ViewFilterCondition
├── FilterAttributeDefinition.cs  # NEW: Attribute and operator definitions
├── Commands/
│   ├── CreateTicketView.cs   # Accept SortLevels, enhanced Conditions
│   └── UpdateTicketView.cs   # Accept SortLevels, enhanced Conditions
└── Services/
    └── ViewFilterBuilder.cs  # Implement new operators, AND/OR logic
```

### Web Layer

```
src/App.Web/Areas/
├── Shared/
│   └── js/
│       ├── filter-builder.js      # NEW: Dynamic filter row management
│       └── sort-configurator.js   # NEW: Sort level management
├── Staff/
│   └── Pages/
│       ├── Views/
│       │   ├── Create.cshtml(.cs)  # Use new partials
│       │   └── Edit.cshtml(.cs)    # Use new partials
│       ├── Tickets/
│       │   └── Index.cshtml(.cs)   # Apply view sorting, column rendering
│       └── Shared/
│           └── _Partials/
│               ├── _FilterBuilder.cshtml     # NEW
│               ├── _SortConfigurator.cshtml  # NEW
│               └── _ColumnSelector.cshtml    # NEW
└── Admin/
    └── Pages/
        └── Tickets/
            └── SystemViews/
                ├── Create.cshtml(.cs)  # Use new partials
                └── Edit.cshtml(.cs)    # Use new partials
```

## ViewFilterBuilder Implementation Guide

### AND/OR Logic

```csharp
public IQueryable<Ticket> ApplyFilters(
    IQueryable<Ticket> query, 
    ViewConditions? conditions,
    Guid? currentUserId = null)
{
    if (conditions == null || !conditions.Filters.Any())
        return query;

    if (conditions.Logic == "OR")
    {
        // Build OR expression combining all conditions
        var predicate = PredicateBuilder.False<Ticket>();
        foreach (var filter in conditions.Filters)
        {
            predicate = predicate.Or(BuildFilterExpression(filter));
        }
        return query.Where(predicate);
    }
    else
    {
        // Default AND logic - apply each filter sequentially
        foreach (var filter in conditions.Filters)
        {
            query = ApplyFilter(query, filter);
        }
        return query;
    }
}
```

### New String Operators

```csharp
private IQueryable<Ticket> ApplyStringOperator(
    IQueryable<Ticket> query,
    ViewFilterCondition filter,
    Expression<Func<Ticket, string?>> fieldSelector)
{
    return filter.Operator switch
    {
        "eq" => query.Where(t => EF.Property<string>(t, filter.Field) == filter.Value),
        "neq" => query.Where(t => EF.Property<string>(t, filter.Field) != filter.Value),
        "contains" => query.Where(t => EF.Property<string>(t, filter.Field).Contains(filter.Value)),
        "not_contains" => query.Where(t => !EF.Property<string>(t, filter.Field).Contains(filter.Value)),
        "starts_with" => query.Where(t => EF.Property<string>(t, filter.Field).StartsWith(filter.Value)),
        "ends_with" => query.Where(t => EF.Property<string>(t, filter.Field).EndsWith(filter.Value)),
        "is_empty" => query.Where(t => string.IsNullOrEmpty(EF.Property<string>(t, filter.Field))),
        "is_not_empty" => query.Where(t => !string.IsNullOrEmpty(EF.Property<string>(t, filter.Field))),
        _ => query
    };
}
```

### Relative Date Handling

```csharp
private DateTime ResolveRelativeDate(ViewFilterCondition filter, string orgTimeZone)
{
    var tz = TimeZoneInfo.FindSystemTimeZoneById(orgTimeZone);
    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    
    return filter.RelativeDatePreset switch
    {
        "today" => now.Date,
        "yesterday" => now.Date.AddDays(-1),
        "this_week" => now.Date.AddDays(-(int)now.DayOfWeek),
        "last_week" => now.Date.AddDays(-(int)now.DayOfWeek - 7),
        "this_month" => new DateTime(now.Year, now.Month, 1),
        "last_month" => new DateTime(now.Year, now.Month, 1).AddMonths(-1),
        "days_ago" => now.Date.AddDays(-(filter.RelativeDateValue ?? 0)),
        "days_from_now" => now.Date.AddDays(filter.RelativeDateValue ?? 0),
        _ => DateTime.Parse(filter.Value ?? DateTime.UtcNow.ToString())
    };
}
```

### Priority Comparison

```csharp
private IQueryable<Ticket> ApplyPriorityComparison(
    IQueryable<Ticket> query,
    ViewFilterCondition filter)
{
    var threshold = TicketPriority.From(filter.Value).SortOrder;
    
    return filter.Operator switch
    {
        "gt" => query.Where(t => GetPrioritySortOrder(t.Priority) > threshold),
        "lt" => query.Where(t => GetPrioritySortOrder(t.Priority) < threshold),
        "gte" => query.Where(t => GetPrioritySortOrder(t.Priority) >= threshold),
        "lte" => query.Where(t => GetPrioritySortOrder(t.Priority) <= threshold),
        _ => query
    };
}

// Note: May need to translate priority string to sort order in database
// Could add computed column or use CASE WHEN in raw SQL for performance
```

## JavaScript Patterns

### Filter Builder (Minimal JS)

```javascript
// filter-builder.js
const FilterBuilder = {
    init(container) {
        this.container = container;
        this.template = container.querySelector('[data-filter-template]');
        this.bindEvents();
    },
    
    bindEvents() {
        this.container.addEventListener('click', (e) => {
            if (e.target.matches('[data-add-condition]')) {
                this.addCondition();
            }
            if (e.target.matches('[data-remove-condition]')) {
                this.removeCondition(e.target.closest('[data-condition-row]'));
            }
        });
        
        this.container.addEventListener('change', (e) => {
            if (e.target.matches('[data-field-select]')) {
                this.updateOperators(e.target);
            }
            if (e.target.matches('[data-operator-select]')) {
                this.updateValueInput(e.target);
            }
        });
    },
    
    addCondition() {
        const clone = this.template.content.cloneNode(true);
        const index = this.container.querySelectorAll('[data-condition-row]').length;
        // Update name attributes with index
        clone.querySelectorAll('[name]').forEach(el => {
            el.name = el.name.replace('[INDEX]', `[${index}]`);
        });
        this.container.querySelector('[data-conditions-list]').appendChild(clone);
        this.updateIndices();
    },
    
    removeCondition(row) {
        row.remove();
        this.updateIndices();
    }
};
```

### SortableJS Integration

```javascript
// sort-configurator.js
document.addEventListener('DOMContentLoaded', () => {
    const sortList = document.querySelector('[data-sort-list]');
    if (sortList) {
        new Sortable(sortList, {
            handle: '.drag-handle',
            animation: 150,
            onEnd: () => updateSortOrder()
        });
    }
    
    const columnList = document.querySelector('[data-column-list]');
    if (columnList) {
        new Sortable(columnList, {
            handle: '.drag-handle',
            animation: 150,
            onEnd: () => updateColumnOrder()
        });
    }
});

function updateSortOrder() {
    document.querySelectorAll('[data-sort-item]').forEach((item, index) => {
        item.querySelector('[data-sort-order]').value = index;
    });
}

function updateColumnOrder() {
    document.querySelectorAll('[data-column-item]').forEach((item, index) => {
        item.querySelector('[data-column-order]').value = index;
    });
}
```

## Testing

### Unit Tests for ViewFilterBuilder

```csharp
public class ViewFilterBuilderTests
{
    [Fact]
    public void ApplyFilters_WithOrLogic_ReturnsMatchingAnyCondition()
    {
        // Arrange
        var builder = new ViewFilterBuilder();
        var conditions = new ViewConditions
        {
            Logic = "OR",
            Filters = new List<ViewFilterCondition>
            {
                new() { Field = "Status", Operator = "is", Value = "open" },
                new() { Field = "Status", Operator = "is", Value = "closed" }
            }
        };
        
        // Act & Assert - tickets with status open OR closed should match
    }
    
    [Fact]
    public void ApplyFilters_WithPriorityGreaterThan_ReturnsHigherPriorities()
    {
        // Arrange
        var conditions = new ViewConditions
        {
            Filters = new List<ViewFilterCondition>
            {
                new() { Field = "Priority", Operator = "gt", Value = "normal" }
            }
        };
        
        // Assert - should return Urgent and High, not Normal or Low
    }
    
    [Fact]
    public void ApplyFilters_WithRelativeDate_EvaluatesAtQueryTime()
    {
        // Test that "7 days ago" is calculated correctly
    }
}
```

## Common Patterns

### Form Array Binding

Use indexed names for binding filter arrays:

```html
<input name="Conditions.Filters[0].Field" value="Status" />
<input name="Conditions.Filters[0].Operator" value="is" />
<input name="Conditions.Filters[0].Value" value="open" />
```

### Cascading Selects

When attribute changes, update available operators via:
1. Data attributes on attribute options containing valid operators
2. JavaScript reads data and updates operator dropdown
3. No AJAX needed - all operator data embedded in page

```html
<option value="Status" data-operators="is,is_not,is_any_of,is_none_of">Status</option>
<option value="Title" data-operators="eq,neq,contains,not_contains,starts_with,ends_with,is_empty,is_not_empty">Title</option>
```

## Troubleshooting

### Filter Not Applied

1. Check that `ConditionsJson` is being serialized correctly
2. Verify operator string matches handler switch cases
3. Check browser console for JavaScript errors in filter builder

### Sort Not Working

1. Ensure `SortLevelsJson` is populated
2. Check that field names match entity properties
3. Verify migration was applied

### Drag-Drop Not Working

1. Ensure SortableJS is loaded (`/admin/lib/sortablejs/Sortable.min.js`)
2. Check container has correct data attribute
3. Verify hidden inputs are being updated on sort end

