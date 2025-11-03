using FocusDeck.Domain.Entities.Remote;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class RemoteActionConfiguration : IEntityTypeConfiguration<RemoteAction>
{
    public void Configure(EntityTypeBuilder<RemoteAction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Kind).IsRequired();
        builder.Property(e => e.PayloadJson).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CompletedAt);
        builder.Property(e => e.Success);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.UserId, e.CompletedAt });
    }
}
