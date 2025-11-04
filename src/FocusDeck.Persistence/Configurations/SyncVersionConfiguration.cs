using FocusDeck.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class SyncVersionConfiguration : IEntityTypeConfiguration<SyncVersion>
{
    public void Configure(EntityTypeBuilder<SyncVersion> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();
        builder.Property(e => e.CreatedAt)
            .IsRequired();
    }
}
