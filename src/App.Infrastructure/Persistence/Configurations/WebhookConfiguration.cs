using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);

        builder.Property(w => w.Url).IsRequired().HasMaxLength(2000);

        builder.Property(w => w.TriggerType).IsRequired().HasMaxLength(50);

        builder.Property(w => w.Description).HasMaxLength(1000);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasIndex(w => w.IsActive);
        builder.HasIndex(w => w.TriggerType);
        builder.HasIndex(w => new { w.TriggerType, w.IsActive });
    }
}
