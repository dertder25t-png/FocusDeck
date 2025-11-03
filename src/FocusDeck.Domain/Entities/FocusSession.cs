namespace FocusDeck.Domain.Entities;

/// <summary>
/// Represents a focus session with smart signals and policies
/// </summary>
public class FocusSession
{
    /// <summary>
    /// Unique identifier for the focus session
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID who owns this session
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// When the focus session started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the focus session ended (null if still active)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Current status of the session
    /// </summary>
    public FocusSessionStatus Status { get; set; } = FocusSessionStatus.Active;

    /// <summary>
    /// Session policy configuration
    /// </summary>
    public FocusPolicy Policy { get; set; } = new();

    /// <summary>
    /// Signals received during this session (stored as JSON)
    /// </summary>
    public List<FocusSignal> Signals { get; set; } = new();

    /// <summary>
    /// Number of distractions detected in this session
    /// </summary>
    public int DistractionsCount { get; set; } = 0;

    /// <summary>
    /// Last time a recovery suggestion was sent
    /// </summary>
    public DateTime? LastRecoverySuggestionAt { get; set; }

    /// <summary>
    /// Timestamp when the record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns true if the session is currently active
    /// </summary>
    public bool IsActive => Status == FocusSessionStatus.Active;

    /// <summary>
    /// Returns true if the session has completed
    /// </summary>
    public bool IsCompleted => Status == FocusSessionStatus.Completed;
}

/// <summary>
/// Focus session status enumeration
/// </summary>
public enum FocusSessionStatus
{
    /// <summary>Session is currently active</summary>
    Active = 0,

    /// <summary>Session is paused</summary>
    Paused = 1,

    /// <summary>Session has completed</summary>
    Completed = 2,

    /// <summary>Session was canceled</summary>
    Canceled = 3
}

/// <summary>
/// Focus session policy configuration
/// </summary>
public class FocusPolicy
{
    /// <summary>
    /// Strict mode: detect phone motion/screen activity as distractions
    /// </summary>
    public bool Strict { get; set; } = false;

    /// <summary>
    /// Automatically suggest breaks when distractions exceed threshold
    /// </summary>
    public bool AutoBreak { get; set; } = true;

    /// <summary>
    /// Automatically dim the desktop background when paused
    /// </summary>
    public bool AutoDim { get; set; } = false;

    /// <summary>
    /// Send notifications to phone for focus reminders
    /// </summary>
    public bool NotifyPhone { get; set; } = false;
}

/// <summary>
/// Signal received from a device
/// </summary>
public class FocusSignal
{
    /// <summary>
    /// Device that sent the signal
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Type of signal
    /// </summary>
    public SignalKind Kind { get; set; }

    /// <summary>
    /// Signal value (interpretation depends on Kind)
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// When the signal was recorded
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Types of signals that can be recorded
/// </summary>
public enum SignalKind
{
    /// <summary>Phone accelerometer detected motion</summary>
    PhoneMotion = 0,

    /// <summary>Phone screen turned on/active</summary>
    PhoneScreen = 1,

    /// <summary>Desktop keyboard activity</summary>
    Keyboard = 2,

    /// <summary>Desktop mouse activity</summary>
    Mouse = 3,

    /// <summary>Ambient noise level detected</summary>
    AmbientNoise = 4
}
