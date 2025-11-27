using FocusDeck.Domain.Entities.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations
{
    public class ContextSnapshotConfiguration : IEntityTypeConfiguration<ContextSnapshot>
    {
        public void Configure(EntityTypeBuilder<ContextSnapshot> builder)
        {
            builder.ToTable("ContextSnapshots");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.UserId)
                .IsRequired();

            builder.Property(c => c.Timestamp)
                .IsRequired();

            builder.HasMany(c => c.Slices)
                .WithOne()
                .HasForeignKey("SnapshotId");

            builder.OwnsOne(c => c.Metadata);
        }
    }
}
