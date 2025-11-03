using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(c => c.Code)
            .HasMaxLength(50);
            
        builder.Property(c => c.Description)
            .HasMaxLength(2000);
            
        builder.Property(c => c.Instructor)
            .HasMaxLength(200);
            
        builder.Property(c => c.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.Code);
        
        builder.HasMany(c => c.Lectures)
            .WithOne(l => l.Course)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
