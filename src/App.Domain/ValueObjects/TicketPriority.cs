namespace App.Domain.ValueObjects;

public class TicketPriority : ValueObject
{
    public const string LOW = "low";
    public const string NORMAL = "normal";
    public const string HIGH = "high";
    public const string URGENT = "urgent";

    static TicketPriority() { }

    public TicketPriority() { }

    private TicketPriority(string label, string developerName, int sortOrder)
    {
        Label = label;
        DeveloperName = developerName;
        SortOrder = sortOrder;
    }

    public static TicketPriority From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new TicketPriorityNotFoundException(developerName);
        }

        return type;
    }

    public static TicketPriority Low => new("Low", LOW, 1);
    public static TicketPriority Normal => new("Normal", NORMAL, 2);
    public static TicketPriority High => new("High", HIGH, 3);
    public static TicketPriority Urgent => new("Urgent", URGENT, 4);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public static implicit operator string(TicketPriority priority)
    {
        return priority.DeveloperName;
    }

    public static explicit operator TicketPriority(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<TicketPriority> SupportedTypes
    {
        get
        {
            yield return Low;
            yield return Normal;
            yield return High;
            yield return Urgent;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class TicketPriorityNotFoundException : Exception
{
    public TicketPriorityNotFoundException(string developerName)
        : base($"Ticket priority '{developerName}' is not supported.")
    {
    }
}

