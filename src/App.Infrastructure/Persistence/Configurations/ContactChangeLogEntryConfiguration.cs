using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class ContactChangeLogEntryConfiguration : IEntityTypeConfiguration<ContactChangeLogEntry>
{
    public void Configure(EntityTypeBuilder<ContactChangeLogEntry> builder)
    {
        builder.HasOne(e => e.Contact)
            .WithMany(c => c.ChangeLogEntries)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ActorStaff)
            .WithMany()
            .HasForeignKey(e => e.ActorStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Message)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.ContactId);
        builder.HasIndex(e => e.CreationTime);
    }
}

