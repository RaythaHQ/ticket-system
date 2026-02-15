using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class AppointmentTypeConfiguration : IEntityTypeConfiguration<AppointmentType>
{
    public void Configure(EntityTypeBuilder<AppointmentType> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Mode).IsRequired().HasMaxLength(50);

        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => a.SortOrder);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);
    }
}
