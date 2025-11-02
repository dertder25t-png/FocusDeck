using System.Timers;
using Timer = System.Timers.Timer;

namespace FocusDeck.Mobile.Services;

/// <summary>
/// Service for managing periodic heartbeat pings to the server
/// </summary>
public interface IHeartbeatService
{
    bool IsEnabled { get; }
    TimeSpan Interval { get; set; }
    void Start();
    void Stop();
    event EventHandler? HeartbeatTick;
}

/// <summary>
/// Heartbeat service implementation with configurable interval
/// Disabled by default - enable via Start() method
/// </summary>
public class HeartbeatService : IHeartbeatService, IDisposable
{
    private readonly Timer _timer;
    private readonly ILogger<HeartbeatService> _logger;

    public bool IsEnabled { get; private set; }

    public TimeSpan Interval
    {
        get => TimeSpan.FromMilliseconds(_timer.Interval);
        set
        {
            if (value.TotalMilliseconds < 1000)
            {
                throw new ArgumentException("Interval must be at least 1 second");
            }

            _timer.Interval = value.TotalMilliseconds;
            _logger.LogInformation("Heartbeat interval set to {Interval}", value);
        }
    }

    public event EventHandler? HeartbeatTick;

    public HeartbeatService(ILogger<HeartbeatService> logger)
    {
        _logger = logger;
        _timer = new Timer
        {
            Interval = 30000, // Default: 30 seconds
            AutoReset = true
        };
        _timer.Elapsed += OnTimerElapsed;
        IsEnabled = false; // Disabled by default
    }

    public void Start()
    {
        if (IsEnabled)
        {
            _logger.LogWarning("Heartbeat service is already running");
            return;
        }

        _timer.Start();
        IsEnabled = true;
        _logger.LogInformation("Heartbeat service started with interval {Interval}ms", _timer.Interval);
    }

    public void Stop()
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Heartbeat service is not running");
            return;
        }

        _timer.Stop();
        IsEnabled = false;
        _logger.LogInformation("Heartbeat service stopped");
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _logger.LogDebug("Heartbeat tick at {Time}", e.SignalTime);
        HeartbeatTick?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Stop();
        _timer.Dispose();
    }
}
