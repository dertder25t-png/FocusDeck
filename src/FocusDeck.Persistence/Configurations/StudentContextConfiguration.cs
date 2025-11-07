using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class StudentContextConfiguration : IEntityTypeConfiguration<StudentContext>
{
    public void Configure(EntityTypeBuilder<StudentContext> builder)
    {
        builder.ToTable("StudentContexts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Timestamp).IsRequired();
        builder.Property(x => x.FocusedAppName).HasMaxLength(200);
        builder.Property(x => x.FocusedWindowTitle).HasMaxLength(500);
        builder.Property(x => x.ActivityIntensity).IsRequired();
        builder.Property(x => x.IsIdle).IsRequired();
        builder.Property(x => x.OpenContextsJson);

        builder.HasIndex(x => new { x.UserId, x.Timestamp }).HasDatabaseName("IX_StudentContexts_User_Timestamp");
    }
}

