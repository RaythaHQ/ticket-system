using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class WebhookLogConfiguration : IEntityTypeConfiguration<WebhookLog>
{
    public void Configure(EntityTypeBuilder<WebhookLog> builder)
    {
        builder.Property(l => l.TriggerType).IsRequired().HasMaxLength(50);

        builder.Property(l => l.PayloadJson).IsRequired();

        builder.Property(l => l.ErrorMessage).HasMaxLength(2000);

        builder.Property(l => l.ResponseBody).HasMaxLength(1024);

        builder
            .HasOne(l => l.Webhook)
            .WithMany(w => w.Logs)
            .HasForeignKey(l => l.WebhookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.WebhookId);
        builder.HasIndex(l => l.TicketId);
        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.Success);
        builder.HasIndex(l => new { l.WebhookId, l.CreatedAt });
    }
}
