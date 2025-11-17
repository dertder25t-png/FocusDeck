using System;
using System.Collections.Generic;

namespace FocusDeck.Contracts.DTOs;

/// <summary>
/// Represents the request payload for generating a suggestion.
/// </summary>
/// <param name="CurrentContext">A summary of the user's current context, such as the active application or task.</param>
public record SuggestionRequestDto(string CurrentContext);

/// <summary>
/// Represents a suggestion action to be presented to the user.
/// </summary>
/// <param name="Action">The name of the action to be taken (e.g., "start_note", "open_url").</param>
/// <param name="Parameters">A dictionary of parameters required for the action (e.g., {"url": "https://example.com"}).</param>
/// <param name="Confidence">A score from 0.0 to 1.0 indicating the model's confidence in the suggestion.</param>
/// <param name="Evidence">A list of snapshot IDs or other data points that support the suggestion.</param>
public record SuggestionResponseDto(
    string Action,
    Dictionary<string, object> Parameters,
    double Confidence,
    Guid[] Evidence
);
