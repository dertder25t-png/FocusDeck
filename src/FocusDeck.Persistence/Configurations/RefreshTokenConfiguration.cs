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
        builder.Property(e => e.Token).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.RevokedAt);
        builder.Property(e => e.ReplacedByToken).HasMaxLength(500);

        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ExpiresAt);

        // Ignore computed properties
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.IsRevoked);
        builder.Ignore(e => e.IsActive);
    }
}
