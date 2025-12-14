using App.Application.Common.Interfaces;

namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for SLA approaching breach email notifications.
/// </summary>
public record SlaApproaching_RenderModel : IInsertTemplateVariable
{
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AssigneeName { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string SlaDueAt { get; init; } = string.Empty;
    public string TimeRemaining { get; init; } = string.Empty;
    public string SlaRuleName { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(TicketId);
        yield return nameof(Title);
        yield return nameof(AssigneeName);
        yield return nameof(Priority);
        yield return nameof(SlaDueAt);
        yield return nameof(TimeRemaining);
        yield return nameof(SlaRuleName);
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
