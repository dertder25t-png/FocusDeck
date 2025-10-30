using System;
using System.Collections.Generic;

namespace FocusDock.Data.Models;

public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string Color { get; set; } = "#7C5CFF"; // Default purple
    public bool IsPinned { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? LastModified { get; set; }
    public List<NoteBookmark> Bookmarks { get; set; } = new();
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
