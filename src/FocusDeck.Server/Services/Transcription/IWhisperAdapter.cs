namespace FocusDeck.Server.Services.Transcription;

/// <summary>
/// Interface for Whisper.cpp transcription adapter
/// </summary>
public interface IWhisperAdapter
{
    /// <summary>
    /// Transcribe an audio file to text
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file</param>
    /// <param name="language">Language code (e.g., "en", "es")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcribed text</returns>
    Task<string> TranscribeAsync(string audioFilePath, string language = "en", CancellationToken cancellationToken = default);
}
