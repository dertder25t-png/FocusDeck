using FocusDeck.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FocusDeck.Persistence.Configurations;

public class SyncMetadataConfiguration : IEntityTypeConfiguration<SyncMetadata>
{
    public void Configure(EntityTypeBuilder<SyncMetadata> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DeviceId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.LastSyncVersion).IsRequired();
        builder.Property(e => e.LastSyncTime).IsRequired();
        builder.Property(e => e.EntityVersions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<SyncEntityType, long>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<SyncEntityType, long>())
            .HasColumnType("TEXT");

        builder.HasIndex(e => e.DeviceId).IsUnique();
    }
}
