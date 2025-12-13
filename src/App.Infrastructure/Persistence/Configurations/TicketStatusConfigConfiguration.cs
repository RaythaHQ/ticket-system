using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;
using App.Domain.ValueObjects;

namespace App.Infrastructure.Persistence.Configurations;

public class TicketStatusConfigConfiguration : IEntityTypeConfiguration<TicketStatusConfig>
{
    public void Configure(EntityTypeBuilder<TicketStatusConfig> builder)
    {
        builder.Property(s => s.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.DeveloperName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.ColorName)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("secondary");

        builder.Property(s => s.StatusType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(TicketStatusType.OPEN);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasIndex(s => s.DeveloperName).IsUnique();
        builder.HasIndex(s => s.SortOrder);
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.StatusType);
    }
}

