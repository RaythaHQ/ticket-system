using App.Application.Common.Interfaces;

namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for ticket reopened email notifications.
/// </summary>
public record TicketReopened_RenderModel : IInsertTemplateVariable
{
    public long TicketId { get; init; }
    public string TicketTitle { get; init; } = string.Empty;
    public string ReopenedByName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(TicketId);
        yield return nameof(TicketTitle);
        yield return nameof(ReopenedByName);
        yield return nameof(Status);
        yield return nameof(RecipientName);
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
