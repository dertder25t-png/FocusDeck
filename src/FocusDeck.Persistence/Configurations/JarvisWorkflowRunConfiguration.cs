using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public sealed class JarvisWorkflowRunConfiguration : IEntityTypeConfiguration<JarvisWorkflowRun>
{
    public void Configure(EntityTypeBuilder<JarvisWorkflowRun> builder)
    {
        builder.ToTable("JarvisWorkflowRuns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkflowId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.RequestedByUserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.LogSummary)
            .HasMaxLength(2000);

        builder.Property(x => x.JobId)
            .HasMaxLength(200);

        builder.Property(x => x.RequestedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.WorkflowId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequestedAtUtc);
    }
}

