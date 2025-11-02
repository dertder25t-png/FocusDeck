namespace FocusDeck.Contracts.DTOs;

public record CreateReviewPlanDto
{
    public required string TargetEntityId { get; init; }
    public required string EntityType { get; init; } // "Lecture" or "Note"
    public required string Title { get; init; }
    public required DateTime[] ScheduledDates { get; init; }
}

public record ReviewPlanDto
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string TargetEntityId { get; init; }
    public required string EntityType { get; init; }
    public required string Title { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required string Status { get; init; }
    public required List<ReviewSessionDto> ReviewSessions { get; init; }
}

public record ReviewSessionDto
{
    public required string Id { get; init; }
    public required string ReviewPlanId { get; init; }
    public required DateTime ScheduledDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    public required string Status { get; init; }
    public int? Score { get; init; }
    public string? Notes { get; init; }
}

public record UpdateReviewSessionDto
{
    public required string Status { get; init; } // "Completed" or "Skipped"
    public int? Score { get; init; }
    public string? Notes { get; init; }
}

public record ComputeSpacedPlanRequest
{
    public required string TargetEntityId { get; init; }
    public required string EntityType { get; init; } // "Lecture" or "Note"
    public required string Title { get; init; }
    public DateTime? StartDate { get; init; } // Optional, defaults to today
}
