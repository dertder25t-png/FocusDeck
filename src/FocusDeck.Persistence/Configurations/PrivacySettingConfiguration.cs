using FocusDeck.Domain.Entities;
using FocusDeck.SharedKernel.Privacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public sealed class PrivacySettingConfiguration : IEntityTypeConfiguration<PrivacySetting>
{
    public void Configure(EntityTypeBuilder<PrivacySetting> builder)
    {
        builder.ToTable("PrivacySettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ContextType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.Tier)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(PrivacyTier.Medium);

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.ContextType })
            .IsUnique();
    }
}
