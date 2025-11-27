namespace FocusDeck.Contracts.DTOs;

public sealed record ActivitySignalDto(
    string SignalType,
    string SignalValue,
    string SourceApp,
    DateTime? CapturedAtUtc = null,
    string? MetadataJson = null);
