namespace App.Domain.ValueObjects;

/// <summary>
/// Filter operators for view conditions.
/// Organized by operator category (string, date, boolean, numeric, selection, priority, user).
/// </summary>
public class FilterOperator : ValueObject
{
    // String operators
    public const string EQ = "eq";
    public const string NEQ = "neq";
    public const string CONTAINS = "contains";
    public const string NOT_CONTAINS = "not_contains";
    public const string STARTS_WITH = "starts_with";
    public const string NOT_STARTS_WITH = "not_starts_with";
    public const string ENDS_WITH = "ends_with";
    public const string NOT_ENDS_WITH = "not_ends_with";
    public const string IS_EMPTY = "is_empty";
    public const string IS_NOT_EMPTY = "is_not_empty";

    // Date operators
    public const string IS = "is";
    public const string IS_WITHIN = "is_within";
    public const string IS_BEFORE = "is_before";
    public const string IS_AFTER = "is_after";
    public const string IS_ON_OR_BEFORE = "is_on_or_before";
    public const string IS_ON_OR_AFTER = "is_on_or_after";

    // Boolean operators
    public const string IS_TRUE = "is_true";
    public const string IS_FALSE = "is_false";

    // Numeric/comparison operators
    public const string GT = "gt";
    public const string LT = "lt";
    public const string GTE = "gte";
    public const string LTE = "lte";

    // Selection operators
    public const string IS_NOT = "is_not";
    public const string IS_ANY_OF = "is_any_of";
    public const string IS_NONE_OF = "is_none_of";

    // Legacy aliases (for backward compatibility)
    public const string EQUALS = "equals";
    public const string IN = "in";
    public const string NOT_IN = "notin";
    public const string IS_NULL = "isnull";
    public const string IS_NOT_NULL = "isnotnull";

    static FilterOperator() { }

    public FilterOperator() { }

    private FilterOperator(string label, string developerName, bool requiresValue, bool allowsMultipleValues)
    {
        Label = label;
        DeveloperName = developerName;
        RequiresValue = requiresValue;
        AllowsMultipleValues = allowsMultipleValues;
    }

    public static FilterOperator From(string developerName)
    {
        if (string.IsNullOrEmpty(developerName))
        {
            return Eq;
        }

        var normalized = developerName.ToLower();
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == normalized);

        if (type == null)
        {
            // Check legacy aliases
            type = normalized switch
            {
                EQUALS => Eq,
                IN => IsAnyOf,
                NOT_IN => IsNoneOf,
                IS_NULL => IsEmpty,
                IS_NOT_NULL => IsNotEmpty,
                _ => null
            };
        }

