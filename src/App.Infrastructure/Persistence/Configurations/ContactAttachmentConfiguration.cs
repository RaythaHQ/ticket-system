using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class ContactAttachmentConfiguration : IEntityTypeConfiguration<ContactAttachment>
{
    public void Configure(EntityTypeBuilder<ContactAttachment> builder)
    {
        builder.HasOne(a => a.Contact)
            .WithMany(c => c.Attachments)
            .HasForeignKey(a => a.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.MediaItem)
            .WithMany()
            .HasForeignKey(a => a.MediaItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasIndex(a => a.ContactId);
        builder.HasIndex(a => a.MediaItemId);
    }
}

