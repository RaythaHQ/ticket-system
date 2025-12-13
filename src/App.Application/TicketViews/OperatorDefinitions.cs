using App.Domain.ValueObjects;

namespace App.Application.TicketViews;

/// <summary>
/// Operator definition DTOs for UI display, backed by domain FilterOperator ValueObjects.
/// </summary>
public static class OperatorDefinitions
{
    /// <summary>
    /// Convert domain operators to UI-friendly DTOs.
    /// </summary>
    private static List<FilterOperatorDto> ToDto(IEnumerable<FilterOperator> operators)
    {
        return operators.Select(op => new FilterOperatorDto
        {
            Value = op.DeveloperName,
            Label = op.Label,
            RequiresValue = op.RequiresValue,
            AllowsMultipleValues = op.AllowsMultipleValues
        }).ToList();
    }

    public static readonly List<FilterOperatorDto> StringOperators = ToDto(FilterOperator.StringOperators);
    public static readonly List<FilterOperatorDto> DateOperators = ToDto(FilterOperator.DateOperators);
    public static readonly List<FilterOperatorDto> BooleanOperators = ToDto(FilterOperator.BooleanOperators);
    public static readonly List<FilterOperatorDto> NumericOperators = ToDto(FilterOperator.NumericOperators);
    public static readonly List<FilterOperatorDto> SelectionOperators = ToDto(FilterOperator.SelectionOperators);
    public static readonly List<FilterOperatorDto> PriorityOperators = ToDto(FilterOperator.PriorityOperators);
    public static readonly List<FilterOperatorDto> UserOperators = ToDto(FilterOperator.UserOperators);

    /// <summary>
    /// Get operators for a given attribute type.
    /// </summary>
    public static List<FilterOperatorDto> GetOperatorsForType(string type)
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

/// <summary>
/// DTO for filter operators to use in UI.
/// </summary>
public record FilterOperatorDto
{
    public string Value { get; init; } = null!;
    public string Label { get; init; } = null!;
    public bool RequiresValue { get; init; } = true;
    public bool AllowsMultipleValues { get; init; }
}
