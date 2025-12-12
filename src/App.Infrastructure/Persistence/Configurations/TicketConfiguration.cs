using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        // Use identity column for numeric ID
        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Category)
            .HasMaxLength(200);

        builder.Property(t => t.SlaStatus)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(t => t.OwningTeam)
            .WithMany(team => team.Tickets)
            .HasForeignKey(t => t.OwningTeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.CreatedByStaff)
            .WithMany()
            .HasForeignKey(t => t.CreatedByStaffId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Contact)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.SlaRule)
            .WithMany()
            .HasForeignKey(t => t.SlaRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Indexes
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.AssigneeId);
        builder.HasIndex(t => t.OwningTeamId);
        builder.HasIndex(t => t.ContactId);
        builder.HasIndex(t => t.SlaDueAt);
        builder.HasIndex(t => t.CreationTime);
    }
}

