namespace FocusDeck.Server.Services.Transcription;

/// <summary>
/// Stub implementation of Whisper adapter for testing
/// </summary>
public class StubWhisperAdapter : IWhisperAdapter
{
    private readonly ILogger<StubWhisperAdapter> _logger;

    public StubWhisperAdapter(ILogger<StubWhisperAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(string audioFilePath, string language = "en", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StubWhisperAdapter.TranscribeAsync called for file: {FilePath}, language: {Language}", audioFilePath, language);
        
        // Simulate transcription delay
        await Task.Delay(200, cancellationToken);
        
        // Return a realistic stub transcription based on the file
        return $"This is a transcribed lecture about important concepts. The speaker discusses various topics including methodology, key principles, and practical applications. " +
               $"Throughout the presentation, several examples are provided to illustrate the main points. " +
               $"The lecture concludes with a summary of the key takeaways and suggestions for further study.";
    }
}
