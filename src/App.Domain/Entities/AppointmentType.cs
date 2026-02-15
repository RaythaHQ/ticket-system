namespace App.Domain.Entities;

/// <summary>
/// Configurable category of appointment with type-specific settings.
/// Examples: "Initial Consultation" (Virtual), "Home Visit" (In-Person), "Follow-Up" (Either).
/// </summary>
public class AppointmentType : BaseAuditableEntity
{
    /// <summary>
    /// Display name for this appointment type.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Appointment mode: "virtual", "in_person", or "either".
    /// Stored as AppointmentMode DeveloperName.
    /// </summary>
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Override org-wide default duration for this type. Null = use org default.
    /// </summary>
    public int? DefaultDurationMinutes { get; set; }

    /// <summary>
    /// Override org-wide default buffer time for this type. Null = use org default.
    /// </summary>
    public int? BufferTimeMinutes { get; set; }

    /// <summary>
    /// Override org-wide default booking horizon for this type. Null = use org default.
    /// </summary>
    public int? BookingHorizonDays { get; set; }

    /// <summary>
    /// Whether this type is active and available for scheduling.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display ordering for this type in lists and dropdowns.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // Navigation collections
    public virtual ICollection<AppointmentTypeStaffEligibility> EligibleStaff { get; set; } =
        new List<AppointmentTypeStaffEligibility>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
