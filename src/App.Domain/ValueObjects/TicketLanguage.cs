namespace App.Domain.ValueObjects;

public class TicketLanguage : ValueObject
{
    public const string ENGLISH = "english";
    public const string SPANISH = "spanish";
    public const string CHINESE = "chinese";
    public const string RUSSIAN = "russian";
    public const string BENGALI = "bengali";
    public const string OTHER = "other";

    static TicketLanguage() { }

    public TicketLanguage() { }

    private TicketLanguage(string label, string developerName, int sortOrder)
    {
        Label = label;
        DeveloperName = developerName;
        SortOrder = sortOrder;
    }

    public static TicketLanguage From(string developerName)
    {
        if (string.IsNullOrEmpty(developerName))
        {
            return English; // Default fallback
        }

        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new TicketLanguageNotFoundException(developerName);
        }

        return type;
    }

    public static TicketLanguage English => new("English", ENGLISH, 1);
    public static TicketLanguage Spanish => new("Spanish", SPANISH, 2);
    public static TicketLanguage Chinese => new("Chinese", CHINESE, 3);
    public static TicketLanguage Russian => new("Russian", RUSSIAN, 4);
    public static TicketLanguage Bengali => new("Bengali", BENGALI, 5);
    public static TicketLanguage Other => new("Other", OTHER, 6);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public static implicit operator string(TicketLanguage language)
    {
        return language.DeveloperName;
    }

    public static explicit operator TicketLanguage(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<TicketLanguage> SupportedTypes
    {
        get
        {
            yield return English;
            yield return Spanish;
            yield return Chinese;
            yield return Russian;
            yield return Bengali;
            yield return Other;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class TicketLanguageNotFoundException : Exception
{
    public TicketLanguageNotFoundException(string developerName)
        : base($"Ticket language '{developerName}' is not supported.") { }
}
