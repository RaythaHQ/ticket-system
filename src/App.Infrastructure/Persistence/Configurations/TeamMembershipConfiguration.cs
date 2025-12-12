using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class TeamMembershipConfiguration : IEntityTypeConfiguration<TeamMembership>
{
    public void Configure(EntityTypeBuilder<TeamMembership> builder)
    {
        builder.HasOne(tm => tm.Team)
            .WithMany(t => t.Memberships)
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tm => tm.StaffAdmin)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(tm => tm.StaffAdminId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Unique constraint: one membership per user per team
        builder.HasIndex(tm => new { tm.TeamId, tm.StaffAdminId }).IsUnique();

        // Index for round-robin queries
        builder.HasIndex(tm => new { tm.TeamId, tm.IsAssignable, tm.LastAssignedAt });
    }
}

