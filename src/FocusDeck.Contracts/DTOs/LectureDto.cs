namespace FocusDeck.Contracts.DTOs;

public record CourseDto(
    string Id,
    string Name,
    string? Code,
    string? Description,
    string? Instructor,
    DateTime CreatedAt,
    string CreatedBy
);

public record CreateCourseDto(
    string Name,
    string? Code,
    string? Description,
    string? Instructor
);

public record LectureDto(
    string Id,
    string CourseId,
    string Title,
    string? Description,
    DateTime RecordedAt,
    DateTime CreatedAt,
    string CreatedBy,
    string? AudioAssetId,
    string Status,
    string? TranscriptionText,
    string? SummaryText,
    string? GeneratedNoteId,
    int? DurationSeconds
);

public record CreateLectureDto(
    string CourseId,
    string Title,
    string? Description,
    DateTime RecordedAt
);

public record UploadLectureAudioResponse(
    string LectureId,
    string AudioAssetId,
    string Status
);
