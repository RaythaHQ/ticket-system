using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations;

public class JwtLoginConfiguration : IEntityTypeConfiguration<JwtLogin>
{
    public void Configure(EntityTypeBuilder<JwtLogin> builder)
    {
        builder.HasIndex(b => b.Jti).IsUnique();
    }
}
