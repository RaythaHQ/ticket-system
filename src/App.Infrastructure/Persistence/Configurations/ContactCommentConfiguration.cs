using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class ContactCommentConfiguration : IEntityTypeConfiguration<ContactComment>
{
    public void Configure(EntityTypeBuilder<ContactComment> builder)
    {
        builder.HasOne(c => c.Contact)
            .WithMany(contact => contact.Comments)
            .HasForeignKey(c => c.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.AuthorStaff)
            .WithMany()
            .HasForeignKey(c => c.AuthorStaffId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(c => c.Body)
            .IsRequired();

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasIndex(c => c.ContactId);
        builder.HasIndex(c => c.CreationTime);
    }
}
