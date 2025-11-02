using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.FileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.SizeInBytes)
            .IsRequired();

        builder.Property(a => a.StoragePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.UploadedAt)
            .IsRequired();

        builder.Property(a => a.UploadedBy)
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        // Store metadata as JSON
        builder.Property(a => a.Metadata)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.HasIndex(a => a.UploadedAt);
        builder.HasIndex(a => a.UploadedBy);
    }
}
