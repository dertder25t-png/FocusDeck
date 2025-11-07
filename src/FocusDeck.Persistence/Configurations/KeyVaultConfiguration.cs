using FocusDeck.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class KeyVaultConfiguration : IEntityTypeConfiguration<KeyVault>
{
    public void Configure(EntityTypeBuilder<KeyVault> builder)
    {
        builder.ToTable("KeyVaults");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).HasMaxLength(320);
        builder.Property(x => x.VaultDataBase64).IsRequired();
        builder.Property(x => x.Version).HasDefaultValue(1);
        builder.Property(x => x.CipherSuite).IsRequired().HasMaxLength(64).HasDefaultValue("AES-256-GCM");
        builder.Property(x => x.KdfMetadataJson);
    }
}
