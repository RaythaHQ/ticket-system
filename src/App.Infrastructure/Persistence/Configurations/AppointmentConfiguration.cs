using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        // Manual ID generation via INumericIdGenerator
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ContactFirstName).IsRequired().HasMaxLength(250);
        builder.Property(a => a.ContactLastName).HasMaxLength(250);
        builder.Property(a => a.ContactEmail).HasMaxLength(500);
        builder.Property(a => a.ContactPhone).HasMaxLength(50);
        builder.Property(a => a.ContactAddress).HasMaxLength(1000);
        builder.Property(a => a.Mode).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Status).IsRequired().HasMaxLength(50);
        builder.Property(a => a.MeetingLink).HasMaxLength(2000);
        builder.Property(a => a.CancellationReason).HasMaxLength(1000);
        builder.Property(a => a.CoverageZoneOverrideReason).HasMaxLength(1000);
        builder.Property(a => a.CancellationNoticeOverrideReason).HasMaxLength(1000);

        // Relationships
        builder
            .HasOne(a => a.Contact)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(a => a.AssignedStaffMember)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.AssignedStaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(a => a.AppointmentType)
            .WithMany(t => t.Appointments)
            .HasForeignKey(a => a.AppointmentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(a => a.CreatedByStaff)
            .WithMany()
            .HasForeignKey(a => a.CreatedByStaffId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Soft delete filter
        builder.HasQueryFilter(a => !a.IsDeleted);

        // Indexes
        builder.HasIndex(a => a.ContactId);
        builder.HasIndex(a => a.AssignedStaffMemberId);
        builder.HasIndex(a => a.AppointmentTypeId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.ScheduledStartTime);
        builder.HasIndex(a => a.CreationTime);

        // Partial index for reminder background job — only index unreminded active appointments
        builder
            .HasIndex(a => new { a.ReminderSentAt, a.Status, a.ScheduledStartTime })
            .HasFilter("\"ReminderSentAt\" IS NULL AND \"Status\" IN ('scheduled','confirmed')");
    }
}
