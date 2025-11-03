using FocusDeck.Domain.Entities.Automations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class ConnectedServiceConfiguration : IEntityTypeConfiguration<ConnectedService>
{
    public void Configure(EntityTypeBuilder<ConnectedService> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Service).IsRequired();
        builder.Property(e => e.AccessToken).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.RefreshToken).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.ExpiresAt);
        builder.Property(e => e.ConnectedAt).IsRequired();
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(200);

        builder.HasIndex(e => e.Service);
        builder.HasIndex(e => e.UserId);
    }
}
