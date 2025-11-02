namespace FocusDeck.Domain.Entities;

public class ReviewPlan
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty; // Lecture or Note ID
    public ReviewPlanEntityType EntityType { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ReviewPlanStatus Status { get; set; } = ReviewPlanStatus.Active;
    
    // Review sessions (spaced repetition schedule)
    public List<ReviewSession> ReviewSessions { get; set; } = new();
}

public class ReviewSession
{
    public string Id { get; set; } = string.Empty;
    public string ReviewPlanId { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public ReviewSessionStatus Status { get; set; } = ReviewSessionStatus.Pending;
    public int? Score { get; set; } // 0-100 self-assessment score
    public string? Notes { get; set; }
    
    // Navigation
    public ReviewPlan ReviewPlan { get; set; } = null!;
}

public enum ReviewPlanEntityType
{
    Lecture = 0,
    Note = 1
}

public enum ReviewPlanStatus
{
    Active = 0,
    Completed = 1,
    Cancelled = 2
}

public enum ReviewSessionStatus
{
    Pending = 0,
    Completed = 1,
    Skipped = 2
}
