using System;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities
{
    public enum NoteType
    {
        QuickNote,
        AcademicPaper
    }

    public class Note : IMustHaveTenant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public NoteType Type { get; set; } = NoteType.QuickNote;
        public List<string> Tags { get; set; } = new();
        public string Color { get; set; } = "#7C5CFF"; // Default purple
        public bool IsPinned { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }
        public List<NoteBookmark> Bookmarks { get; set; } = new();

        // Academic Paper specific
        public List<AcademicSource> Sources { get; set; } = new();
        public string? CitationStyle { get; set; } = "APA"; // APA, MLA, Chicago

        // Context / Calendar
        public Guid? CourseId { get; set; }
        public Guid? EventId { get; set; }

        public Guid TenantId { get; set; }
    }

    public class NoteBookmark
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public int Position { get; set; } // Character position in the note
        public int Length { get; set; } // Highlight length when jumping back
        public string Color { get; set; } = "#FFD700"; // Gold/yellow highlight by default
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
