namespace App.Domain.ValueObjects;

public class NotificationEventType : ValueObject
{
    public const string TICKET_ASSIGNED = "ticket_assigned";
    public const string TICKET_ASSIGNED_TEAM = "ticket_assigned_team";
    public const string COMMENT_ADDED = "comment_added";
    public const string STATUS_CHANGED = "status_changed";
    public const string TICKET_CLOSED = "ticket_closed";
    public const string TICKET_REOPENED = "ticket_reopened";
    public const string SLA_APPROACHING = "sla_approaching";
    public const string SLA_BREACHED = "sla_breached";

    static NotificationEventType() { }

    public NotificationEventType() { }

    private NotificationEventType(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static NotificationEventType From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new NotificationEventTypeNotFoundException(developerName);
        }

        return type;
    }

    public static NotificationEventType TicketAssigned => new("Ticket Assigned", TICKET_ASSIGNED);
    public static NotificationEventType TicketAssignedToTeam => new("Ticket Assigned to Team", TICKET_ASSIGNED_TEAM);
    public static NotificationEventType CommentAdded => new("Comment Added", COMMENT_ADDED);
    public static NotificationEventType StatusChanged => new("Status Changed", STATUS_CHANGED);
    public static NotificationEventType TicketClosed => new("Ticket Closed", TICKET_CLOSED);
    public static NotificationEventType TicketReopened => new("Ticket Reopened", TICKET_REOPENED);
    public static NotificationEventType SlaApproaching => new("SLA Approaching Breach", SLA_APPROACHING);
    public static NotificationEventType SlaBreached => new("SLA Breached", SLA_BREACHED);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(NotificationEventType eventType)
    {
        return eventType.DeveloperName;
    }

    public static explicit operator NotificationEventType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<NotificationEventType> SupportedTypes
    {
        get
        {
            yield return TicketAssigned;
            yield return TicketAssignedToTeam;
            yield return CommentAdded;
            yield return StatusChanged;
            yield return TicketClosed;
            yield return TicketReopened;
            yield return SlaApproaching;
            yield return SlaBreached;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class NotificationEventTypeNotFoundException : Exception
{
    public NotificationEventTypeNotFoundException(string developerName)
        : base($"Notification event type '{developerName}' is not supported.")
    {
    }
}

