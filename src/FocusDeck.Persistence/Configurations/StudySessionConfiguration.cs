using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.HasKey(e => e.SessionId);
        builder.Property(e => e.SessionId).ValueGeneratedNever();
        builder.Property(e => e.StartTime).IsRequired();
        builder.Property(e => e.EndTime);
        builder.Property(e => e.DurationMinutes).IsRequired();
        builder.Property(e => e.SessionNotes);
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.FocusRate);
        builder.Property(e => e.BreaksCount).HasDefaultValue(0);
        builder.Property(e => e.BreakDurationMinutes).HasDefaultValue(0);
        builder.Property(e => e.Category).HasMaxLength(120);

        builder.HasIndex(e => e.StartTime);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Category);
    }
}
