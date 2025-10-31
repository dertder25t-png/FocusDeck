namespace FocusDeck.Services.Implementations.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Services.Abstractions;

/// <summary>
/// Core study session service (cross-platform implementation).
/// Manages creation, persistence, and retrieval of study sessions.
/// </summary>
public class StudySessionService : IStudySessionService
{
    private readonly IPlatformService _platformService;
    private string? _storagePath;
    private string? _sessionsFile;
    private List<StudySessionDto> _sessions = new();
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized = false;

    public StudySessionService(IPlatformService platformService)
    {
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
        
        // Initialize asynchronously without blocking constructor
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the service asynchronously (called from constructor)
    /// </summary>
    private async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _storagePath = await _platformService.GetAppDataPath();
            _sessionsFile = Path.Combine(_storagePath, "sessions.json");
            
            // Ensure directory exists
            Directory.CreateDirectory(_storagePath);
            
            // Load existing sessions
            await LoadSessionsAsync();
            
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Ensures the service is initialized before operations
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        await InitializeAsync();
    }

    /// <summary>Creates a new study session</summary>
    public async Task<StudySessionDto> CreateSessionAsync(string subject, DateTime startTime)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var session = new StudySessionDto
            {
                Id = Guid.NewGuid().ToString(),
                Subject = subject,
                StartTime = startTime,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                NeedsSynced = true
            };

            _sessions.Add(session);
            System.Diagnostics.Debug.WriteLine($"Session created: {session.Id} ({subject})");

            return session;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating session: {ex.Message}");
            throw;
        }
    }

    /// <summary>Ends a study session and saves it</summary>
    public async Task<StudySessionDto> EndSessionAsync(string sessionId, int effectiveness, string? notes = null)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null)
            {
                throw new KeyNotFoundException($"Session not found: {sessionId}");
            }

            session.EndTime = DateTime.UtcNow;
            session.Effectiveness = Math.Clamp(effectiveness, 1, 5);
            session.Notes = notes;
            session.DurationMinutes = (int)(session.EndTime - session.StartTime).TotalMinutes;
            session.LastModified = DateTime.UtcNow;
            session.NeedsSynced = true;

            System.Diagnostics.Debug.WriteLine($"Session ended: {sessionId}, Effectiveness: {effectiveness}");
            
            // Save to file
            _ = SaveAllSessionsAsync();

            return session;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error ending session: {ex.Message}");
            throw;
        }
    }

    /// <summary>Gets a specific study session by ID</summary>
    public async Task<StudySessionDto?> GetSessionAsync(string sessionId)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            return session;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting session: {ex.Message}");
            return null;
        }
    }

    /// <summary>Gets all sessions for a date range</summary>
    public async Task<List<StudySessionDto>> GetSessionsAsync(DateTime startDate, DateTime endDate)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var sessions = _sessions
                .Where(s => s.StartTime.Date >= startDate.Date && s.StartTime.Date <= endDate.Date)
                .OrderByDescending(s => s.StartTime)
                .ToList();

            return sessions;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting sessions: {ex.Message}");
            return new List<StudySessionDto>();
        }
    }

    /// <summary>Gets sessions for a specific subject</summary>
    public async Task<List<StudySessionDto>> GetSessionsBySubjectAsync(string subject, int days = 30)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var sessions = _sessions
                .Where(s => s.Subject == subject && s.StartTime >= cutoffDate)
                .OrderByDescending(s => s.StartTime)
                .ToList();

            return sessions;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting sessions by subject: {ex.Message}");
            return new List<StudySessionDto>();
        }
    }

    /// <summary>Updates session notes</summary>
    public async Task UpdateSessionNotesAsync(string sessionId, string notes)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null)
            {
                throw new KeyNotFoundException($"Session not found: {sessionId}");
            }

            session.Notes = notes;
            session.LastModified = DateTime.UtcNow;
            session.NeedsSynced = true;

            await SaveAllSessionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating session notes: {ex.Message}");
        }
    }

    /// <summary>Deletes a session</summary>
    public async Task DeleteSessionAsync(string sessionId)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                _sessions.Remove(session);
                await SaveAllSessionsAsync();
                System.Diagnostics.Debug.WriteLine($"Session deleted: {sessionId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting session: {ex.Message}");
        }
    }

    /// <summary>Saves all sessions to file</summary>
    public async Task SaveAllSessionsAsync()
    {
        await EnsureInitializedAsync();
        
        try
        {
            if (_sessionsFile == null) return; // Not initialized yet
            
            var json = JsonSerializer.Serialize(_sessions, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_sessionsFile, json);
            System.Diagnostics.Debug.WriteLine($"Sessions saved: {_sessions.Count} sessions");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving sessions: {ex.Message}");
        }
    }

    /// <summary>Marks a session as synced to server</summary>
    public async Task MarkSyncedAsync(string sessionId, DateTime syncedAt)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                session.SyncedAt = syncedAt;
                session.NeedsSynced = false;
                await SaveAllSessionsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking session as synced: {ex.Message}");
        }
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            if (_sessionsFile != null && File.Exists(_sessionsFile))
            {
                var json = await File.ReadAllTextAsync(_sessionsFile);
                _sessions = JsonSerializer.Deserialize<List<StudySessionDto>>(json) ?? new();
                System.Diagnostics.Debug.WriteLine($"Loaded {_sessions.Count} sessions from file");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading sessions: {ex.Message}");
            _sessions = new();
        }
    }
}

