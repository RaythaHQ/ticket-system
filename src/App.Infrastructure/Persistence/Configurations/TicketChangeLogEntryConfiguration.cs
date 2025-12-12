using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class TicketChangeLogEntryConfiguration : IEntityTypeConfiguration<TicketChangeLogEntry>
{
    public void Configure(EntityTypeBuilder<TicketChangeLogEntry> builder)
    {
        builder.HasOne(e => e.Ticket)
            .WithMany(t => t.ChangeLogEntries)
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ActorStaff)
            .WithMany()
            .HasForeignKey(e => e.ActorStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Message)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.TicketId);
        builder.HasIndex(e => e.CreationTime);
    }
}

