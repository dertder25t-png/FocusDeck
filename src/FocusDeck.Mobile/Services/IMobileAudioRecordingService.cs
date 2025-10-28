namespace FocusDeck.Mobile.Services;

/// <summary>
/// Audio recording service for mobile platforms (iOS, Android).
/// Provides platform-specific audio capture functionality.
/// </summary>
public interface IMobileAudioRecordingService
{
    event EventHandler<AudioRecordingEventArgs>? RecordingStarted;
    event EventHandler<AudioRecordingEventArgs>? RecordingStopped;
    event EventHandler<RecordingErrorEventArgs>? RecordingError;

    bool IsRecording { get; }
    TimeSpan RecordingDuration { get; }
    
    Task<bool> StartRecordingAsync();
    Task<bool> StopRecordingAsync();
    Task<bool> PauseRecordingAsync();
    Task<bool> ResumeRecordingAsync();
    Task<string> GetLastRecordingPathAsync();
}

public class AudioRecordingEventArgs : EventArgs
{
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
}

public class RecordingErrorEventArgs : EventArgs
{
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
