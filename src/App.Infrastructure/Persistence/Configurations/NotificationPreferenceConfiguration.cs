using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.Property(p => p.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.WebhookUrl)
            .HasMaxLength(2000);

        builder.HasOne(p => p.StaffAdmin)
            .WithMany()
            .HasForeignKey(p => p.StaffAdminId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Unique constraint: one preference per event type per user
        builder.HasIndex(p => new { p.StaffAdminId, p.EventType }).IsUnique();
    }
}

