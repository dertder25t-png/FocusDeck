using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// Defines the contract for a service that generates intelligent suggestions based on user context.
/// </summary>
public interface ISuggestionService
{
    /// <summary>
    /// Generates a suggestion based on the provided request context.
    /// </summary>
    /// <param name="request">The request containing the user's current context.</param>
    /// <returns>A suggestion response, or null if no suggestion is available.</returns>
    Task<SuggestionResponseDto?> GenerateSuggestionAsync(SuggestionRequestDto request);

    /// <summary>
    /// Analyzes a note and generates AI-powered suggestions for improvements or additions.
    /// </summary>
    /// <param name="note">The note to analyze.</param>
    /// <returns>A list of generated suggestions.</returns>
    Task<List<NoteSuggestion>> AnalyzeNoteAsync(Note note);
}
