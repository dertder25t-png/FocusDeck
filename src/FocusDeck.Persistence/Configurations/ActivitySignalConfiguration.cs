using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public sealed class ActivitySignalConfiguration : IEntityTypeConfiguration<ActivitySignal>
{
    public void Configure(EntityTypeBuilder<ActivitySignal> builder)
    {
        builder.ToTable("ActivitySignals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SignalType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SignalValue)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.SourceApp)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.MetadataJson)
            .HasMaxLength(4000);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CapturedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CapturedAtUtc);
    }
}
