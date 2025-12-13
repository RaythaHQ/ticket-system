using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class TicketPriorityConfigConfiguration : IEntityTypeConfiguration<TicketPriorityConfig>
{
    public void Configure(EntityTypeBuilder<TicketPriorityConfig> builder)
    {
        builder.Property(p => p.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DeveloperName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.ColorName)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("secondary");

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasIndex(p => p.DeveloperName).IsUnique();
        builder.HasIndex(p => p.SortOrder);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsDefault);
    }
}

