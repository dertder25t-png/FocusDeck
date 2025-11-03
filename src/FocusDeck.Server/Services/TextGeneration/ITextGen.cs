namespace FocusDeck.Server.Services.TextGeneration;

/// <summary>
/// Interface for text generation services
/// </summary>
public interface ITextGen
{
    /// <summary>
    /// Generate text based on a prompt
    /// </summary>
    /// <param name="prompt">The prompt to generate text from</param>
    /// <param name="maxTokens">Maximum number of tokens to generate</param>
    /// <param name="temperature">Temperature for generation (0.0 to 2.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text</returns>
    Task<string> GenerateAsync(string prompt, int maxTokens = 500, double temperature = 0.7, CancellationToken cancellationToken = default);
}
