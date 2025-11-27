using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;

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
}
