using FocusDeck.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class PairingSessionConfiguration : IEntityTypeConfiguration<PairingSession>
{
    public void Configure(EntityTypeBuilder<PairingSession> builder)
    {
        builder.ToTable("PairingSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(12).IsRequired();
        builder.Property(x => x.VaultKdfMetadataJson);
        builder.Property(x => x.VaultCipherSuite).HasMaxLength(64);
        builder.HasIndex(x => new { x.UserId, x.Code, x.Status });
        builder.HasIndex(x => x.Code);
        builder.HasIndex(x => x.ExpiresAt);
    }
}
