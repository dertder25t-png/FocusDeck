using FocusDeck.Domain.Entities.Automations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecution>
{
    public void Configure(EntityTypeBuilder<AutomationExecution> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AutomationId).IsRequired();
        builder.Property(e => e.ExecutedAt).IsRequired();
        builder.Property(e => e.Success).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.DurationMs).IsRequired();
        builder.Property(e => e.TriggerData).HasMaxLength(1000);

        builder.HasIndex(e => e.AutomationId);
        builder.HasIndex(e => e.ExecutedAt);
        builder.HasIndex(e => new { e.AutomationId, e.ExecutedAt });
    }
}
