namespace App.Domain.ValueObjects;

public class TicketTaskStatus : ValueObject
{
    public const string OPEN = "open";
    public const string CLOSED = "closed";

    static TicketTaskStatus() { }

    public TicketTaskStatus() { }

    private TicketTaskStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static TicketTaskStatus From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new TicketTaskStatusNotFoundException(developerName);
        }

        return type;
    }

    public static TicketTaskStatus Open => new("Open", OPEN);
    public static TicketTaskStatus Closed => new("Closed", CLOSED);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(TicketTaskStatus status)
    {
        return status.DeveloperName;
    }

    public static explicit operator TicketTaskStatus(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<TicketTaskStatus> SupportedTypes
    {
        get
        {
            yield return Open;
            yield return Closed;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class TicketTaskStatusNotFoundException : Exception
{
    public TicketTaskStatusNotFoundException(string developerName)
        : base($"Ticket task status '{developerName}' is not supported.") { }
}
