using FocusDeck.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class SyncChangeConfiguration : IEntityTypeConfiguration<SyncChange>
{
    public void Configure(EntityTypeBuilder<SyncChange> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TransactionId).IsRequired();
        builder.Property(e => e.EntityType).IsRequired();
        builder.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Operation).IsRequired();
        builder.Property(e => e.DataJson).IsRequired();
        builder.Property(e => e.ChangedAt).IsRequired();
        builder.Property(e => e.ChangeVersion).IsRequired();

        builder.HasIndex(e => e.TransactionId);
        builder.HasIndex(e => e.EntityId);
        builder.HasIndex(e => e.ChangeVersion);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
    }
}
