using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class ServiceConfigurationConfiguration : IEntityTypeConfiguration<ServiceConfiguration>
{
    public void Configure(EntityTypeBuilder<ServiceConfiguration> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ServiceName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ClientId).HasMaxLength(500);
        builder.Property(e => e.ClientSecret).HasMaxLength(500);
        builder.Property(e => e.ApiKey).HasMaxLength(500);
        builder.Property(e => e.AdditionalConfig);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.ServiceName).IsUnique();
    }
}
