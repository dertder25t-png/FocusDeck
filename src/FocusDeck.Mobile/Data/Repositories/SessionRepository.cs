using Microsoft.EntityFrameworkCore;
using FocusDeck.Shared.Models;

namespace FocusDeck.Mobile.Data.Repositories;

/// <summary>
/// Repository implementation for managing StudySession data.
/// Provides CRUD operations and query methods for study sessions.
/// </summary>
public class SessionRepository : ISessionRepository
{
    private readonly StudySessionDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the SessionRepository.
    /// </summary>
    /// <param name="dbContext">The database context for study sessions.</param>
    public SessionRepository(StudySessionDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<StudySession> CreateSessionAsync(StudySession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        session.CreatedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        _dbContext.StudySessions.Add(session);
        await _dbContext.SaveChangesAsync();

        return session;
    }

    /// <inheritdoc/>
    public async Task<StudySession?> GetSessionByIdAsync(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));

        return await _dbContext.StudySessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    /// <inheritdoc/>
    public async Task<List<StudySession>> GetAllSessionsAsync()
    {
        return await _dbContext.StudySessions
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<StudySession>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be greater than or equal to start date.");

        // Normalize dates to ensure full day coverage
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddTicks(-1);

        return await _dbContext.StudySessions
            .Where(s => s.StartTime >= start && s.StartTime <= end)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<StudySession>> GetRecentSessionsAsync(int count = 10)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be greater than 0.", nameof(count));

        return await _dbContext.StudySessions
            .OrderByDescending(s => s.StartTime)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<StudySession> UpdateSessionAsync(StudySession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        var existing = await _dbContext.StudySessions
            .FirstOrDefaultAsync(s => s.SessionId == session.SessionId);

        if (existing == null)
            throw new InvalidOperationException($"Session with ID {session.SessionId} not found.");

        // Update properties while preserving CreatedAt
        existing.EndTime = session.EndTime;
        existing.DurationMinutes = session.DurationMinutes;
        existing.SessionNotes = session.SessionNotes;
        existing.Status = session.Status;
        existing.FocusRate = session.FocusRate;
        existing.BreaksCount = session.BreaksCount;
        existing.BreakDurationMinutes = session.BreakDurationMinutes;
        existing.Category = session.Category;
        existing.UpdatedAt = DateTime.UtcNow;

        _dbContext.StudySessions.Update(existing);
        await _dbContext.SaveChangesAsync();

        return existing;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));

        var session = await _dbContext.StudySessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            return false;

        _dbContext.StudySessions.Remove(session);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalStudyTimeAsync()
    {
        return await _dbContext.StudySessions
            .Where(s => s.Status == SessionStatus.Completed)
            .SumAsync(s => s.DurationMinutes);
    }

    /// <inheritdoc/>
    public async Task<SessionStatistics> GetSessionStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be greater than or equal to start date.");

        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddTicks(-1);

        var sessions = await _dbContext.StudySessions
            .Where(s => s.StartTime >= start && s.StartTime <= end)
            .ToListAsync();

        var completedSessions = sessions.Where(s => s.Status == SessionStatus.Completed).ToList();

        return new SessionStatistics
        {
            SessionCount = sessions.Count,
            TotalMinutes = completedSessions.Sum(s => s.DurationMinutes),
            AverageSessionMinutes = completedSessions.Any() 
                ? completedSessions.Average(s => s.DurationMinutes) 
                : 0,
            AverageFocusRate = completedSessions.Any(s => s.FocusRate.HasValue)
                ? completedSessions.Where(s => s.FocusRate.HasValue).Average(s => s.FocusRate)
                : null,
            TotalBreaks = completedSessions.Sum(s => s.BreaksCount),
            MostCommonCategory = sessions
                .Where(s => !string.IsNullOrEmpty(s.Category))
                .GroupBy(s => s.Category)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(),
            MostRecentSessionDate = sessions.Any() ? sessions.Max(s => s.StartTime) : null
        };
    }

    /// <inheritdoc/>
    public async Task<StudySession?> CompleteSessionAsync(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));

        var session = await _dbContext.StudySessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            return null;

        session.Status = SessionStatus.Completed;
        if (!session.EndTime.HasValue)
            session.EndTime = DateTime.UtcNow;

        // Recalculate duration if not already set
        if (session.DurationMinutes == 0 && session.EndTime.HasValue)
        {
            session.DurationMinutes = (int)(session.EndTime.Value - session.StartTime).TotalMinutes;
        }

        session.UpdatedAt = DateTime.UtcNow;

        _dbContext.StudySessions.Update(session);
        await _dbContext.SaveChangesAsync();

        return session;
    }

    /// <inheritdoc/>
    public async Task<bool> ClearAllSessionsAsync()
    {
        try
        {
            // Delete all sessions
            var sessions = await _dbContext.StudySessions.ToListAsync();
            _dbContext.StudySessions.RemoveRange(sessions);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing all sessions: {ex.Message}");
            return false;
        }
    }
}
