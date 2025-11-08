using FocusDeck.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class RevokedAccessTokenConfiguration : IEntityTypeConfiguration<RevokedAccessToken>
{
    public void Configure(EntityTypeBuilder<RevokedAccessToken> builder)
    {
        builder.ToTable("RevokedAccessTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Jti).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.Jti).IsUnique();
        builder.HasIndex(x => x.ExpiresUtc);
    }
}
