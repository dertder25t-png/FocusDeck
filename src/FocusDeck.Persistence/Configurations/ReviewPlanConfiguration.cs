using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class ReviewPlanConfiguration : IEntityTypeConfiguration<ReviewPlan>
{
    public void Configure(EntityTypeBuilder<ReviewPlan> builder)
    {
        builder.ToTable("ReviewPlans");
        
        builder.HasKey(rp => rp.Id);
        
        builder.Property(rp => rp.UserId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(rp => rp.TargetEntityId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(rp => rp.Title)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(rp => rp.EntityType)
            .IsRequired();
        
        builder.Property(rp => rp.Status)
            .IsRequired();
        
        builder.HasIndex(rp => rp.UserId);
        builder.HasIndex(rp => rp.TargetEntityId);
        builder.HasIndex(rp => rp.CreatedAt);
        
        // Relationships
        builder.HasMany(rp => rp.ReviewSessions)
            .WithOne(rs => rs.ReviewPlan)
            .HasForeignKey(rs => rs.ReviewPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReviewSessionConfiguration : IEntityTypeConfiguration<ReviewSession>
{
    public void Configure(EntityTypeBuilder<ReviewSession> builder)
    {
        builder.ToTable("ReviewSessions");
        
        builder.HasKey(rs => rs.Id);
        
        builder.Property(rs => rs.ReviewPlanId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(rs => rs.Status)
            .IsRequired();
        
        builder.Property(rs => rs.Notes)
            .HasMaxLength(2000);
        
        builder.HasIndex(rs => rs.ReviewPlanId);
        builder.HasIndex(rs => rs.ScheduledDate);
        builder.HasIndex(rs => rs.Status);
    }
}
