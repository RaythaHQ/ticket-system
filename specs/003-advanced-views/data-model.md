# Data Model: Advanced Views

**Feature Branch**: `003-advanced-views`  
**Date**: 2025-12-13

## Entity Changes

### TicketView (Modified)

The existing `TicketView` entity is extended with a new JSON column for multi-level sorting.

```csharp
// Existing fields (unchanged)
public string Name { get; set; } = null!;
public string? Description { get; set; }
public Guid? OwnerStaffId { get; set; }
public bool IsDefault { get; set; }
public bool IsSystem { get; set; }
public string? ConditionsJson { get; set; }  // Contains ViewConditions
public string? VisibleColumnsJson { get; set; }

// DEPRECATED - will be migrated to SortLevelsJson
public string? SortPrimaryField { get; set; }
public string? SortPrimaryDirection { get; set; }
public string? SortSecondaryField { get; set; }
public string? SortSecondaryDirection { get; set; }

// NEW: Multi-level sorting stored as JSON array
public string? SortLevelsJson { get; set; }

[NotMapped]
public List<ViewSortLevel> SortLevels
{
    get => string.IsNullOrEmpty(SortLevelsJson)
        ? MigrateLegacySort()  // Backward compat
        : JsonSerializer.Deserialize<List<ViewSortLevel>>(SortLevelsJson) ?? new();
    set => SortLevelsJson = JsonSerializer.Serialize(value);
}

private List<ViewSortLevel> MigrateLegacySort()
{
    var levels = new List<ViewSortLevel>();
    if (!string.IsNullOrEmpty(SortPrimaryField))
        levels.Add(new ViewSortLevel { Order = 0, Field = SortPrimaryField, Direction = SortPrimaryDirection ?? "asc" });
    if (!string.IsNullOrEmpty(SortSecondaryField))
        levels.Add(new ViewSortLevel { Order = 1, Field = SortSecondaryField, Direction = SortSecondaryDirection ?? "asc" });
    return levels;
}
```

**Migration Strategy**: The legacy `SortPrimaryField`/`SortSecondaryField` columns will be kept for backward compatibility. The application will prefer `SortLevelsJson` when present, falling back to legacy columns. A future migration can remove the deprecated columns.

---

## Value Objects (Application Layer)

### ViewConditions (Modified)

```csharp
/// <summary>
/// Filter conditions for a ticket view. Supports AND/OR logic at top level.
/// </summary>
public record ViewConditions
{
    /// <summary>
    /// Logical operator for combining conditions: "AND" or "OR"
    /// </summary>
    public string Logic { get; init; } = "AND";
    
    /// <summary>
    /// List of filter conditions to apply
    /// </summary>
    public List<ViewFilterCondition> Filters { get; init; } = new();
}
```

### ViewFilterCondition (Enhanced)

```csharp
/// <summary>
/// Individual filter condition with type-aware operators and values.
/// </summary>
public record ViewFilterCondition
{
    /// <summary>
    /// Field/attribute to filter on (e.g., "Status", "CreatedAt", "Title")
    /// </summary>
    public string Field { get; init; } = null!;
    
    /// <summary>
    /// Operator to apply. Valid operators depend on field type.
    /// String: eq, neq, contains, not_contains, starts_with, not_starts_with, ends_with, not_ends_with, is_empty, is_not_empty
    /// Date: is, is_within, is_before, is_after, is_on_or_before, is_on_or_after, is_empty, is_not_empty
    /// Boolean: is_true, is_false
    /// Numeric: eq, neq, gt, lt, gte, lte, is_empty, is_not_empty
    /// Selection: is, is_not, is_any_of, is_none_of
    /// Priority: is, is_not, gt, lt, gte, lte (based on SortOrder)
    /// User: is, is_not, is_any_of, is_none_of, is_empty, is_not_empty
    /// </summary>
    public string Operator { get; init; } = "eq";
    
    /// <summary>
    /// Single value for operators like eq, contains, etc.
    /// </summary>
    public string? Value { get; init; }
    
    /// <summary>
    /// Multiple values for operators like is_any_of, is_none_of
    /// </summary>
    public List<string>? Values { get; init; }
    
    /// <summary>
    /// For date fields: "exact", "relative", or null
    /// </summary>
    public string? DateType { get; init; }
    
    /// <summary>
    /// For relative dates: the unit (days, weeks, months)
    /// </summary>
    public string? RelativeDateUnit { get; init; }
    
    /// <summary>
    /// For relative dates: the value (negative for past, positive for future)
    /// e.g., -7 with unit "days" = 7 days ago
    /// </summary>
    public int? RelativeDateValue { get; init; }
    
    /// <summary>
    /// For relative dates: preset like "today", "yesterday", "this_week", "last_week", etc.
    /// </summary>
    public string? RelativeDatePreset { get; init; }
}
```

