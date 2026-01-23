using CSharpVitamins;

namespace App.Application.Notifications;

/// <summary>
/// DTO for a notification in the notification center.
/// </summary>
public record NotificationDto
{
    public ShortGuid Id { get; init; }
    public ShortGuid RecipientUserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string EventTypeLabel { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Url { get; init; }
    public long? TicketId { get; init; }
    public string? TicketTitle { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedAtFormatted { get; init; } = string.Empty;
    public string CreatedAtRelative { get; init; } = string.Empty;
    public DateTime? ReadAt { get; init; }
}

/// <summary>
/// Simplified DTO for notification list display.
/// </summary>
public record NotificationListItemDto
{
    public ShortGuid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string EventTypeLabel { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Url { get; init; }
    public long? TicketId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedAtRelative { get; init; } = string.Empty;
}

