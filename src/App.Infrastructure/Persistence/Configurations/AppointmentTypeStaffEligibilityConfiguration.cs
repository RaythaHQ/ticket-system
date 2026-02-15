using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class AppointmentTypeStaffEligibilityConfiguration
    : IEntityTypeConfiguration<AppointmentTypeStaffEligibility>
{
    public void Configure(EntityTypeBuilder<AppointmentTypeStaffEligibility> builder)
    {
        builder.HasKey(e => e.Id);

        // Unique composite index: one eligibility record per (type, staff) pair
        builder.HasIndex(e => new { e.AppointmentTypeId, e.SchedulerStaffMemberId }).IsUnique();

        // Relationships with cascade delete
        builder
            .HasOne(e => e.AppointmentType)
            .WithMany(t => t.EligibleStaff)
            .HasForeignKey(e => e.AppointmentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.SchedulerStaffMember)
            .WithMany(s => s.EligibleAppointmentTypes)
            .HasForeignKey(e => e.SchedulerStaffMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);
    }
}
