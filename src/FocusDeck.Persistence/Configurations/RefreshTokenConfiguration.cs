using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TokenHash).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ClientFingerprint).IsRequired().HasMaxLength(500);
        builder.Property(e => e.IssuedUtc).IsRequired();
        builder.Property(e => e.ExpiresUtc).IsRequired();
        builder.Property(e => e.RevokedUtc);
        builder.Property(e => e.ReplacedByTokenHash).HasMaxLength(500);

        builder.HasIndex(e => e.TokenHash).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ExpiresUtc);
        builder.HasIndex(e => e.RevokedUtc);

        // Ignore computed properties
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.IsRevoked);
        builder.Ignore(e => e.IsActive);
    }
}
