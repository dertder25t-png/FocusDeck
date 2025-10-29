using FocusDeck.Shared.Models;

namespace FocusDeck.Mobile.Data.Repositories;

/// <summary>
/// Repository interface for managing StudySession data operations.
/// Implements the Repository pattern for consistent data access.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Creates a new study session in the database.
    /// </summary>
    /// <param name="session">The study session to create.</param>
    /// <returns>The created session with database-assigned values.</returns>
    Task<StudySession> CreateSessionAsync(StudySession session);

    /// <summary>
    /// Retrieves a specific study session by its ID.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session.</param>
    /// <returns>The study session if found; otherwise null.</returns>
    Task<StudySession?> GetSessionByIdAsync(Guid sessionId);

    /// <summary>
    /// Retrieves all study sessions from the database.
    /// </summary>
    /// <returns>A list of all study sessions.</returns>
    Task<List<StudySession>> GetAllSessionsAsync();

    /// <summary>
    /// Retrieves study sessions for a specific date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <returns>Sessions that occurred within the date range.</returns>
    Task<List<StudySession>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Retrieves the most recent study sessions.
    /// </summary>
    /// <param name="count">The number of recent sessions to retrieve.</param>
    /// <returns>The specified number of most recent sessions.</returns>
    Task<List<StudySession>> GetRecentSessionsAsync(int count = 10);

    /// <summary>
    /// Updates an existing study session.
    /// </summary>
    /// <param name="session">The study session with updated values.</param>
    /// <returns>The updated study session.</returns>
    Task<StudySession> UpdateSessionAsync(StudySession session);

    /// <summary>
    /// Deletes a study session from the database.
    /// </summary>
    /// <param name="sessionId">The ID of the session to delete.</param>
    /// <returns>True if deletion was successful; otherwise false.</returns>
    Task<bool> DeleteSessionAsync(Guid sessionId);

    /// <summary>
    /// Gets the total study time across all sessions.
    /// </summary>
    /// <returns>Total study duration in minutes.</returns>
    Task<int> GetTotalStudyTimeAsync();

    /// <summary>
    /// Gets statistics for sessions within a date range.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>Session statistics including count, total time, and average focus rate.</returns>
    Task<SessionStatistics> GetSessionStatisticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Marks a session as completed.
    /// </summary>
    /// <param name="sessionId">The ID of the session to complete.</param>
    /// <returns>The updated completed session.</returns>
    Task<StudySession?> CompleteSessionAsync(Guid sessionId);

    /// <summary>
    /// Clears all study sessions from the database.
    /// Warning: This operation cannot be undone.
    /// </summary>
    /// <returns>True if operation was successful.</returns>
    Task<bool> ClearAllSessionsAsync();
}

/// <summary>
/// Statistics about study sessions for a given time period.
/// </summary>
public class SessionStatistics
{
    /// <summary>
    /// Total number of sessions in the period.
    /// </summary>
    public int SessionCount { get; set; }

    /// <summary>
    /// Total study time in minutes.
    /// </summary>
    public int TotalMinutes { get; set; }

    /// <summary>
    /// Average session duration in minutes.
    /// </summary>
    public double AverageSessionMinutes { get; set; }

    /// <summary>
    /// Average focus rate across sessions (0-100).
    /// </summary>
    public double? AverageFocusRate { get; set; }

    /// <summary>
    /// Total number of breaks across all sessions.
    /// </summary>
    public int TotalBreaks { get; set; }

    /// <summary>
    /// Most common category or null if no sessions have categories.
    /// </summary>
    public string? MostCommonCategory { get; set; }

    /// <summary>
    /// Date of the most recent session in the period.
    /// </summary>
    public DateTime? MostRecentSessionDate { get; set; }
}