        return type ?? throw new FilterOperatorNotFoundException(developerName);
    }

    public static FilterOperator? TryFrom(string developerName)
    {
        try { return From(developerName); }
        catch { return null; }
    }

    // String operators
    public static FilterOperator Eq => new("equals", EQ, true, false);
    public static FilterOperator Neq => new("does not equal", NEQ, true, false);
    public static FilterOperator Contains => new("contains", CONTAINS, true, false);
    public static FilterOperator NotContains => new("does not contain", NOT_CONTAINS, true, false);
    public static FilterOperator StartsWith => new("starts with", STARTS_WITH, true, false);
    public static FilterOperator NotStartsWith => new("does not start with", NOT_STARTS_WITH, true, false);
    public static FilterOperator EndsWith => new("ends with", ENDS_WITH, true, false);
    public static FilterOperator NotEndsWith => new("does not end with", NOT_ENDS_WITH, true, false);
    public static FilterOperator IsEmpty => new("is empty", IS_EMPTY, false, false);
    public static FilterOperator IsNotEmpty => new("is not empty", IS_NOT_EMPTY, false, false);

    // Date operators
    public static FilterOperator Is => new("is", IS, true, false);
    public static FilterOperator IsWithin => new("is within", IS_WITHIN, true, false);
    public static FilterOperator IsBefore => new("is before", IS_BEFORE, true, false);
    public static FilterOperator IsAfter => new("is after", IS_AFTER, true, false);
    public static FilterOperator IsOnOrBefore => new("is on or before", IS_ON_OR_BEFORE, true, false);
    public static FilterOperator IsOnOrAfter => new("is on or after", IS_ON_OR_AFTER, true, false);

    // Boolean operators
    public static FilterOperator IsTrue => new("is Yes", IS_TRUE, false, false);
    public static FilterOperator IsFalse => new("is No", IS_FALSE, false, false);

    // Numeric operators
    public static FilterOperator Gt => new(">", GT, true, false);
    public static FilterOperator Lt => new("<", LT, true, false);
    public static FilterOperator Gte => new("≥", GTE, true, false);
    public static FilterOperator Lte => new("≤", LTE, true, false);

    // Selection operators
    public static FilterOperator IsNot => new("is not", IS_NOT, true, false);
    public static FilterOperator IsAnyOf => new("is any of", IS_ANY_OF, true, true);
    public static FilterOperator IsNoneOf => new("is none of", IS_NONE_OF, true, true);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public bool RequiresValue { get; set; }
    public bool AllowsMultipleValues { get; set; }

    public static implicit operator string(FilterOperator op) => op.DeveloperName;

    public static explicit operator FilterOperator(string type) => From(type);

    public override string ToString() => Label;

    /// <summary>
    /// All supported operator types.
    /// </summary>
    public static IEnumerable<FilterOperator> SupportedTypes
    {
        get
        {
            // String
            yield return Eq;
            yield return Neq;
            yield return Contains;
            yield return NotContains;
            yield return StartsWith;
            yield return NotStartsWith;
            yield return EndsWith;
            yield return NotEndsWith;
            yield return IsEmpty;
            yield return IsNotEmpty;
            // Date
            yield return Is;
            yield return IsWithin;
            yield return IsBefore;
            yield return IsAfter;
            yield return IsOnOrBefore;
            yield return IsOnOrAfter;
            // Boolean
            yield return IsTrue;
            yield return IsFalse;
            // Numeric
            yield return Gt;
            yield return Lt;
            yield return Gte;
            yield return Lte;
            // Selection
            yield return IsNot;
            yield return IsAnyOf;
            yield return IsNoneOf;
        }
    }

    /// <summary>
    /// String operators subset.
    /// </summary>
    public static IEnumerable<FilterOperator> StringOperators
    {
        get
        {
            yield return Eq;
            yield return Neq;
            yield return Contains;
            yield return NotContains;
            yield return StartsWith;
            yield return NotStartsWith;
            yield return EndsWith;
            yield return NotEndsWith;
            yield return IsEmpty;
            yield return IsNotEmpty;
        }
    }

    /// <summary>
    /// Date operators subset.
    /// </summary>
    public static IEnumerable<FilterOperator> DateOperators
    {
        get
        {
            yield return Is;
            yield return IsWithin;
            yield return IsBefore;
            yield return IsAfter;
            yield return IsOnOrBefore;
            yield return IsOnOrAfter;
            yield return IsEmpty;
            yield return IsNotEmpty;
        }
    }

    /// <summary>
    /// Boolean operators subset.
    /// </summary>
    public static IEnumerable<FilterOperator> BooleanOperators
    {
        get
        {
            yield return IsTrue;
            yield return IsFalse;
        }
    }

    /// <summary>
    /// Numeric operators subset.
    /// </summary>
    public static IEnumerable<FilterOperator> NumericOperators
    {
        get
        {
            yield return Eq;
            yield return Neq;
            yield return Gt;
            yield return Lt;
            yield return Gte;
            yield return Lte;
            yield return IsEmpty;
            yield return IsNotEmpty;
        }
    }

    /// <summary>
    /// Selection operators subset.
    /// </summary>
    public static IEnumerable<FilterOperator> SelectionOperators
    {
        get
        {
            yield return Is;
            yield return IsNot;
            yield return IsAnyOf;
            yield return IsNoneOf;
        }
    }

    /// <summary>
    /// Priority operators subset (includes comparison).
    /// </summary>
    public static IEnumerable<FilterOperator> PriorityOperators
    {
        get
        {
            yield return Is;
            yield return IsNot;
            yield return IsAnyOf;
            yield return Gt;
            yield return Lt;
            yield return Gte;
            yield return Lte;
        }
    }

    /// <summary>
    /// User operators subset.
    /// </summary>
    public static IEnumerable<FilterOperator> UserOperators
    {
        get
        {
            yield return Is;
            yield return IsNot;
            yield return IsAnyOf;
            yield return IsNoneOf;
            yield return IsEmpty;
            yield return IsNotEmpty;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class FilterOperatorNotFoundException : Exception
{
    public FilterOperatorNotFoundException(string developerName)
        : base($"Filter operator '{developerName}' is not supported.")
    {
    }
}

