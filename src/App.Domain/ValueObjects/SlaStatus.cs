namespace App.Domain.ValueObjects;

public class SlaStatus : ValueObject
{
    public const string ON_TRACK = "on_track";
    public const string APPROACHING_BREACH = "approaching_breach";
    public const string BREACHED = "breached";
    public const string COMPLETED = "completed";

    static SlaStatus() { }

    public SlaStatus() { }

    private SlaStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static SlaStatus From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new SlaStatusNotFoundException(developerName);
        }

        return type;
    }

    public static SlaStatus OnTrack => new("On Track", ON_TRACK);
    public static SlaStatus ApproachingBreach => new("Approaching Breach", APPROACHING_BREACH);
    public static SlaStatus Breached => new("Breached", BREACHED);
    public static SlaStatus Completed => new("Completed", COMPLETED);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(SlaStatus status)
    {
        return status.DeveloperName;
    }

    public static explicit operator SlaStatus(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<SlaStatus> SupportedTypes
    {
        get
        {
            yield return OnTrack;
            yield return ApproachingBreach;
            yield return Breached;
            yield return Completed;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class SlaStatusNotFoundException : Exception
{
    public SlaStatusNotFoundException(string developerName)
        : base($"SLA status '{developerName}' is not supported.")
    {
    }
}

