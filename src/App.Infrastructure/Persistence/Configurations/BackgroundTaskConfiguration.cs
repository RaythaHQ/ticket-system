using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;
using App.Domain.ValueObjects;

namespace App.Infrastructure.Persistence.Configurations;

public class BackgroundTaskConfiguration : IEntityTypeConfiguration<BackgroundTask>
{
    public void Configure(EntityTypeBuilder<BackgroundTask> builder)
    {
        builder
            .Property(b => b.Status)
            .HasConversion(v => v.DeveloperName, v => BackgroundTaskStatus.From(v));
    }
}
