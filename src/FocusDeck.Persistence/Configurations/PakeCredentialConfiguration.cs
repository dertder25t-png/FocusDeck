using FocusDeck.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class PakeCredentialConfiguration : IEntityTypeConfiguration<PakeCredential>
{
    public void Configure(EntityTypeBuilder<PakeCredential> builder)
    {
        builder.ToTable("PakeCredentials");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).HasMaxLength(320);
        builder.Property(x => x.SaltBase64).IsRequired();
        builder.Property(x => x.VerifierBase64).IsRequired();
        builder.Property(x => x.Algorithm).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ModulusHex).IsRequired();
        builder.Property(x => x.Generator).IsRequired();
        builder.Property(x => x.KdfParametersJson);
    }
}
