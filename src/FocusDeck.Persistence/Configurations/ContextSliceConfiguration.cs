using FocusDeck.Domain.Entities.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations
{
    public class ContextSliceConfiguration : IEntityTypeConfiguration<ContextSlice>
    {
        public void Configure(EntityTypeBuilder<ContextSlice> builder)
        {
            builder.ToTable("ContextSlices");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.SourceType)
                .IsRequired();

            builder.Property(c => c.Timestamp)
                .IsRequired();

            builder.Property(c => c.Data)
                .HasConversion(
                    v => v == null ? null : v.ToString(),
                    v => v == null ? null : System.Text.Json.Nodes.JsonNode.Parse(v, null, default)!.AsObject());
        }
    }
}
