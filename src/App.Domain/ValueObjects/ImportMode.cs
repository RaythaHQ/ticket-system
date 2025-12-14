namespace App.Domain.ValueObjects;

public class ImportMode : ValueObject
{
    public const string INSERT_IF_NOT_EXISTS = "insert_if_not_exists";
    public const string UPDATE_EXISTING_ONLY = "update_existing_only";
    public const string UPSERT = "upsert";

    static ImportMode() { }

    public ImportMode() { }

    private ImportMode(string label, string developerName, string description)
    {
        Label = label;
        DeveloperName = developerName;
        Description = description;
    }

    public static ImportMode From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new ImportModeNotFoundException(developerName);
        }

        return type;
    }

    public static ImportMode InsertIfNotExists =>
        new(
            "Insert If Not Exists",
            INSERT_IF_NOT_EXISTS,
            "Only insert new records. Skip rows where the ID already exists in the database."
        );

    public static ImportMode UpdateExistingOnly =>
        new(
            "Update Existing Only",
            UPDATE_EXISTING_ONLY,
            "Only update existing records. Skip rows where the ID does not exist in the database."
        );

    public static ImportMode Upsert =>
        new("Upsert", UPSERT, "Insert new records and update existing ones based on ID.");

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public static implicit operator string(ImportMode mode)
    {
        return mode.DeveloperName;
    }

    public static explicit operator ImportMode(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<ImportMode> SupportedTypes
    {
        get
        {
            yield return InsertIfNotExists;
            yield return UpdateExistingOnly;
            yield return Upsert;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class ImportModeNotFoundException : Exception
{
    public ImportModeNotFoundException(string developerName)
        : base($"Import mode '{developerName}' is not supported.") { }
}
