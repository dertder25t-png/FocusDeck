namespace FocusDeck.Contracts.DTOs;

public class CreateStudySessionDto
{
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? SessionNotes { get; set; }
    public string? Category { get; set; }
}

public class UpdateStudySessionDto
{
    public Guid SessionId { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? SessionNotes { get; set; }
    public int Status { get; set; }
    public int? FocusRate { get; set; }
    public int BreaksCount { get; set; }
    public int BreakDurationMinutes { get; set; }
    public string? Category { get; set; }
}

public class StudySessionDto
{
    public Guid SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? SessionNotes { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? FocusRate { get; set; }
    public int BreaksCount { get; set; }
    public int BreakDurationMinutes { get; set; }
    public string? Category { get; set; }
}