/// <summary>
/// Analytics service implementation (cross-platform).
/// Calculates statistics and trends from study sessions.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IStudySessionService _sessionService;

    public AnalyticsService(IStudySessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>Gets statistics for a date range</summary>
    public async Task<StudyStatsDto> GetStatsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var sessions = await _sessionService.GetSessionsAsync(startDate, endDate);

            if (sessions.Count == 0)
            {
                return new StudyStatsDto
                {
                    TotalSessions = 0,
                    TotalMinutes = 0,
                    AverageEffectiveness = 0,
                    UniqueDaysStudied = 0
                };
            }

            var totalMinutes = sessions.Sum(s => s.DurationMinutes);
            var avgEffectiveness = sessions.Average(s => s.Effectiveness);
            var uniqueDays = sessions.Select(s => s.StartTime.Date).Distinct().Count();
            var mostStudiedSubject = sessions
                .GroupBy(s => s.Subject)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            return new StudyStatsDto
            {
                TotalSessions = sessions.Count,
                TotalMinutes = totalMinutes,
                AverageEffectiveness = avgEffectiveness,
                UniqueDaysStudied = uniqueDays,
                MostStudiedSubject = mostStudiedSubject,
                LongestStreak = CalculateLongestStreak(sessions),
                CurrentStreak = CalculateCurrentStreak(sessions)
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting stats: {ex.Message}");
            return new StudyStatsDto();
        }
    }

    /// <summary>Gets effectiveness trend over time</summary>
    public async Task<List<EffectivenessDataPoint>> GetEffectivenessTrendAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            var sessions = await _sessionService.GetSessionsAsync(startDate, endDate);

            var trends = sessions
                .GroupBy(s => s.StartTime.Date)
                .OrderBy(g => g.Key)
                .Select(g => new EffectivenessDataPoint
                {
                    Date = g.Key,
                    AverageEffectiveness = g.Average(s => s.Effectiveness),
                    SessionCount = g.Count()
                })
                .ToList();

            return trends;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting trend: {ex.Message}");
            return new List<EffectivenessDataPoint>();
        }
    }

    /// <summary>Gets study time by subject</summary>
    public async Task<Dictionary<string, int>> GetStudyTimeBySubjectAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            var sessions = await _sessionService.GetSessionsAsync(startDate, endDate);

            return sessions
                .GroupBy(s => s.Subject)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting study time by subject: {ex.Message}");
            return new Dictionary<string, int>();
        }
    }

    /// <summary>Gets most productive hours of the day</summary>
    public async Task<Dictionary<int, int>> GetProductiveHoursAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            var sessions = await _sessionService.GetSessionsAsync(startDate, endDate);

            return sessions
                .GroupBy(s => s.StartTime.Hour)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting productive hours: {ex.Message}");
            return new Dictionary<int, int>();
        }
    }

    /// <summary>Calculates average session duration</summary>
    public async Task<double> GetAverageSessionDurationAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            var sessions = await _sessionService.GetSessionsAsync(startDate, endDate);

            if (sessions.Count == 0)
                return 0;

            return sessions.Average(s => s.DurationMinutes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting average duration: {ex.Message}");
            return 0;
        }
    }

    /// <summary>Gets break adherence statistics</summary>
    public async Task<BreakStatsDto> GetBreakStatsAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            var sessions = await _sessionService.GetSessionsAsync(startDate, endDate);

            var totalBreaks = sessions.Sum(s => s.BreaksTaken);
            var breakSuggestions = sessions.Count(s => s.DurationMinutes > 25); // Pomodoro rule

            return new BreakStatsDto
            {
                TotalBreaksTaken = totalBreaks,
                BreaksSuggested = breakSuggestions,
                BreakAdherencePercentage = breakSuggestions > 0 ? (totalBreaks / (double)breakSuggestions) * 100 : 0,
                AverageBreakDurationMinutes = 5
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting break stats: {ex.Message}");
            return new BreakStatsDto();
        }
    }

    private int CalculateLongestStreak(List<StudySessionDto> sessions)
    {
        if (sessions.Count == 0) return 0;

        var dates = sessions
            .Select(s => s.StartTime.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        int longestStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            if ((dates[i] - dates[i - 1]).TotalDays == 1)
            {
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return longestStreak;
    }

    private int CalculateCurrentStreak(List<StudySessionDto> sessions)
    {
        if (sessions.Count == 0) return 0;

        var today = DateTime.UtcNow.Date;
        var dates = sessions
            .Select(s => s.StartTime.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int currentStreak = 0;

        for (int i = 0; i < dates.Count; i++)
        {
            if (i == 0 && dates[i] != today && dates[i] != today.AddDays(-1))
                break;

            if (i == 0 || (dates[i - 1] - dates[i]).TotalDays == 1)
            {
                currentStreak++;
            }
            else
            {
                break;
            }
        }

        return currentStreak;
    }
}
