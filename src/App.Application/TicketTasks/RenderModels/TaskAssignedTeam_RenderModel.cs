using App.Application.Common.Interfaces;

namespace App.Application.TicketTasks.RenderModels;

/// <summary>
/// Render model for task-assigned-to-team email notifications.
/// </summary>
public record TaskAssignedTeam_RenderModel : IInsertTemplateVariable
{
    public long TicketId { get; init; }
    public string TicketTitle { get; init; } = string.Empty;
    public string TaskTitle { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string? DueAt { get; init; }
    public string TicketUrl { get; init; } = string.Empty;

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(TicketId);
        yield return nameof(TicketTitle);
        yield return nameof(TaskTitle);
        yield return nameof(TeamName);
        yield return nameof(RecipientName);
        yield return nameof(DueAt);
        yield return nameof(TicketUrl);
    }

    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(developerName, $"Target.{developerName}");
        }
    }

    public string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}
