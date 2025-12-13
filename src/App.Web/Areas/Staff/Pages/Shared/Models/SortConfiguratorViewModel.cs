using App.Application.TicketViews;

namespace App.Web.Areas.Staff.Pages.Shared.Models;

/// <summary>
/// View model for the _SortConfigurator partial.
/// </summary>
public class SortConfiguratorViewModel
{
    /// <summary>
    /// Current sort levels (for edit mode).
    /// </summary>
    public List<SortLevelModel>? SortLevels { get; set; }

    /// <summary>
    /// Available sortable fields.
    /// </summary>
    public List<SortFieldModel> SortableFields { get; set; } = new();

    /// <summary>
    /// Show help text below the sort configurator.
    /// </summary>
    public bool ShowHelp { get; set; } = true;

    /// <summary>
    /// Creates a SortConfiguratorViewModel with default sortable fields.
    /// </summary>
    public static SortConfiguratorViewModel CreateWithDefaults()
    {
        var model = new SortConfiguratorViewModel
        {
            SortableFields = FilterAttributes.GetSortable().Select(a => new SortFieldModel
            {
                Field = a.Field,
                Label = a.Label
            }).ToList()
        };
        return model;
    }
}

public class SortLevelModel
{
    public int Order { get; set; }
    public string Field { get; set; } = null!;
    public string Direction { get; set; } = "asc";
}

public class SortFieldModel
{
    public string Field { get; set; } = null!;
    public string Label { get; set; } = null!;
}

