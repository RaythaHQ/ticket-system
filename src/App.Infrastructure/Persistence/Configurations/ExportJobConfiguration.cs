using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        builder.HasKey(e => e.Id);

        // Status stored as string
        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                v => v.DeveloperName,
                v => Domain.ValueObjects.ExportJobStatus.From(v)
            );

        builder.Property(e => e.ProgressStage)
            .HasMaxLength(100);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        // JSON column for snapshot payload
        builder.Property(e => e.SnapshotPayloadJson)
            .IsRequired()
            .HasColumnType("jsonb");

        // Relationships
        builder.HasOne(e => e.Requester)
            .WithMany()
            .HasForeignKey(e => e.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.MediaItem)
            .WithMany()
            .HasForeignKey(e => e.MediaItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.BackgroundTask)
            .WithMany()
            .HasForeignKey(e => e.BackgroundTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Indexes
        builder.HasIndex(e => e.RequesterUserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => new { e.ExpiresAt, e.IsCleanedUp });
    }
}

