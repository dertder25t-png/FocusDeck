using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FocusDeck.Persistence.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnType("TEXT");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Color).HasMaxLength(32);
        builder.Property(e => e.IsPinned).HasDefaultValue(false);
        builder.Property(e => e.CreatedDate).IsRequired();
        builder.Property(e => e.LastModified);
        builder.Property(e => e.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("TEXT");
        builder.Property(e => e.Bookmarks)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<NoteBookmark>>(v, (JsonSerializerOptions?)null) ?? new List<NoteBookmark>())
            .HasColumnType("TEXT");

        builder.HasIndex(e => e.IsPinned);
        builder.HasIndex(e => e.CreatedDate);
        builder.HasIndex(e => e.LastModified);
    }
}
