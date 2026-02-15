using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class SchedulerStaffMemberConfiguration : IEntityTypeConfiguration<SchedulerStaffMember>
{
    public void Configure(EntityTypeBuilder<SchedulerStaffMember> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DefaultMeetingLink).HasMaxLength(2000);

        // One staff record per user
        builder.HasIndex(s => s.UserId).IsUnique();
        builder.HasIndex(s => s.IsActive);

        // Relationships
        builder
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);
    }
}
