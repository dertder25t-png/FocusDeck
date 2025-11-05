using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class NoteSuggestionConfiguration : IEntityTypeConfiguration<NoteSuggestion>
{
    public void Configure(EntityTypeBuilder<NoteSuggestion> builder)
    {
        builder.ToTable("NoteSuggestions");
        
        builder.HasKey(ns => ns.Id);
        
        builder.Property(ns => ns.Id)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(ns => ns.NoteId)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(ns => ns.Type)
            .IsRequired();
            
        builder.Property(ns => ns.ContentMarkdown)
            .IsRequired();
            
        builder.Property(ns => ns.Source)
            .HasMaxLength(500);
            
        builder.Property(ns => ns.Confidence)
            .IsRequired();
            
        builder.Property(ns => ns.CreatedAt)
            .IsRequired();
            
        builder.Property(ns => ns.AcceptedBy)
            .HasMaxLength(100);
            
        builder.HasOne(ns => ns.Note)
            .WithMany()
            .HasForeignKey(ns => ns.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(ns => ns.NoteId);
        builder.HasIndex(ns => ns.CreatedAt);
    }
}
