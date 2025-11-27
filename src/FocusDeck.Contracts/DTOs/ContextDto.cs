namespace FocusDeck.Contracts.DTOs;

/// <summary>
/// Represents the different layers of context for a user.
/// </summary>
/// <param name="ImmediateContext">What the user is doing right now.</param>
/// <param name="SessionContext">What the user has been doing in the current session.</param>
/// <param name="ProjectContext">What the user has been doing in the current project.</param>
/// <param name="SeasonalContext">What the user has been doing over a longer period of time.</param>
public record LayeredContextDto(
    string ImmediateContext,
    string SessionContext,
    string ProjectContext,
    string SeasonalContext
);
