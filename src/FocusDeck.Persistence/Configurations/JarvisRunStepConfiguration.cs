using FocusDeck.Domain.Entities.Jarvis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations
{
    public class JarvisRunStepConfiguration : IEntityTypeConfiguration<JarvisRunStep>
    {
        public void Configure(EntityTypeBuilder<JarvisRunStep> builder)
        {
            builder.ToTable("JarvisRunSteps");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.RequestJson)
                .HasColumnType("jsonb");

            builder.Property(s => s.ResponseJson)
                .HasColumnType("jsonb");

            builder.Property(s => s.ErrorJson)
                .HasColumnType("jsonb");
        }
    }
}