### ViewSortLevel (New)

```csharp
/// <summary>
/// Represents a single level in multi-level sorting.
/// </summary>
public record ViewSortLevel
{
    /// <summary>
    /// Sort order (0 = primary, 1 = secondary, etc.)
    /// </summary>
    public int Order { get; init; }
    
    /// <summary>
    /// Field to sort by
    /// </summary>
    public string Field { get; init; } = null!;
    
    /// <summary>
    /// Sort direction: "asc" or "desc"
    /// </summary>
    public string Direction { get; init; } = "asc";
}
```

---

## Filter Attribute Definitions

### FilterAttributeDefinition (New)

```csharp
/// <summary>
/// Defines a filterable/sortable attribute with its type and available operators.
/// </summary>
public record FilterAttributeDefinition
{
    public string Field { get; init; } = null!;
    public string Label { get; init; } = null!;
    public string Type { get; init; } = null!;  // string, date, boolean, numeric, selection, priority, user
    public List<FilterOperator> Operators { get; init; } = new();
    public bool IsSortable { get; init; } = true;
    public bool IsColumnVisible { get; init; } = true;
}

public record FilterOperator
{
    public string Value { get; init; } = null!;
    public string Label { get; init; } = null!;
    public bool RequiresValue { get; init; } = true;
    public bool AllowsMultipleValues { get; init; } = false;
}
```

### Attribute Registry

