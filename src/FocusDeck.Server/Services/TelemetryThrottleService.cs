using System.Collections.Concurrent;

namespace FocusDeck.Server.Services;

/// <summary>
/// Service to throttle telemetry messages to prevent flooding.
/// Implements backpressure to handle high-frequency updates from activity monitoring.
/// </summary>
public interface ITelemetryThrottleService
{
    /// <summary>
    /// Check if telemetry can be sent for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if telemetry can be sent, false if throttled</returns>
    bool CanSendTelemetry(string userId);
    
    /// <summary>
    /// Record that telemetry was sent for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    void RecordTelemetrySent(string userId);

    /// <summary>
    /// Get throttle statistics for monitoring
    /// </summary>
    /// <returns>Dictionary with stats: activeUsers, throttledInLastMinute</returns>
    IDictionary<string, int> GetThrottleStats();
}

public class TelemetryThrottleService : ITelemetryThrottleService
{
    private readonly ConcurrentDictionary<string, DateTime> _lastSent = new();
    private readonly ConcurrentDictionary<string, int> _throttleCount = new(); // Track throttled attempts
    private readonly TimeSpan _minimumInterval = TimeSpan.FromSeconds(1);
    private readonly ILogger<TelemetryThrottleService> _logger;
    private int _totalThrottledCount;

    public TelemetryThrottleService(ILogger<TelemetryThrottleService> logger)
    {
        _logger = logger;
    }

    public bool CanSendTelemetry(string userId)
    {
        if (!_lastSent.TryGetValue(userId, out var lastSent))
        {
            return true; // First time, allow
        }

        var elapsed = DateTime.UtcNow - lastSent;
        var canSend = elapsed >= _minimumInterval;

        if (!canSend)
        {
            // Track throttled attempts for backpressure monitoring
            _throttleCount.AddOrUpdate(userId, 1, (_, count) => count + 1);
            Interlocked.Increment(ref _totalThrottledCount);

            _logger.LogDebug("Throttling telemetry for user {UserId}, last sent {Elapsed}ms ago (throttled {Count} times)", 
                userId, elapsed.TotalMilliseconds, _throttleCount[userId]);

            // Log warning if a user is being throttled excessively
            if (_throttleCount[userId] % 100 == 0)
            {
                _logger.LogWarning(
                    "User {UserId} has been throttled {Count} times. " +
                    "Client may be sending telemetry too frequently. Consider client-side rate limiting.",
                    userId, _throttleCount[userId]);
            }
        }

        return canSend;
    }

    public void RecordTelemetrySent(string userId)
    {
        _lastSent[userId] = DateTime.UtcNow;
        
        // Reset throttle count after successful send
        _throttleCount.TryRemove(userId, out _);
        
        // Clean up old entries when we exceed the limit
        if (_lastSent.Count > 1000)
        {
            CleanupOldEntries();
        }
    }

    public IDictionary<string, int> GetThrottleStats()
    {
        return new Dictionary<string, int>
        {
            ["activeUsers"] = _lastSent.Count,
            ["throttledInLastMinute"] = _totalThrottledCount
        };
    }

    private void CleanupOldEntries()
    {
        // Remove entries older than 5 minutes
        var oldEntries = _lastSent
            .Where(kvp => DateTime.UtcNow - kvp.Value > TimeSpan.FromMinutes(5))
            .Select(kvp => kvp.Key)
            .Take(100) // Limit cleanup batch size
            .ToList();

        foreach (var key in oldEntries)
        {
            _lastSent.TryRemove(key, out _);
            _throttleCount.TryRemove(key, out _); // Also clean throttle counts
        }

        // If still over limit after removing old entries, remove oldest entries
        if (_lastSent.Count > 1000)
        {
            var entriesToRemove = _lastSent
                .OrderBy(kvp => kvp.Value)
                .Take(_lastSent.Count - 900) // Keep at 900 entries
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in entriesToRemove)
            {
                _lastSent.TryRemove(key, out _);
                _throttleCount.TryRemove(key, out _);
            }

            _logger.LogWarning(
                "Throttle service cleaned up {Count} entries to stay within limits. " +
                "Active users: {ActiveUsers}, Total throttled: {ThrottledCount}",
                entriesToRemove.Count, _lastSent.Count, _totalThrottledCount);
        }

        // Reset total throttled count periodically
        if (_totalThrottledCount > 10000)
        {
            _logger.LogInformation(
                "Resetting throttle counter after {Count} throttled attempts", _totalThrottledCount);
            Interlocked.Exchange(ref _totalThrottledCount, 0);
        }
    }
}

