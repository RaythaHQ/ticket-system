namespace App.Domain.ValueObjects;

/// <summary>
/// Represents the type/category of a ticket status - either Open or Closed.
/// Used for filtering and business logic (SLA, metrics, etc.).
/// </summary>
public class TicketStatusType : ValueObject
{
    public const string OPEN = "open";
    public const string CLOSED = "closed";

    static TicketStatusType() { }

    public TicketStatusType() { }

    private TicketStatusType(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static TicketStatusType From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new TicketStatusTypeNotFoundException(developerName);
        }

        return type;
    }

    public static TicketStatusType Open => new("Open", OPEN);
    public static TicketStatusType Closed => new("Closed", CLOSED);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(TicketStatusType type)
    {
        return type.DeveloperName;
    }

    public static explicit operator TicketStatusType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<TicketStatusType> SupportedTypes
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

public class TicketStatusTypeNotFoundException : Exception
{
    public TicketStatusTypeNotFoundException(string developerName)
        : base($"Ticket status type '{developerName}' is not supported. Valid types are: open, closed.")
    {
    }
}

