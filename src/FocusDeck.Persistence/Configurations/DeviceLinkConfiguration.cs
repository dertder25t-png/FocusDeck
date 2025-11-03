using FocusDeck.Domain.Entities.Remote;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class DeviceLinkConfiguration : IEntityTypeConfiguration<DeviceLink>
{
    public void Configure(EntityTypeBuilder<DeviceLink> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.DeviceType).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.CapabilitiesJson).IsRequired();
        builder.Property(e => e.LastSeenUtc).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.DeviceType });
    }
}
