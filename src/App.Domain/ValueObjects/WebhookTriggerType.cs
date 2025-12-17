namespace App.Domain.ValueObjects;

/// <summary>
/// Value object representing the type of event that triggers a webhook.
/// </summary>
public class WebhookTriggerType : ValueObject
{
    public const string TICKET_CREATED = "ticket_created";
    public const string TICKET_UPDATED = "ticket_updated";
    public const string TICKET_STATUS_CHANGED = "ticket_status_changed";
    public const string TICKET_ASSIGNEE_CHANGED = "ticket_assignee_changed";
    public const string COMMENT_ADDED = "comment_added";

    static WebhookTriggerType() { }

    public WebhookTriggerType() { }

    private WebhookTriggerType(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static WebhookTriggerType From(string developerName)
    {
        if (string.IsNullOrEmpty(developerName))
        {
            throw new WebhookTriggerTypeNotFoundException(developerName ?? "null");
        }

        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new WebhookTriggerTypeNotFoundException(developerName);
        }

        return type;
    }

    public static WebhookTriggerType TicketCreated => new("New Ticket Added", TICKET_CREATED);
    public static WebhookTriggerType TicketUpdated => new("Ticket Updated", TICKET_UPDATED);
    public static WebhookTriggerType TicketStatusChanged =>
        new("Ticket Status Changed", TICKET_STATUS_CHANGED);
    public static WebhookTriggerType TicketAssigneeChanged =>
        new("Ticket Assignee Changed", TICKET_ASSIGNEE_CHANGED);
    public static WebhookTriggerType CommentAdded => new("New Comment Added", COMMENT_ADDED);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(WebhookTriggerType type)
    {
        return type.DeveloperName;
    }

    public static explicit operator WebhookTriggerType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<WebhookTriggerType> SupportedTypes
    {
        get
        {
            yield return TicketCreated;
            yield return TicketUpdated;
            yield return TicketStatusChanged;
            yield return TicketAssigneeChanged;
            yield return CommentAdded;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class WebhookTriggerTypeNotFoundException : Exception
{
    public WebhookTriggerTypeNotFoundException(string developerName)
        : base($"Webhook trigger type '{developerName}' is not supported.") { }
}
