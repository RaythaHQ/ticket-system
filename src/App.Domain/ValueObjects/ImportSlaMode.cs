namespace App.Domain.ValueObjects;

/// <summary>
/// Defines how SLA rules should be applied during ticket import.
/// </summary>
public class ImportSlaMode : ValueObject
{
    public const string DO_NOT_APPLY = "do_not_apply";
    public const string FROM_CREATED_AT = "from_created_at";
    public const string FROM_CURRENT_TIME = "from_current_time";

    static ImportSlaMode() { }

    public ImportSlaMode() { }

    private ImportSlaMode(string label, string developerName, string description)
    {
        Label = label;
        DeveloperName = developerName;
        Description = description;
    }

    public static ImportSlaMode From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new ImportSlaModeNotFoundException(developerName);
        }

        return type;
    }

    public static ImportSlaMode DoNotApply =>
        new(
            "Do not apply SLA rules",
            DO_NOT_APPLY,
            "Tickets will be imported without SLA assignment. Use this for historical data migration."
        );

    public static ImportSlaMode FromCreatedAt =>
        new(
            "Apply SLA rules from Created At date",
            FROM_CREATED_AT,
            "SLA due dates will be calculated from the ticket's Created At timestamp."
        );

    public static ImportSlaMode FromCurrentTime =>
        new(
            "Apply SLA rules from current time",
            FROM_CURRENT_TIME,
            "SLA due dates will be calculated from the current time when the import runs."
        );

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public static implicit operator string(ImportSlaMode mode)
    {
        return mode.DeveloperName;
    }

    public static explicit operator ImportSlaMode(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<ImportSlaMode> SupportedTypes
    {
        get
        {
            yield return DoNotApply;
            yield return FromCreatedAt;
            yield return FromCurrentTime;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class ImportSlaModeNotFoundException : Exception
{
    public ImportSlaModeNotFoundException(string developerName)
        : base($"Import SLA mode '{developerName}' is not supported.") { }
}
