using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class TicketTaskConfiguration : IEntityTypeConfiguration<TicketTask>
{
    public void Configure(EntityTypeBuilder<TicketTask> builder)
    {
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Status).IsRequired().HasMaxLength(50);

        // Relationships
        builder
            .HasOne(t => t.Ticket)
            .WithMany(ticket => ticket.Tasks)
            .HasForeignKey(t => t.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(t => t.OwningTeam)
            .WithMany()
            .HasForeignKey(t => t.OwningTeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(t => t.DependsOnTask)
            .WithMany(t => t.DependentTasks)
            .HasForeignKey(t => t.DependsOnTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(t => t.CreatedByStaff)
            .WithMany()
            .HasForeignKey(t => t.CreatedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(t => t.ClosedByStaff)
            .WithMany()
            .HasForeignKey(t => t.ClosedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Indexes
        builder.HasIndex(t => t.TicketId);
        builder.HasIndex(t => t.AssigneeId);
        builder.HasIndex(t => t.OwningTeamId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.DependsOnTaskId);
        builder.HasIndex(t => t.CreatedByStaffId);

        // Partial index for overdue queries — only index rows with a DueAt value
        builder
            .HasIndex(t => t.DueAt)
            .HasFilter("\"DueAt\" IS NOT NULL");
    }
}
