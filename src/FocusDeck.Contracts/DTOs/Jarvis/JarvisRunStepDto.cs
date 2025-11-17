using System;

namespace FocusDeck.Contracts.DTOs.Jarvis
{
    public record JarvisRunStepDto(
        int Order,
        string StepType,
        DateTimeOffset CreatedAt,
        string? RequestJson,
        string? ResponseJson,
        string? ErrorJson);
}
