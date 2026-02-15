namespace App.Domain.Entities;

/// <summary>
/// Junction entity linking appointment types to eligible staff members.
/// Only staff members on this list appear as assignees when creating an appointment of the linked type.
/// </summary>
public class AppointmentTypeStaffEligibility : BaseAuditableEntity
{
    public Guid AppointmentTypeId { get; set; }
    public virtual AppointmentType AppointmentType { get; set; } = null!;

    public Guid SchedulerStaffMemberId { get; set; }
    public virtual SchedulerStaffMember SchedulerStaffMember { get; set; } = null!;
}
