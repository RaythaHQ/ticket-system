namespace App.Application.TicketViews;

/// <summary>
/// Definition of a displayable column in ticket list views.
/// </summary>
public record ColumnDefinition
{
    public string Field { get; init; } = null!;
    public string Label { get; init; } = null!;
    public bool IsClickable { get; init; }

    /// <summary>
    /// Target for clickable columns: "ticket", "contact", or null.
    /// </summary>
    public string? ClickTarget { get; init; }

    /// <summary>
    /// Whether this column is searchable.
    /// </summary>
    public bool IsSearchable { get; init; } = true;
}

/// <summary>
/// Registry of all available display columns for ticket views.
/// </summary>
public static class ColumnRegistry
{
    public static readonly IReadOnlyList<ColumnDefinition> Columns = new List<ColumnDefinition>
    {
        new()
        {
            Field = "Id",
            Label = "Ticket ID",
            IsClickable = true,
            ClickTarget = "ticket",
        },
        new()
        {
            Field = "Title",
            Label = "Title",
            IsClickable = true,
            ClickTarget = "ticket",
        },
        new() { Field = "Status", Label = "Status" },
        new() { Field = "Priority", Label = "Priority" },
        new() { Field = "Category", Label = "Category" },
        new() { Field = "AssigneeName", Label = "Assignee" },
        new() { Field = "OwningTeamName", Label = "Team" },
        new()
        {
            Field = "ContactId",
            Label = "Contact ID",
            IsClickable = true,
            ClickTarget = "contact",
        },
        new() { Field = "ContactName", Label = "Contact" },
        new() { Field = "SlaStatus", Label = "SLA Status" },
        new() { Field = "SlaDueAt", Label = "SLA Due" },
        new() { Field = "CreationTime", Label = "Created" },
        new() { Field = "LastModificationTime", Label = "Last Updated" },
        new() { Field = "ClosedAt", Label = "Closed" },
        new() { Field = "Tags", Label = "Tags" },
        new() { Field = "CreatedByName", Label = "Created By" },
        new()
        {
            Field = "Description",
            Label = "Description",
            IsSearchable = true,
        },
    };

    /// <summary>
    /// Get column definition by field name.
    /// </summary>
    public static ColumnDefinition? GetByField(string field)
    {
        return Columns.FirstOrDefault(c =>
            c.Field.Equals(field, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Get all searchable columns.
    /// </summary>
    public static IEnumerable<ColumnDefinition> GetSearchable()
    {
        return Columns.Where(c => c.IsSearchable);
    }

    /// <summary>
    /// Get all clickable columns.
    /// </summary>
    public static IEnumerable<ColumnDefinition> GetClickable()
    {
        return Columns.Where(c => c.IsClickable);
    }
}
