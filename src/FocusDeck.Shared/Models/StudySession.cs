namespace FocusDeck.Shared.Models;

/// <summary>
/// Represents a study session with timing, notes, and status information.
/// This is a cross-platform model shared between desktop and mobile applications.
/// </summary>
public class StudySession
{
    /// <summary>
    /// Unique identifier for the study session (Primary Key).
    /// </summary>
    public Guid SessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When the study session started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the study session ended. Null if session is still in progress.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total duration of the study session in minutes.
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Optional notes about what was studied during the session.
    /// </summary>
    public string? SessionNotes { get; set; }

    /// <summary>
    /// Current status of the session (Active, Paused, Completed, Canceled).
    /// </summary>
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>
    /// Timestamp when the record was created in the database.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional focus rate metric (0-100) representing session effectiveness.
    /// </summary>
    public int? FocusRate { get; set; }

    /// <summary>
    /// Number of breaks taken during the session.
    /// </summary>
    public int BreaksCount { get; set; } = 0;

    /// <summary>
    /// Total break duration in minutes.
    /// </summary>
    public int BreakDurationMinutes { get; set; } = 0;

    /// <summary>
    /// Optional category or project name for the session (e.g., "Math", "Reading").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Calculates the actual productive time (duration minus breaks).
    /// </summary>
    public int GetProductiveMinutes() => DurationMinutes - BreakDurationMinutes;

    /// <summary>
    /// Returns true if the session is currently active.
    /// </summary>
    public bool IsActive => Status == SessionStatus.Active;

    /// <summary>
    /// Returns true if the session has completed.
    /// </summary>
    public bool IsCompleted => Status == SessionStatus.Completed;
}

/// <summary>
/// Enumeration for study session status.
/// </summary>
public enum SessionStatus
{
    /// <summary>Session is currently in progress.</summary>
    Active = 0,

    /// <summary>Session is temporarily paused.</summary>
    Paused = 1,

    /// <summary>Session has finished successfully.</summary>
    Completed = 2,

    /// <summary>Session was canceled before completion.</summary>
    Canceled = 3
}
