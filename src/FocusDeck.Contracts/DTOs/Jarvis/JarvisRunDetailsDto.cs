using System.Collections.Generic;

namespace FocusDeck.Contracts.DTOs.Jarvis
{
    public record JarvisRunDetailsDto(
        JarvisRunDto Run,
        IReadOnlyList<JarvisRunStepDto> Steps);
}
