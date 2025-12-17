namespace App.Application.Webhooks;

/// <summary>
/// The payload structure sent to webhook endpoints.
/// </summary>
public record WebhookPayloadDto
{
    /// <summary>
    /// The event type that triggered this webhook.
    /// </summary>
    public string Event { get; init; } = null!;

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The ticket ID associated with this event.
    /// </summary>
    public long TicketId { get; init; }

    /// <summary>
    /// The previous state of the ticket (null for ticket_created events).
    /// </summary>
    public WebhookTicketDto? Old { get; init; }

    /// <summary>
    /// The current state of the ticket.
    /// </summary>
    public WebhookTicketDto New { get; init; } = null!;
}

/// <summary>
/// Ticket data included in webhook payloads.
/// </summary>
public record WebhookTicketDto
{
    public long Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string Status { get; init; } = null!;
    public string StatusLabel { get; init; } = null!;
    public string Priority { get; init; } = null!;
    public string PriorityLabel { get; init; } = null!;
    public string? Category { get; init; }
    public List<string> Tags { get; init; } = new();

    // Assignee
    public string? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }

    // Team
    public string? TeamId { get; init; }
    public string? TeamName { get; init; }

    // Contact
    public long? ContactId { get; init; }
    public string? ContactName { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPrimaryPhone { get; init; }

    // SLA
    public string? SlaStatus { get; init; }
    public DateTime? SlaDueAt { get; init; }

    // Timestamps
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}

/// <summary>
/// Extended payload for comment-added events.
/// </summary>
public record WebhookCommentPayloadDto : WebhookPayloadDto
{
    /// <summary>
    /// The comment that was added.
    /// </summary>
    public WebhookCommentDto Comment { get; init; } = null!;
}

/// <summary>
/// Comment data included in webhook payloads.
/// </summary>
public record WebhookCommentDto
{
    public string Id { get; init; } = null!;
    public string Content { get; init; } = null!;
    public bool IsInternal { get; init; }
    public string? AuthorId { get; init; }
    public string? AuthorName { get; init; }
    public DateTime CreatedAt { get; init; }
}
