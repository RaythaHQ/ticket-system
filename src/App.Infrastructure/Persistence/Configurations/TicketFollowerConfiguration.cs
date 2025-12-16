using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class TicketFollowerConfiguration : IEntityTypeConfiguration<TicketFollower>
{
    public void Configure(EntityTypeBuilder<TicketFollower> builder)
    {
        builder
            .HasOne(f => f.Ticket)
            .WithMany(t => t.Followers)
            .HasForeignKey(f => f.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(f => f.StaffAdmin)
            .WithMany()
            .HasForeignKey(f => f.StaffAdminId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Unique constraint: a user can only follow a ticket once
        builder.HasIndex(f => new { f.TicketId, f.StaffAdminId }).IsUnique();

        builder.HasIndex(f => f.TicketId);
        builder.HasIndex(f => f.StaffAdminId);
    }
}
