using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public sealed class StudentWellnessMetricsConfiguration : IEntityTypeConfiguration<StudentWellnessMetrics>
{
    public void Configure(EntityTypeBuilder<StudentWellnessMetrics> builder)
    {
        builder.ToTable("StudentWellnessMetrics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CapturedAtUtc)
            .IsRequired();

        builder.Property(x => x.HoursWorked)
            .IsRequired();

        builder.Property(x => x.BreakFrequency)
            .IsRequired();

        builder.Property(x => x.QualityScore)
            .IsRequired();

        builder.Property(x => x.SleepHours)
            .IsRequired();

        builder.Property(x => x.IsUnsustainable)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CapturedAtUtc);
    }
}
