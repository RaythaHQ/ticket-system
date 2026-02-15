using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class StaffBlockOutTimeConfiguration : IEntityTypeConfiguration<StaffBlockOutTime>
{
    public void Configure(EntityTypeBuilder<StaffBlockOutTime> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title).IsRequired().HasMaxLength(250);
        builder.Property(b => b.Reason).HasMaxLength(1000);
        builder.Property(b => b.StartTimeUtc).IsRequired();
        builder.Property(b => b.EndTimeUtc).IsRequired();
        builder.Property(b => b.IsAllDay).IsRequired().HasDefaultValue(false);

        builder
            .HasOne(b => b.StaffMember)
            .WithMany(s => s.BlockOutTimes)
            .HasForeignKey(b => b.StaffMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Indexes for efficient querying by staff member and date range
        builder.HasIndex(b => b.StaffMemberId);
        builder.HasIndex(b => new { b.StaffMemberId, b.StartTimeUtc, b.EndTimeUtc });
    }
}
