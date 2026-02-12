using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class TaskTemplateConfiguration : IEntityTypeConfiguration<TaskTemplate>
{
    public void Configure(EntityTypeBuilder<TaskTemplate> builder)
    {
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(1000);

        // Items relationship with cascade delete
        builder.HasMany(t => t.Items)
            .WithOne(i => i.TaskTemplate)
            .HasForeignKey(i => i.TaskTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
