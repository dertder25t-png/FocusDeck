using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class DesignProjectConfiguration : IEntityTypeConfiguration<DesignProject>
{
    public void Configure(EntityTypeBuilder<DesignProject> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserId).IsRequired().HasMaxLength(256);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.GoalsText).HasMaxLength(2000);
        builder.Property(p => p.RequirementsText).HasMaxLength(2000);
        
        builder.Property(p => p.Vibes)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
            
        builder.Property(p => p.BrandKeywords)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
        
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.CreatedAt);
        
        builder.HasMany(p => p.Ideas)
            .WithOne(i => i.Project)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DesignIdeaConfiguration : IEntityTypeConfiguration<DesignIdea>
{
    public void Configure(EntityTypeBuilder<DesignIdea> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Content).IsRequired();
        builder.Property(i => i.Type).HasConversion<string>();
        
        builder.HasIndex(i => i.ProjectId);
        builder.HasIndex(i => i.CreatedAt);
        builder.HasIndex(i => new { i.ProjectId, i.IsPinned });
    }
}
