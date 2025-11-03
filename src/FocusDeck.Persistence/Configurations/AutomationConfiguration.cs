using FocusDeck.Domain.Entities.Automations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FocusDeck.Persistence.Configurations;

public class AutomationConfiguration : IEntityTypeConfiguration<Automation>
{
    public void Configure(EntityTypeBuilder<Automation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.IsEnabled).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // Store trigger and actions as JSON
        builder.Property(e => e.Trigger)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<AutomationTrigger>(v, (JsonSerializerOptions?)null)!)
            .HasColumnType("TEXT");

        builder.Property(e => e.Actions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<AutomationAction>>(v, (JsonSerializerOptions?)null)!)
            .HasColumnType("TEXT");

        builder.HasIndex(e => e.IsEnabled);
        builder.HasIndex(e => e.CreatedAt);
    }
}