```csharp
public static class FilterAttributes
{
    public static readonly IReadOnlyList<FilterAttributeDefinition> All = new List<FilterAttributeDefinition>
    {
        // Ticket Core
        new() { Field = "Id", Label = "Ticket ID", Type = "numeric", Operators = NumericOperators },
        new() { Field = "Title", Label = "Title", Type = "string", Operators = StringOperators },
        new() { Field = "Description", Label = "Description", Type = "string", Operators = StringOperators, IsColumnVisible = false },
        new() { Field = "Category", Label = "Category", Type = "string", Operators = StringOperators },
        new() { Field = "Tags", Label = "Tags", Type = "string", Operators = StringOperators },
        
        // Status & Priority
        new() { Field = "Status", Label = "Status", Type = "selection", Operators = SelectionOperators },
        new() { Field = "Priority", Label = "Priority", Type = "priority", Operators = PriorityOperators },
        
        // Relationships
        new() { Field = "AssigneeId", Label = "Assignee", Type = "user", Operators = UserOperators },
        new() { Field = "CreatedByStaffId", Label = "Created By", Type = "user", Operators = UserOperators },
        new() { Field = "OwningTeamId", Label = "Team", Type = "selection", Operators = SelectionOperators },
        new() { Field = "ContactId", Label = "Contact ID", Type = "numeric", Operators = NumericOperators },
        
        // Dates
        new() { Field = "CreationTime", Label = "Created At", Type = "date", Operators = DateOperators },
        new() { Field = "LastModificationTime", Label = "Updated At", Type = "date", Operators = DateOperators },
        new() { Field = "ClosedAt", Label = "Closed At", Type = "date", Operators = DateOperators },
        new() { Field = "SlaDueAt", Label = "SLA Due At", Type = "date", Operators = DateOperators },
        
        // Booleans
        new() { Field = "SlaBreached", Label = "SLA Breached", Type = "boolean", Operators = BooleanOperators },
        new() { Field = "HasContact", Label = "Has Contact", Type = "boolean", Operators = BooleanOperators },
        new() { Field = "HasAttachments", Label = "Has Attachments", Type = "boolean", Operators = BooleanOperators },
        
        // Contact Fields
        new() { Field = "Contact.FirstName", Label = "Contact First Name", Type = "string", Operators = StringOperators },
        new() { Field = "Contact.LastName", Label = "Contact Last Name", Type = "string", Operators = StringOperators },
        new() { Field = "Contact.EmailAddress", Label = "Contact Email", Type = "string", Operators = StringOperators },
        new() { Field = "Contact.PhoneNumber", Label = "Contact Phone", Type = "string", Operators = StringOperators },
        new() { Field = "Contact.Organization", Label = "Contact Organization", Type = "string", Operators = StringOperators },
    };
    
    // Display columns (derived from Contact joins, Assignee joins, etc.)
    public static readonly IReadOnlyList<ColumnDefinition> Columns = new List<ColumnDefinition>
    {
        new() { Field = "Id", Label = "Ticket ID", IsClickable = true, ClickTarget = "ticket" },
        new() { Field = "Title", Label = "Title", IsClickable = true, ClickTarget = "ticket" },
        new() { Field = "Status", Label = "Status" },
        new() { Field = "Priority", Label = "Priority" },
        new() { Field = "Category", Label = "Category" },
        new() { Field = "AssigneeName", Label = "Assignee" },
        new() { Field = "OwningTeamName", Label = "Team" },
        new() { Field = "ContactId", Label = "Contact ID", IsClickable = true, ClickTarget = "contact" },
        new() { Field = "ContactName", Label = "Contact" },
        new() { Field = "SlaStatus", Label = "SLA Status" },
        new() { Field = "SlaDueAt", Label = "SLA Due" },
        new() { Field = "CreationTime", Label = "Created" },
        new() { Field = "LastModificationTime", Label = "Last Updated" },
        new() { Field = "ClosedAt", Label = "Closed" },
        new() { Field = "Tags", Label = "Tags" },
        new() { Field = "CreatedByName", Label = "Created By" },
    };
}
```

---

## Operator Definitions

