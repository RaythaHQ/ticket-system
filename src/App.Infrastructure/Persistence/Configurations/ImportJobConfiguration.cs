using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.HasKey(e => e.Id);

        // EntityType stored as string
        builder
            .Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(v => v.DeveloperName, v => Domain.ValueObjects.ImportEntityType.From(v));

        // Mode stored as string
        builder
            .Property(e => e.Mode)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(v => v.DeveloperName, v => Domain.ValueObjects.ImportMode.From(v));

        // Status stored as string
        builder
            .Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(v => v.DeveloperName, v => Domain.ValueObjects.ImportJobStatus.From(v));

        builder.Property(e => e.ProgressStage).HasMaxLength(100);

        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        // Relationships
        builder
            .HasOne(e => e.Requester)
            .WithMany()
            .HasForeignKey(e => e.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.SourceMediaItem)
            .WithMany()
            .HasForeignKey(e => e.SourceMediaItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.ErrorMediaItem)
            .WithMany()
            .HasForeignKey(e => e.ErrorMediaItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(e => e.BackgroundTask)
            .WithMany()
            .HasForeignKey(e => e.BackgroundTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Indexes
        builder.HasIndex(e => e.RequesterUserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.EntityType);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => new { e.ExpiresAt, e.IsCleanedUp });
    }
}
