using CSharpVitamins;

namespace App.Application.NotificationPreferences;

/// <summary>
/// DTO for notification preferences.
/// </summary>
public record NotificationPreferenceDto
{
    public ShortGuid Id { get; init; }
    public ShortGuid StaffAdminId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string EventTypeLabel { get; init; } = string.Empty;
    public bool EmailEnabled { get; init; }
    public bool InAppEnabled { get; init; }
    public bool WebhookEnabled { get; init; }
    public DateTime CreationTime { get; init; }
}
