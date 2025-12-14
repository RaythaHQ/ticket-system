using App.Application.Common.Interfaces;

namespace App.Application.Tickets.RenderModels;

/// <summary>
/// Render model for ticket status changed email notifications.
/// </summary>
public record TicketStatusChanged_RenderModel : IInsertTemplateVariable
{
    public long TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public string ChangedBy { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(TicketId);
        yield return nameof(Title);
        yield return nameof(RecipientName);
        yield return nameof(OldStatus);
        yield return nameof(NewStatus);
        yield return nameof(ChangedBy);
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
