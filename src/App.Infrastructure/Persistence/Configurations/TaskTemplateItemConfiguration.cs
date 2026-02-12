using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class TaskTemplateItemConfiguration : IEntityTypeConfiguration<TaskTemplateItem>
{
    public void Configure(EntityTypeBuilder<TaskTemplateItem> builder)
    {
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);

        // FK to TaskTemplate (cascade handled in parent config)
        builder.HasOne(t => t.TaskTemplate)
            .WithMany(template => template.Items)
            .HasForeignKey(t => t.TaskTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing dependency — SET NULL on delete
        builder.HasOne(t => t.DependsOnItem)
            .WithMany()
            .HasForeignKey(t => t.DependsOnItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Indexes
        builder.HasIndex(t => t.TaskTemplateId);
    }
}
