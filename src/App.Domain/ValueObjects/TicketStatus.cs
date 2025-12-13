namespace App.Domain.ValueObjects;

public class TicketStatus : ValueObject
{
    public const string OPEN = "open";
    public const string IN_PROGRESS = "in_progress";
    public const string PENDING = "pending";
    public const string RESOLVED = "resolved";
    public const string CLOSED = "closed";

    static TicketStatus() { }

    public TicketStatus() { }

    private TicketStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static TicketStatus From(string developerName)
    {
        if (string.IsNullOrEmpty(developerName))
        {
            return Open; // Default fallback
        }

        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            // Return a dynamic status for custom/unknown values
            // This supports configurable statuses from the database
            return new TicketStatus(
                ToPascalCase(developerName),
                developerName.ToLower()
            );
        }

        return type;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var words = input.Replace("_", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    }

    public static TicketStatus Open => new("Open", OPEN);
    public static TicketStatus InProgress => new("In Progress", IN_PROGRESS);
    public static TicketStatus Pending => new("Pending", PENDING);
    public static TicketStatus Resolved => new("Resolved", RESOLVED);
    public static TicketStatus Closed => new("Closed", CLOSED);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(TicketStatus status)
    {
        return status.DeveloperName;
    }

    public static explicit operator TicketStatus(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<TicketStatus> SupportedTypes
    {
        get
        {
            yield return Open;
            yield return InProgress;
            yield return Pending;
            yield return Resolved;
            yield return Closed;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class TicketStatusNotFoundException : Exception
{
    public TicketStatusNotFoundException(string developerName)
        : base($"Ticket status '{developerName}' is not supported.")
    {
    }
}

