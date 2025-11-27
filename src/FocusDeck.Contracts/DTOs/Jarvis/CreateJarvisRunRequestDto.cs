namespace FocusDeck.Contracts.DTOs.Jarvis
{
    public record CreateJarvisRunRequestDto(
        string EntryPoint,
        string? InputPayloadJson);
}
