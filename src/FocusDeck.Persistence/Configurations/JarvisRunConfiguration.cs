using FocusDeck.Domain.Entities.Jarvis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations
{
    public class JarvisRunConfiguration : IEntityTypeConfiguration<JarvisRun>
    {
        public void Configure(EntityTypeBuilder<JarvisRun> builder)
        {
            builder.ToTable("JarvisRuns");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.HasMany(r => r.Steps)
                .WithOne(s => s.Run)
                .HasForeignKey(s => s.RunId);
        }
    }
}
