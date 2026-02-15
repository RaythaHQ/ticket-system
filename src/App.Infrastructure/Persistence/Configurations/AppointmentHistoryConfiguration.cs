using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class AppointmentHistoryConfiguration : IEntityTypeConfiguration<AppointmentHistory>
{
    public void Configure(EntityTypeBuilder<AppointmentHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ChangeType).IsRequired().HasMaxLength(50);
        builder.Property(h => h.OverrideReason).HasMaxLength(1000);

        // Relationships
        builder
            .HasOne(h => h.Appointment)
            .WithMany(a => a.History)
            .HasForeignKey(h => h.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(h => h.AppointmentId);
        builder.HasIndex(h => h.Timestamp);
    }
}
