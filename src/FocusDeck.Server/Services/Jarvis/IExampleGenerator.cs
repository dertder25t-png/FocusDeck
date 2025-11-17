using System.Collections.Generic;
using System.Threading.Tasks;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// Defines the contract for a service that generates few-shot examples from historical data.
/// </summary>
public interface IExampleGenerator
{
    /// <summary>
    /// Generates a list of few-shot examples based on the current context.
    /// </summary>
    /// <param name="context">The current user context.</param>
    /// <returns>A list of few-shot examples.</returns>
    Task<List<string>> GenerateExamplesAsync(string context);
}
