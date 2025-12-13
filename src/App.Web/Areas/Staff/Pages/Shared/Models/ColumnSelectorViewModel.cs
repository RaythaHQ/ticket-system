using App.Application.TicketViews;

namespace App.Web.Areas.Staff.Pages.Shared.Models;

/// <summary>
/// View model for the _ColumnSelector partial.
/// </summary>
public class ColumnSelectorViewModel
{
    /// <summary>
    /// Currently selected columns in order (for edit mode).
    /// </summary>
    public List<string>? SelectedColumns { get; set; }

    /// <summary>
    /// All available columns.
    /// </summary>
    public List<ColumnModel> AvailableColumns { get; set; } = new();

    /// <summary>
    /// Show help text below the column selector.
    /// </summary>
    public bool ShowHelp { get; set; } = true;

    /// <summary>
    /// Creates a ColumnSelectorViewModel with default columns.
    /// </summary>
    public static ColumnSelectorViewModel CreateWithDefaults()
    {
        var model = new ColumnSelectorViewModel
        {
            AvailableColumns = ColumnRegistry.Columns.Select(c => new ColumnModel
            {
                Field = c.Field,
                Label = c.Label,
                IsClickable = c.IsClickable,
                ClickTarget = c.ClickTarget
            }).ToList()
        };
        return model;
    }
}

public class ColumnModel
{
    public string Field { get; set; } = null!;
    public string Label { get; set; } = null!;
    public bool IsClickable { get; set; }
    public string? ClickTarget { get; set; }
}

