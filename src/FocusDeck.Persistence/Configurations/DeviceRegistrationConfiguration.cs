using FocusDeck.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class DeviceRegistrationConfiguration : IEntityTypeConfiguration<DeviceRegistration>
{
    public void Configure(EntityTypeBuilder<DeviceRegistration> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DeviceId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.DeviceName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Platform).IsRequired();
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.RegisteredAt).IsRequired();
        builder.Property(e => e.LastSyncAt).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.AppVersion).HasMaxLength(50);

        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.DeviceId, e.UserId });
    }
}
