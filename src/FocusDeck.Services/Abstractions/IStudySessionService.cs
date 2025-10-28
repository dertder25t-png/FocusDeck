namespace FocusDeck.Services.Abstractions;

/// <summary>Service for managing study sessions (cross-platform)</summary>
public interface IStudySessionService
{
    /// <summary>Creates a new study session</summary>
    Task<StudySessionDto> CreateSessionAsync(string subject, DateTime startTime);

    /// <summary>Ends a study session and saves it</summary>
    Task<StudySessionDto> EndSessionAsync(string sessionId, int effectiveness, string? notes = null);

    /// <summary>Gets a specific study session by ID</summary>
    Task<StudySessionDto?> GetSessionAsync(string sessionId);

    /// <summary>Gets all sessions for a date range</summary>
    Task<List<StudySessionDto>> GetSessionsAsync(DateTime startDate, DateTime endDate);

    /// <summary>Gets sessions for a specific subject</summary>
    Task<List<StudySessionDto>> GetSessionsBySubjectAsync(string subject, int days = 30);

    /// <summary>Updates session notes</summary>
    Task UpdateSessionNotesAsync(string sessionId, string notes);

    /// <summary>Deletes a session</summary>
    Task DeleteSessionAsync(string sessionId);

    /// <summary>Saves all sessions locally (for sync later)</summary>
    Task SaveAllSessionsAsync();

    /// <summary>Marks session as synced to server</summary>
    Task MarkSyncedAsync(string sessionId, DateTime syncedAt);
}

/// <summary>DTO for study session (platform-agnostic)</summary>
public class StudySessionDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Subject { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int Effectiveness { get; set; } // 1-5
    public string? MusicId { get; set; }
    public string? AudioNoteId { get; set; }
    public string? Notes { get; set; }
    public int BreaksTaken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedAt { get; set; }
    public bool NeedsSynced { get; set; } = true;
}

/// <summary>Service for study analytics</summary>
public interface IAnalyticsService
{
    /// <summary>Gets statistics for a date range</summary>
    Task<StudyStatsDto> GetStatsAsync(DateTime startDate, DateTime endDate);

    /// <summary>Gets effectiveness trend over time</summary>
    Task<List<EffectivenessDataPoint>> GetEffectivenessTrendAsync(int days = 30);

    /// <summary>Gets study time by subject</summary>
    Task<Dictionary<string, int>> GetStudyTimeBySubjectAsync(int days = 30);

    /// <summary>Gets most productive hours of the day</summary>
    Task<Dictionary<int, int>> GetProductiveHoursAsync(int days = 30);

    /// <summary>Calculates average session duration</summary>
    Task<double> GetAverageSessionDurationAsync(int days = 30);

    /// <summary>Gets break adherence statistics</summary>
    Task<BreakStatsDto> GetBreakStatsAsync(int days = 30);
}

/// <summary>Study statistics DTO</summary>
public class StudyStatsDto
{
    public int TotalSessions { get; set; }
    public int TotalMinutes { get; set; }
    public double AverageEffectiveness { get; set; }
    public int UniqueDaysStudied { get; set; }
    public string MostStudiedSubject { get; set; } = string.Empty;
    public int LongestStreak { get; set; }
    public int CurrentStreak { get; set; }
}

/// <summary>Single data point for effectiveness trend</summary>
public class EffectivenessDataPoint
{
    public DateTime Date { get; set; }
    public double AverageEffectiveness { get; set; }
    public int SessionCount { get; set; }
}

/// <summary>Break statistics</summary>
public class BreakStatsDto
{
    public int TotalBreaksTaken { get; set; }
    public int BreaksSuggested { get; set; }
    public double BreakAdherencePercentage { get; set; }
    public int AverageBreakDurationMinutes { get; set; }
}
