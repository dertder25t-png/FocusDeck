using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Course entity.
/// 
/// CASCADE DELETE BEHAVIOR:
/// - Course → Lectures: CASCADE
///   When a Course is deleted, all associated Lectures are automatically deleted.
///   This ensures referential integrity and prevents orphaned lecture records.
///   
/// IMPACT: Deleting a course will cascade to:
/// 1. All Lectures in the course (via Lectures collection)
/// 2. All Assets referenced by those Lectures (via Lecture → Asset relationship, see LectureConfiguration)
/// 
/// CAUTION: Course deletion is a destructive operation that will remove all child data.
/// Consider soft-delete or archive patterns for production use.
/// </summary>
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
        
        // CASCADE DELETE: Course → Lectures
        // When a Course is deleted, all Lectures in that course are automatically deleted
        builder.HasMany(c => c.Lectures)
            .WithOne(l => l.Course)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
