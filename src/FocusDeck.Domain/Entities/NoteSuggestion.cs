namespace FocusDeck.Domain.Entities;

public class NoteSuggestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NoteId { get; set; } = string.Empty;
    public NoteSuggestionType Type { get; set; }
    public string ContentMarkdown { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // e.g., "Lecture transcript timestamp 12:34"
    public double Confidence { get; set; } // 0.0 to 1.0
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedBy { get; set; }
    
    // Navigation properties
    public Note Note { get; set; } = null!;
}

public enum NoteSuggestionType
{
    MissingPoint = 0,
    Definition = 1,
    Reference = 2,
    Clarification = 3
}
