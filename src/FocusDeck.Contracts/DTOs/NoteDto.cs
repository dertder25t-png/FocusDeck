namespace FocusDeck.Contracts.DTOs;

public class CreateNoteDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Color { get; set; } = "#7C5CFF";
    public bool IsPinned { get; set; }
}

public class UpdateNoteDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Color { get; set; } = "#7C5CFF";
    public bool IsPinned { get; set; }
}

public class NoteDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Color { get; set; } = "#7C5CFF";
    public bool IsPinned { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModified { get; set; }
}
