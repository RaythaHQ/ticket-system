using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class UserFavoriteViewConfiguration : IEntityTypeConfiguration<UserFavoriteView>
{
    public void Configure(EntityTypeBuilder<UserFavoriteView> builder)
    {
        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.TicketView)
            .WithMany()
            .HasForeignKey(f => f.TicketViewId)
            .OnDelete(DeleteBehavior.Cascade);

        // Each user can only favorite a view once
        builder.HasIndex(f => new { f.UserId, f.TicketViewId }).IsUnique();
        
        builder.HasIndex(f => f.UserId);
    }
}

