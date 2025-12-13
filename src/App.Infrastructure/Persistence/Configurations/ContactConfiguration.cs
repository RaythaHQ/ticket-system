using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        // Use identity column for numeric ID, but allow manual assignment
        builder.Property(c => c.Id).ValueGeneratedOnAdd().UseIdentityColumn();

        // Note: Manual ID assignment requires special handling in the handler

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(500);

        builder.Property(c => c.Email).HasMaxLength(500);

        builder.Property(c => c.OrganizationAccount).HasMaxLength(500);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Soft delete filter
        builder.HasQueryFilter(c => !c.IsDeleted);

        // Indexes
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.CreationTime);
    }
}
