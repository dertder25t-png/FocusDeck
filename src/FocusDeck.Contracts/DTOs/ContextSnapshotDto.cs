namespace FocusDeck.Contracts.DTOs;

public record ContextSnapshotDto(
    string EventType,
    DateTime Timestamp,
    string? ActiveApplication,
    string? ActiveWindowTitle,
    string? CalendarEventId,
    string? CourseContext,
    string? MachineState);
