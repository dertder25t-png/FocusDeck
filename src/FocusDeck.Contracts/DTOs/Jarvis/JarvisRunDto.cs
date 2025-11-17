using System;

namespace FocusDeck.Contracts.DTOs.Jarvis
{
    public record JarvisRunDto(
        Guid Id,
        string Status,
        string EntryPoint,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);
}
