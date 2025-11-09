namespace FocusDeck.Domain.Entities;

public class Lecture : IMustHaveTenant
{
    public string Id { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    // Audio file reference
    public string? AudioAssetId { get; set; }
    
    // Processing status
    public LectureStatus Status { get; set; } = LectureStatus.Created;
    
    // Transcription and summary
    public string? TranscriptionText { get; set; }
    public string? SummaryText { get; set; }
    public string? GeneratedNoteId { get; set; }
    
    // Duration in seconds
    public int? DurationSeconds { get; set; }
    
    // Navigation properties
    public Course Course { get; set; } = null!;
    public Asset? AudioAsset { get; set; }
    public Guid TenantId { get; set; }
}

public enum LectureStatus
{
    Created = 0,
    AudioUploaded = 1,
    Transcribing = 2,
    Transcribed = 3,
    Summarizing = 4,
    Summarized = 5,
    GeneratingNotes = 6,
    Completed = 7,
    Failed = 8
}
