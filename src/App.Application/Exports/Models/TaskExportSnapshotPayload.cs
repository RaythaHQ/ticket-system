namespace App.Application.Exports.Models;

/// <summary>
/// Represents the captured task view state at export request time.
/// </summary>
public record TaskExportSnapshotPayload
{
    /// <summary>
    /// The built-in task view (my-tasks, team-tasks, unassigned, etc.)
    /// </summary>
    public string? BuiltInView { get; init; }

    /// <summary>
    /// The user ID who requested the export (needed for My Tasks, Created By Me, Team Tasks views).
    /// </summary>
    public Guid? RequestingUserId { get; init; }

    public string? SearchTerm { get; init; }
    public string? SortField { get; init; }
    public string? SortDirection { get; init; }
    public string? StatusFilter { get; init; }
}
