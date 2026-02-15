namespace App.Domain.ValueObjects;

public class AppointmentMode : ValueObject
{
    public const string VIRTUAL = "virtual";
    public const string IN_PERSON = "in_person";
    public const string EITHER = "either";

    static AppointmentMode() { }

    public AppointmentMode() { }

    private AppointmentMode(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static AppointmentMode From(string developerName)
    {
        if (string.IsNullOrEmpty(developerName))
        {
            throw new AppointmentModeNotFoundException(developerName ?? "null");
        }

        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new AppointmentModeNotFoundException(developerName);
        }

        return type;
    }

    public static AppointmentMode Virtual => new("Virtual", VIRTUAL);
    public static AppointmentMode InPerson => new("In-Person", IN_PERSON);
    public static AppointmentMode Either => new("Either", EITHER);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this mode requires a meeting link (true for Virtual).
    /// </summary>
    public bool RequiresMeetingLink => DeveloperName == VIRTUAL;

    /// <summary>
    /// Whether this mode requires coverage zone validation (true for In-Person).
    /// </summary>
    public bool RequiresCoverageValidation => DeveloperName == IN_PERSON;

    public static implicit operator string(AppointmentMode mode)
    {
        return mode.DeveloperName;
    }

    public static explicit operator AppointmentMode(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<AppointmentMode> SupportedTypes
    {
        get
        {
            yield return Virtual;
            yield return InPerson;
            yield return Either;
        }
    }

    /// <summary>
    /// Returns only the modes that can be assigned to an individual appointment (not "Either").
    /// </summary>
    public static IEnumerable<AppointmentMode> AppointmentModes
    {
        get
        {
            yield return Virtual;
            yield return InPerson;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class AppointmentModeNotFoundException : Exception
{
    public AppointmentModeNotFoundException(string developerName)
        : base($"Appointment mode '{developerName}' is not supported.") { }
}