```csharp
public static class OperatorDefinitions
{
    public static readonly List<FilterOperator> StringOperators = new()
    {
        new() { Value = "eq", Label = "equals", RequiresValue = true },
        new() { Value = "neq", Label = "does not equal", RequiresValue = true },
        new() { Value = "contains", Label = "contains", RequiresValue = true },
        new() { Value = "not_contains", Label = "does not contain", RequiresValue = true },
        new() { Value = "starts_with", Label = "starts with", RequiresValue = true },
        new() { Value = "not_starts_with", Label = "does not start with", RequiresValue = true },
        new() { Value = "ends_with", Label = "ends with", RequiresValue = true },
        new() { Value = "not_ends_with", Label = "does not end with", RequiresValue = true },
        new() { Value = "is_empty", Label = "is empty", RequiresValue = false },
        new() { Value = "is_not_empty", Label = "is not empty", RequiresValue = false },
    };
    
    public static readonly List<FilterOperator> DateOperators = new()
    {
        new() { Value = "is", Label = "is", RequiresValue = true },
        new() { Value = "is_within", Label = "is within", RequiresValue = true },
        new() { Value = "is_before", Label = "is before", RequiresValue = true },
        new() { Value = "is_after", Label = "is after", RequiresValue = true },
        new() { Value = "is_on_or_before", Label = "is on or before", RequiresValue = true },
        new() { Value = "is_on_or_after", Label = "is on or after", RequiresValue = true },
        new() { Value = "is_empty", Label = "is empty", RequiresValue = false },
        new() { Value = "is_not_empty", Label = "is not empty", RequiresValue = false },
    };
    
    public static readonly List<FilterOperator> BooleanOperators = new()
    {
        new() { Value = "is_true", Label = "is Yes", RequiresValue = false },
        new() { Value = "is_false", Label = "is No", RequiresValue = false },
    };
    
    public static readonly List<FilterOperator> NumericOperators = new()
    {
        new() { Value = "eq", Label = "=", RequiresValue = true },
        new() { Value = "neq", Label = "≠", RequiresValue = true },
        new() { Value = "gt", Label = ">", RequiresValue = true },
        new() { Value = "lt", Label = "<", RequiresValue = true },
        new() { Value = "gte", Label = "≥", RequiresValue = true },
        new() { Value = "lte", Label = "≤", RequiresValue = true },
        new() { Value = "is_empty", Label = "is empty", RequiresValue = false },
        new() { Value = "is_not_empty", Label = "is not empty", RequiresValue = false },
    };
    
    public static readonly List<FilterOperator> SelectionOperators = new()
    {
        new() { Value = "is", Label = "is", RequiresValue = true },
        new() { Value = "is_not", Label = "is not", RequiresValue = true },
        new() { Value = "is_any_of", Label = "is any of", RequiresValue = true, AllowsMultipleValues = true },
        new() { Value = "is_none_of", Label = "is none of", RequiresValue = true, AllowsMultipleValues = true },
    };
    
    public static readonly List<FilterOperator> PriorityOperators = new()
    {
        new() { Value = "is", Label = "is", RequiresValue = true },
        new() { Value = "is_not", Label = "is not", RequiresValue = true },
        new() { Value = "is_any_of", Label = "is any of", RequiresValue = true, AllowsMultipleValues = true },
        new() { Value = "gt", Label = "is higher than", RequiresValue = true },
        new() { Value = "lt", Label = "is lower than", RequiresValue = true },
        new() { Value = "gte", Label = "is at least", RequiresValue = true },
        new() { Value = "lte", Label = "is at most", RequiresValue = true },
    };
    
    public static readonly List<FilterOperator> UserOperators = new()
    {
        new() { Value = "is", Label = "is", RequiresValue = true },
        new() { Value = "is_not", Label = "is not", RequiresValue = true },
        new() { Value = "is_any_of", Label = "is any of", RequiresValue = true, AllowsMultipleValues = true },
        new() { Value = "is_none_of", Label = "is none of", RequiresValue = true, AllowsMultipleValues = true },
        new() { Value = "is_empty", Label = "is unassigned", RequiresValue = false },
        new() { Value = "is_not_empty", Label = "is assigned", RequiresValue = false },
    };
}
```

---

## Relative Date Presets

```csharp
public static class RelativeDatePresets
{
    public static readonly List<(string Value, string Label, int Days)> Presets = new()
    {
        ("today", "today", 0),
        ("yesterday", "yesterday", -1),
        ("tomorrow", "tomorrow", 1),
        ("this_week", "this week", 0),  // Special handling
        ("last_week", "last week", -7),  // Special handling
        ("this_month", "this month", 0),  // Special handling
        ("last_month", "last month", -30),  // Special handling
        ("days_ago", "number of days ago...", 0),  // Custom input
        ("days_from_now", "number of days from now...", 0),  // Custom input
        ("exact_date", "exact date...", 0),  // Date picker
    };
}
```

---

## Database Migration

```csharp
// Migration: AddSortLevelsToTicketView
public partial class AddSortLevelsToTicketView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SortLevelsJson",
            table: "TicketViews",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SortLevelsJson",
            table: "TicketViews");
    }
}
```

---

## Validation Rules

### ViewConditions Validation

- `Logic` must be "AND" or "OR"
- `Filters` must contain at most 20 conditions
- Each condition must have a valid `Field` from the attribute registry
- Each condition must have a valid `Operator` for its field type
- Value requirements depend on operator (some operators don't require values)

### ViewSortLevel Validation

- `Field` must be from the sortable attribute list
- `Direction` must be "asc" or "desc"
- Maximum 6 sort levels
- No duplicate fields in sort levels

### VisibleColumns Validation

- At least one column must be selected
- Maximum 20 columns
- All column names must be from the column registry

