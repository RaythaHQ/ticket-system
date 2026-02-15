namespace App.Application.Scheduler.RenderModels;

/// <summary>
/// Render model for scheduler email/SMS templates.
/// All merge variables available in Liquid templates via the {{ Target.PropertyName }} syntax.
/// </summary>
public class AppointmentNotification_RenderModel
{
    /// <summary>
    /// Human-readable appointment code (e.g., "APT-0001234").
    /// Template variable: {{ Target.AppointmentCode }}
    /// </summary>
    public string AppointmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Meeting URL for virtual appointments. Empty string for in-person.
    /// Template variable: {{ Target.MeetingLink }}
    /// </summary>
    public string MeetingLink { get; set; } = string.Empty;

    /// <summary>
    /// Appointment type name (e.g., "Initial Consultation").
    /// Template variable: {{ Target.AppointmentType }}
    /// </summary>
    public string AppointmentType { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the appointment mode (e.g., "Virtual", "In-Person").
    /// Template variable: {{ Target.AppointmentMode }}
    /// </summary>
    public string AppointmentMode { get; set; } = string.Empty;

    /// <summary>
    /// Formatted date/time in the organization's timezone.
    /// Template variable: {{ Target.DateTime }}
    /// </summary>
    public string DateTime { get; set; } = string.Empty;

    /// <summary>
    /// Duration display string (e.g., "30 minutes").
    /// Template variable: {{ Target.Duration }}
    /// </summary>
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// Assigned staff member's full name.
    /// Template variable: {{ Target.StaffName }}
    /// </summary>
    public string StaffName { get; set; } = string.Empty;

    /// <summary>
    /// Assigned staff member's email address.
    /// Template variable: {{ Target.StaffEmail }}
    /// </summary>
    public string StaffEmail { get; set; } = string.Empty;

    /// <summary>
    /// Contact's full name (from Contact record).
    /// Template variable: {{ Target.ContactName }}
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Contact's email address (from Contact record).
    /// Template variable: {{ Target.ContactEmail }}
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Contact's zipcode/postal code (from Contact record).
    /// Template variable: {{ Target.ContactZipcode }}
    /// </summary>
    public string ContactZipcode { get; set; } = string.Empty;

    /// <summary>
    /// Per-appointment contact first name (may differ from Contact record).
    /// Template variable: {{ Target.AppointmentContactFirstName }}
    /// </summary>
    public string AppointmentContactFirstName { get; set; } = string.Empty;

    /// <summary>
    /// Per-appointment contact last name.
    /// Template variable: {{ Target.AppointmentContactLastName }}
    /// </summary>
    public string AppointmentContactLastName { get; set; } = string.Empty;

    /// <summary>
    /// Per-appointment contact email.
    /// Template variable: {{ Target.AppointmentContactEmail }}
    /// </summary>
    public string AppointmentContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Per-appointment contact phone.
    /// Template variable: {{ Target.AppointmentContactPhone }}
    /// </summary>
    public string AppointmentContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Per-appointment contact address.
    /// Template variable: {{ Target.AppointmentContactAddress }}
    /// </summary>
    public string AppointmentContactAddress { get; set; } = string.Empty;

    /// <summary>
    /// Appointment notes.
    /// Template variable: {{ Target.Notes }}
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
