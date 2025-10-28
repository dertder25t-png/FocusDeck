namespace FocusDeck.Services.Abstractions;

/// <summary>Audio recording service abstraction</summary>
public interface IAudioRecordingService
{
    /// <summary>Starts recording audio and returns the session ID</summary>
    Task<string> StartRecording();

    /// <summary>Stops recording and returns the audio file information</summary>
    Task<AudioRecording> StopRecording();

    /// <summary>Transcribes audio file to text</summary>
    Task<string> TranscribeAudio(string filePath);

    /// <summary>Gets all audio notes for a specific date</summary>
    Task<List<AudioRecording>> GetNotesForDate(DateTime date);

    /// <summary>Fired when recording progress changes (0-100)</summary>
    event EventHandler<double>? RecordingProgressChanged;

    /// <summary>Fired when recording encounters an error</summary>
    event EventHandler<string>? RecordingError;
}

/// <summary>Audio playback service abstraction</summary>
public interface IAudioPlaybackService
{
    /// <summary>Plays audio from the specified file path</summary>
    Task PlayAudio(string filePath);

    /// <summary>Pauses current audio playback</summary>
    Task PauseAudio();

    /// <summary>Resumes paused audio</summary>
    Task ResumeAudio();

    /// <summary>Stops audio playback completely</summary>
    Task StopAudio();

    /// <summary>Sets volume level (0-100)</summary>
    Task SetVolume(int percentage);

    /// <summary>Plays ambient sounds for focus (rain, forest, coffee shop, etc.)</summary>
    Task PlayAmbientSound(AmbientSoundType type);

    /// <summary>Gets current playback position in milliseconds</summary>
    Task<long> GetCurrentPosition();

    /// <summary>Gets total duration in milliseconds</summary>
    Task<long> GetDuration();

    /// <summary>Fired when playback completes</summary>
    event EventHandler? PlaybackCompleted;
}

/// <summary>Recorded audio information</summary>
public class AudioRecording
{
    /// <summary>Unique identifier for this recording</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>File path where audio is stored</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Duration of the recording</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>When the recording was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Transcribed text (null if not transcribed yet)</summary>
    public string? Transcription { get; set; }

    /// <summary>Associated study session ID (if any)</summary>
    public string? SessionId { get; set; }

    /// <summary>Associated study subject (if any)</summary>
    public string? Subject { get; set; }
}

/// <summary>Types of ambient sounds for focus</summary>
public enum AmbientSoundType
{
    Rain,
    Forest,
    Ocean,
    CoffeeShop,
    LofiBeats,
    ClassroomAmbience,
    LibrarySilence,
    Thunderstorm,
    MorningBirds,
    FireCrackle
}
