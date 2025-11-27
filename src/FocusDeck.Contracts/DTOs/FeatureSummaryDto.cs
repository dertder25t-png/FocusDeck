namespace FocusDeck.Contracts.DTOs;

public record FeatureSummaryDto(
    double? TypingVelocity,
    double? MouseEntropy,
    int? ContextSwitchCount
);
