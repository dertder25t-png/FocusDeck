using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FocusDeck.Persistence.Configurations;

public class FocusSessionConfiguration : IEntityTypeConfiguration<FocusSession>
{
    public void Configure(EntityTypeBuilder<FocusSession> builder)
    {
        builder.ToTable("FocusSessions");

        builder.HasKey(fs => fs.Id);

        builder.Property(fs => fs.UserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fs => fs.StartTime)
            .IsRequired();

        builder.Property(fs => fs.EndTime);

        builder.Property(fs => fs.Status)
            .IsRequired();

        builder.Property(fs => fs.DistractionsCount)
            .IsRequired();

        builder.Property(fs => fs.LastRecoverySuggestionAt);

        builder.Property(fs => fs.CreatedAt)
            .IsRequired();

        builder.Property(fs => fs.UpdatedAt)
            .IsRequired();

        // Configure Policy as JSON column using conversion
        builder.Property(fs => fs.Policy)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<FocusPolicy>(v, (JsonSerializerOptions?)null) ?? new FocusPolicy()
            );

        // Configure Signals as JSON column
        builder.Property(fs => fs.Signals)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<FocusSignal>>(v, (JsonSerializerOptions?)null) ?? new List<FocusSignal>()
            );

        // Indexes for efficient queries
        builder.HasIndex(fs => fs.UserId);
        builder.HasIndex(fs => fs.Status);
        builder.HasIndex(fs => new { fs.UserId, fs.Status });
        builder.HasIndex(fs => fs.StartTime);
    }
}
