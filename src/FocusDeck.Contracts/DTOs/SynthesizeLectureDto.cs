using System;

namespace FocusDeck.Contracts.DTOs;

public record SynthesizeLectureDto(
    string Transcript,
    DateTime Timestamp,
    Guid? CourseId,
    Guid? EventId
);
