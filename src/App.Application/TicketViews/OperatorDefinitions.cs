namespace App.Application.TicketViews;

/// <summary>
/// Operator definitions for each attribute type.
/// </summary>
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

    /// <summary>
    /// Get operators for a given attribute type.
    /// </summary>
    public static List<FilterOperator> GetOperatorsForType(string type)
    {
        return type.ToLower() switch
        {
            "string" => StringOperators,
            "date" => DateOperators,
            "boolean" => BooleanOperators,
            "numeric" => NumericOperators,
            "selection" => SelectionOperators,
            "priority" => PriorityOperators,
            "user" => UserOperators,
            "team" => SelectionOperators,
            _ => StringOperators
        };
    }
}

