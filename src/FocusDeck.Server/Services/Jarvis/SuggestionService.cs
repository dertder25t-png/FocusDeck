using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// A service that generates suggestions for the user based on their current context.
/// This initial version implements a simple rule-based MVP.
/// </summary>
public class SuggestionService : ISuggestionService
{
    private readonly ILogger<SuggestionService> _logger;

    public SuggestionService(ILogger<SuggestionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a suggestion based on a simple, rule-based logic.
    /// </summary>
    /// <param name="request">The request containing the user's current context.</param>
    /// <returns>A suggestion if a rule is matched; otherwise, null.</returns>
    public Task<SuggestionResponseDto?> GenerateSuggestionAsync(SuggestionRequestDto request)
    {
        _logger.LogInformation("Generating suggestion for context: {Context}", request.CurrentContext);

        // STEP 1: Implement a simple rule-based MVP.
        // The goal is to check the input context against a set of predefined rules.
        // For example, if the context indicates the user is in a lecture, suggest starting a note.

        if (request.CurrentContext.Contains("lecture", StringComparison.OrdinalIgnoreCase))
        {
            var suggestion = new SuggestionResponseDto(
                Action: "start_note",
                Parameters: new Dictionary<string, object> { { "course", "Lecture" } },
                Confidence: 0.8,
                Evidence: Array.Empty<Guid>()
            );
            return Task.FromResult<SuggestionResponseDto?>(suggestion);
        }

        // STEP 2: Future Enhancement: Upgrade to a vector-driven approach.
        // 1. Generate an embedding for the `request.CurrentContext`.
        // 2. Perform a similarity search against the `ContextVectors` table in the database.
        // 3. Retrieve the top N most similar historical snapshots.
        // 4. Use the retrieved snapshots to formulate a more informed suggestion.
        //    For example, if similar contexts led to the user opening a specific file, suggest opening that file.

        _logger.LogInformation("No suggestion generated for the given context.");
        return Task.FromResult<SuggestionResponseDto?>(null);
    }
}
