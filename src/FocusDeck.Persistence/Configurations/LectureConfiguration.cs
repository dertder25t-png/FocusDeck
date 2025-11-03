using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Lecture entity.
/// 
/// CASCADE DELETE BEHAVIOR:
/// - Course → Lecture: CASCADE (configured in CourseConfiguration)
///   When a Course is deleted, all associated Lectures are automatically deleted.
///   
/// - Lecture → AudioAsset: SET NULL
///   When an Asset is deleted, the Lecture's AudioAssetId is set to null.
///   The Lecture record is preserved but loses its audio file reference.
///   This prevents accidental data loss if an asset is removed.
///   
/// - Lecture → GeneratedNote: NO FOREIGN KEY (string reference only)
///   GeneratedNoteId is a string reference to a Note entity. No cascade behavior.
///   Application logic must handle orphaned references if notes are deleted.
///   
/// IMPACT: Deleting a lecture will:
/// - Remove the lecture record from the database
/// - NOT delete the referenced Asset (use Asset deletion endpoint separately)
/// - NOT delete the referenced Note (application must handle cleanup)
/// 
/// CAUTION: Assets and Notes may become orphaned if not cleaned up manually.
/// Consider implementing background job to clean orphaned assets/notes.
/// </summary>
public class LectureConfiguration : IEntityTypeConfiguration<Lecture>
{
    public void Configure(EntityTypeBuilder<Lecture> builder)
    {
        builder.HasKey(l => l.Id);
        
        builder.Property(l => l.Title)
            .IsRequired()
            .HasMaxLength(300);
            
        builder.Property(l => l.Description)
            .HasMaxLength(2000);
            
        builder.Property(l => l.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(l => l.Status)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(l => l.AudioAssetId)
            .HasMaxLength(100);
            
        builder.Property(l => l.GeneratedNoteId)
            .HasMaxLength(100);
            
        builder.HasIndex(l => l.CourseId);
        builder.HasIndex(l => l.RecordedAt);
        builder.HasIndex(l => l.Status);
        
        // CASCADE: Course → Lecture
        // Configured in CourseConfiguration, but shown here for clarity
        builder.HasOne(l => l.Course)
            .WithMany(c => c.Lectures)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // SET NULL: Asset deletion sets Lecture.AudioAssetId to null
        // Lecture is preserved even if its audio asset is deleted
        builder.HasOne(l => l.AudioAsset)
            .WithMany()
            .HasForeignKey(l => l.AudioAssetId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
