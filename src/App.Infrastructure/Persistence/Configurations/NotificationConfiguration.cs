using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.Url)
            .HasMaxLength(2000);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Ticket)
            .WithMany()
            .HasForeignKey(n => n.TicketId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        // Primary query: user's unread notifications, newest first
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_RecipientUserId_IsRead_CreatedAt")
            .IsDescending(false, false, true);

        // Secondary: filter by type
        builder.HasIndex(n => new { n.RecipientUserId, n.EventType, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_RecipientUserId_EventType_CreatedAt")
            .IsDescending(false, false, true);
    }
}

