using System.Text.Json.Nodes;

namespace FocusDeck.Contracts.DTOs;

public record FeatureSummaryDto(
    double? TypingVelocity,
    double? MouseEntropy,
    int? ContextSwitchCount,
    string? DevicePosture,
    string? AudioContext,
    string? PhysicalLocation,
    JsonNode? ApplicationStateDetails
);
