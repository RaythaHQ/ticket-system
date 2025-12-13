using App.Application.TicketViews;

namespace App.Web.Areas.Staff.Pages.Shared.Models;

/// <summary>
/// View model for the _FilterBuilder partial.
/// </summary>
public class FilterBuilderViewModel
{
    /// <summary>
    /// Current filter conditions (for edit mode).
    /// </summary>
    public ViewConditions? Conditions { get; set; }

    /// <summary>
    /// Available filter attributes with operators.
    /// </summary>
    public List<FilterAttributeModel> Attributes { get; set; } = new();

    /// <summary>
    /// Available users for user selection filters.
    /// </summary>
    public List<UserOption> Users { get; set; } = new();

    /// <summary>
    /// Available teams for team selection filters.
    /// </summary>
    public List<TeamOption> Teams { get; set; } = new();

    /// <summary>
    /// Available statuses for status filters.
    /// </summary>
    public List<SelectOption> Statuses { get; set; } = new();

    /// <summary>
    /// Available priorities for priority filters.
    /// </summary>
    public List<SelectOption> Priorities { get; set; } = new();

    /// <summary>
    /// Relative date presets.
    /// </summary>
    public List<SelectOption> DatePresets { get; set; } = new();

    /// <summary>
    /// Show help text below the filter builder.
    /// </summary>
    public bool ShowHelp { get; set; } = true;

    /// <summary>
    /// Creates a FilterBuilderViewModel with default attribute definitions.
    /// </summary>
    public static FilterBuilderViewModel CreateWithDefaults()
    {
        var model = new FilterBuilderViewModel
        {
            Attributes = FilterAttributes.All.Select(a => new FilterAttributeModel
            {
                Field = a.Field,
                Label = a.Label,
                Type = a.Type,
                Operators = a.Operators.Select(o => new OperatorOption
                {
                    Value = o.Value,
                    Label = o.Label,
                    RequiresValue = o.RequiresValue,
                    AllowsMultipleValues = o.AllowsMultipleValues
                }).ToList()
            }).ToList(),
            DatePresets = RelativeDatePresets.Presets.Select(p => new SelectOption
            {
                Value = p.Value,
                Label = p.Label
            }).ToList()
        };
        return model;
    }
}

public class FilterAttributeModel
{
    public string Field { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Type { get; set; } = null!;
    public List<OperatorOption> Operators { get; set; } = new();
}

public class OperatorOption
{
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
    public bool RequiresValue { get; set; } = true;
    public bool AllowsMultipleValues { get; set; }
}

public class UserOption
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsDeactivated { get; set; }
}

public class TeamOption
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class SelectOption
{
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
}

