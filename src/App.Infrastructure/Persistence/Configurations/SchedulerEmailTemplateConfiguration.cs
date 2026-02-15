using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class SchedulerEmailTemplateConfiguration
    : IEntityTypeConfiguration<SchedulerEmailTemplate>
{
    public void Configure(EntityTypeBuilder<SchedulerEmailTemplate> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TemplateType).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Channel).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Subject).HasMaxLength(500);
        builder.Property(t => t.Content).IsRequired();

        // Unique composite: one template per (type, channel) combination
        builder.HasIndex(t => new { t.TemplateType, t.Channel }).IsUnique();

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);
    }
}
