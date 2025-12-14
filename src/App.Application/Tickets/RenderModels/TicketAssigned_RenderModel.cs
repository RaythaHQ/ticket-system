using App.Application.Common.Interfaces;

namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for ticket assignment email notifications.
/// </summary>
public record TicketAssigned_RenderModel : IInsertTemplateVariable
{
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? TeamName { get; init; }
    public string? SlaDueAt { get; init; }
    public string TicketUrl { get; init; } = string.Empty;

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(TicketId);
        yield return nameof(Title);
        yield return nameof(Priority);
        yield return nameof(Status);
        yield return nameof(Category);
        yield return nameof(AssigneeName);
        yield return nameof(ContactName);
        yield return nameof(TeamName);
        yield return nameof(SlaDueAt);
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
