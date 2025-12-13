namespace App.Domain.ValueObjects;

public class ExportJobStatus : ValueObject
{
    public const string QUEUED = "queued";
    public const string RUNNING = "running";
    public const string COMPLETED = "completed";
    public const string FAILED = "failed";

    static ExportJobStatus() { }

    public ExportJobStatus() { }

    private ExportJobStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static ExportJobStatus From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new ExportJobStatusNotFoundException(developerName);
        }

        return type;
    }

    public static ExportJobStatus Queued => new("Queued", QUEUED);
    public static ExportJobStatus Running => new("Running", RUNNING);
    public static ExportJobStatus Completed => new("Completed", COMPLETED);
    public static ExportJobStatus Failed => new("Failed", FAILED);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(ExportJobStatus status)
    {
        return status.DeveloperName;
    }

    public static explicit operator ExportJobStatus(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<ExportJobStatus> SupportedTypes
    {
        get
        {
            yield return Queued;
            yield return Running;
            yield return Completed;
            yield return Failed;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class ExportJobStatusNotFoundException : Exception
{
    public ExportJobStatusNotFoundException(string developerName)
        : base($"Export job status '{developerName}' is not supported.")
    {
    }
}

