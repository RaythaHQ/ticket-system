namespace App.Domain.ValueObjects;

public class ImportEntityType : ValueObject
{
    public const string CONTACTS = "contacts";
    public const string TICKETS = "tickets";

    static ImportEntityType() { }

    public ImportEntityType() { }

    private ImportEntityType(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static ImportEntityType From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new ImportEntityTypeNotFoundException(developerName);
        }

        return type;
    }

    public static ImportEntityType Contacts => new("Contacts", CONTACTS);
    public static ImportEntityType Tickets => new("Tickets", TICKETS);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(ImportEntityType type)
    {
        return type.DeveloperName;
    }

    public static explicit operator ImportEntityType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<ImportEntityType> SupportedTypes
    {
        get
        {
            yield return Contacts;
            yield return Tickets;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }

    public static bool operator ==(ImportEntityType? left, ImportEntityType? right)
    {
        return EqualOperator(left!, right!);
    }

    public static bool operator !=(ImportEntityType? left, ImportEntityType? right)
    {
        return NotEqualOperator(left!, right!);
    }

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();
}

public class ImportEntityTypeNotFoundException : Exception
{
    public ImportEntityTypeNotFoundException(string developerName)
        : base($"Import entity type '{developerName}' is not supported.") { }
}
