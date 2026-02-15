namespace App.Domain.ValueObjects;

public class AppointmentStatus : ValueObject
{
    public const string SCHEDULED = "scheduled";
    public const string CONFIRMED = "confirmed";
    public const string IN_PROGRESS = "in_progress";
    public const string COMPLETED = "completed";
    public const string CANCELLED = "cancelled";
    public const string NO_SHOW = "no_show";

    // Valid forward transitions (from → allowed targets)
    private static readonly Dictionary<string, HashSet<string>> _validTransitions = new()
    {
        { SCHEDULED, new HashSet<string> { CONFIRMED, CANCELLED, NO_SHOW } },
        { CONFIRMED, new HashSet<string> { IN_PROGRESS, CANCELLED, NO_SHOW } },
        { IN_PROGRESS, new HashSet<string> { COMPLETED, CANCELLED, NO_SHOW } },
        { COMPLETED, new HashSet<string>() },
        { CANCELLED, new HashSet<string>() },
        { NO_SHOW, new HashSet<string>() },
    };

    static AppointmentStatus() { }

    public AppointmentStatus() { }

    private AppointmentStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static AppointmentStatus From(string developerName)
    {
        if (string.IsNullOrEmpty(developerName))
        {
            throw new AppointmentStatusNotFoundException(developerName ?? "null");
        }

        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new AppointmentStatusNotFoundException(developerName);
        }

        return type;
    }

    public static AppointmentStatus Scheduled => new("Scheduled", SCHEDULED);
    public static AppointmentStatus Confirmed => new("Confirmed", CONFIRMED);
    public static AppointmentStatus InProgress => new("In Progress", IN_PROGRESS);
    public static AppointmentStatus Completed => new("Completed", COMPLETED);
    public static AppointmentStatus Cancelled => new("Cancelled", CANCELLED);
    public static AppointmentStatus NoShow => new("No-Show", NO_SHOW);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this status is terminal (Cancelled or No-Show). Terminal statuses cannot transition to any other status.
    /// </summary>
    public bool IsTerminal => DeveloperName == CANCELLED || DeveloperName == NO_SHOW;

    /// <summary>
    /// Whether this status is active (Scheduled, Confirmed, or In Progress).
    /// </summary>
    public bool IsActive =>
        DeveloperName == SCHEDULED || DeveloperName == CONFIRMED || DeveloperName == IN_PROGRESS;

    /// <summary>
    /// Checks whether a transition from this status to the target status is valid.
    /// Valid transitions: Scheduled → Confirmed → In Progress → Completed (linear).
    /// Cancelled and No-Show reachable from any active status but are terminal.
    /// </summary>
    public bool CanTransitionTo(AppointmentStatus target)
    {
        if (_validTransitions.TryGetValue(DeveloperName, out var allowed))
        {
            return allowed.Contains(target.DeveloperName);
        }

        return false;
    }

    public static implicit operator string(AppointmentStatus status)
    {
        return status.DeveloperName;
    }

    public static explicit operator AppointmentStatus(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<AppointmentStatus> SupportedTypes
    {
        get
        {
            yield return Scheduled;
            yield return Confirmed;
            yield return InProgress;
            yield return Completed;
            yield return Cancelled;
            yield return NoShow;
        }
    }

    /// <summary>
    /// Returns only the active (non-terminal) statuses.
    /// </summary>
    public static IEnumerable<AppointmentStatus> ActiveStatuses
    {
        get
        {
            yield return Scheduled;
            yield return Confirmed;
            yield return InProgress;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class AppointmentStatusNotFoundException : Exception
{
    public AppointmentStatusNotFoundException(string developerName)
        : base($"Appointment status '{developerName}' is not supported.") { }
}
