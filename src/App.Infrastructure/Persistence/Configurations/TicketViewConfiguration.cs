using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class TicketViewConfiguration : IEntityTypeConfiguration<TicketView>
{
    public void Configure(EntityTypeBuilder<TicketView> builder)
    {
        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.SortPrimaryField)
            .HasMaxLength(100);

        builder.Property(v => v.SortPrimaryDirection)
            .HasMaxLength(10);

        builder.Property(v => v.SortSecondaryField)
            .HasMaxLength(100);

        builder.Property(v => v.SortSecondaryDirection)
            .HasMaxLength(10);

        builder.HasOne(v => v.OwnerStaff)
            .WithMany()
            .HasForeignKey(v => v.OwnerStaffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasIndex(v => v.OwnerStaffId);
        builder.HasIndex(v => v.IsDefault);
        builder.HasIndex(v => v.IsSystem);
    }
}

