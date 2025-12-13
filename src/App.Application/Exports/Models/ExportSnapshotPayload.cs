using CSharpVitamins;

namespace App.Application.Exports.Models;

/// <summary>
/// Represents the captured view state at export request time.
/// </summary>
public record ExportSnapshotPayload
{
    public ShortGuid? ViewId { get; init; }
    public List<ExportFilter> Filters { get; init; } = new();
    public string? SearchTerm { get; init; }
    public string? SortField { get; init; }
    public string? SortDirection { get; init; }
    public List<string> Columns { get; init; } = new();
    public ExportScope? Scope { get; init; }
}

/// <summary>
/// Represents a filter condition in the export snapshot.
/// </summary>
public record ExportFilter
{
    public string Field { get; init; } = null!;
    public string Operator { get; init; } = null!;
    public string? Value { get; init; }
}

/// <summary>
/// Represents the scope of the export (team, assigned user, etc).
/// </summary>
public record ExportScope
{
    public ShortGuid? TeamId { get; init; }
    public ShortGuid? AssignedToUserId { get; init; }
}

