using System.Collections.Concurrent;

namespace FocusDeck.Server.Services;

/// <summary>
/// Service to throttle telemetry messages to prevent flooding
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
}

public class TelemetryThrottleService : ITelemetryThrottleService
{
    private readonly ConcurrentDictionary<string, DateTime> _lastSent = new();
    private readonly TimeSpan _minimumInterval = TimeSpan.FromSeconds(1);
    private readonly ILogger<TelemetryThrottleService> _logger;

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
            _logger.LogDebug("Throttling telemetry for user {UserId}, last sent {Elapsed}ms ago", 
                userId, elapsed.TotalMilliseconds);
        }

        return canSend;
    }

    public void RecordTelemetrySent(string userId)
    {
        _lastSent[userId] = DateTime.UtcNow;
        
        // Clean up old entries when we exceed the limit
        if (_lastSent.Count > 1000)
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
                }

                _logger.LogWarning("Throttle service cleaned up {Count} entries to stay within limits", 
                    entriesToRemove.Count);
            }
        }
    }
}
