namespace FocusDeck.Server.Services.TextGeneration;

/// <summary>
/// Stub implementation of ITextGen for testing
/// </summary>
public class StubTextGen : ITextGen
{
    private readonly ILogger<StubTextGen> _logger;

    public StubTextGen(ILogger<StubTextGen> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt, int maxTokens = 500, double temperature = 0.7, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StubTextGen.GenerateAsync called with prompt length: {Length}, maxTokens: {MaxTokens}", prompt.Length, maxTokens);
        
        // Simulate API delay
        await Task.Delay(150, cancellationToken);
        
        // Return a stub summary
        return $"[Generated Summary] This is a concise summary of the content. Key points include: main topics discussed, important concepts, and takeaways.";
    }
}
