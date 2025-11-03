using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Persistence.Configurations;

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
        
        builder.HasOne(l => l.Course)
            .WithMany(c => c.Lectures)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(l => l.AudioAsset)
            .WithMany()
            .HasForeignKey(l => l.AudioAssetId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
