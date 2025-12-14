using App.Application.Common.Interfaces;

namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for ticket assigned to team email notifications.
/// </summary>
public record TicketAssignedToTeam_RenderModel : IInsertTemplateVariable
{
    public long TicketId { get; init; }
    public string TicketTitle { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public string? AssigneeName { get; init; }
    public string TicketUrl { get; init; } = string.Empty;

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(TicketId);
        yield return nameof(TicketTitle);
        yield return nameof(Priority);
        yield return nameof(TeamName);
        yield return nameof(AssigneeName);
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
