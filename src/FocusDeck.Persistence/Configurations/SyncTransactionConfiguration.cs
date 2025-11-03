using FocusDeck.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class SyncTransactionConfiguration : IEntityTypeConfiguration<SyncTransaction>
{
    public void Configure(EntityTypeBuilder<SyncTransaction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DeviceId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.Ignore(e => e.Changes); // Changes stored separately in SyncChanges table

        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.Timestamp);
    }
}
