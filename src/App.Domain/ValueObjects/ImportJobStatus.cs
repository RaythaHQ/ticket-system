namespace App.Domain.ValueObjects;

public class ImportJobStatus : ValueObject
{
    public const string QUEUED = "queued";
    public const string RUNNING = "running";
    public const string COMPLETED = "completed";
    public const string FAILED = "failed";

    static ImportJobStatus() { }

    public ImportJobStatus() { }

    private ImportJobStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static ImportJobStatus From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new ImportJobStatusNotFoundException(developerName);
        }

        return type;
    }

    public static ImportJobStatus Queued => new("Queued", QUEUED);
    public static ImportJobStatus Running => new("Running", RUNNING);
    public static ImportJobStatus Completed => new("Completed", COMPLETED);
    public static ImportJobStatus Failed => new("Failed", FAILED);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(ImportJobStatus status)
    {
        return status.DeveloperName;
    }

    public static explicit operator ImportJobStatus(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<ImportJobStatus> SupportedTypes
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

public class ImportJobStatusNotFoundException : Exception
{
    public ImportJobStatusNotFoundException(string developerName)
        : base($"Import job status '{developerName}' is not supported.") { }
}
