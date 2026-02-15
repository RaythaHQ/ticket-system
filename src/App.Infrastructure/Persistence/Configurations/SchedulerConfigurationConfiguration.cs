using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class SchedulerConfigurationConfiguration : IEntityTypeConfiguration<SchedulerConfiguration>
{
    public void Configure(EntityTypeBuilder<SchedulerConfiguration> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.AvailableHoursJson).IsRequired();

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);
    }
}
