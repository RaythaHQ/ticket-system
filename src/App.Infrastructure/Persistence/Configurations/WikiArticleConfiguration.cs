using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class WikiArticleConfiguration : IEntityTypeConfiguration<WikiArticle>
{
    public void Configure(EntityTypeBuilder<WikiArticle> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Content)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasMaxLength(200);

        builder.Property(e => e.Excerpt)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);
        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        // Indexes
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.IsPublished);
        builder.HasIndex(e => e.IsPinned);
        builder.HasIndex(e => new { e.Category, e.SortOrder });
    }
}
