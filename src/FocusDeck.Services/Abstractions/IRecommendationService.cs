namespace FocusDeck.Services.Abstractions;

/// <summary>Service for generating AI-powered study recommendations</summary>
public interface IRecommendationService
{
    /// <summary>Gets personalized study recommendations based on session history</summary>
    Task<StudyRecommendationDto> GetSessionRecommendationsAsync(string subject, int recentDays = 7);

    /// <summary>Generates a recommended study plan for a deadline</summary>
    Task<LearningPathDto> GenerateStudyPathAsync(string subject, DateTime deadline, int hoursAvailable);

    /// <summary>Suggests a break activity based on study pattern</summary>
    Task<BreakActivityDto> SuggestBreakActivityAsync(int sessionNumber, int effectiveness);

    /// <summary>Gets optimal study times based on historical effectiveness</summary>
    Task<List<OptimalTimeSlotDto>> GetOptimalStudyTimesAsync();

    /// <summary>Analyzes learning pattern and identifies weak areas</summary>
    Task<LearningAnalysisDto> AnalyzeLearningPatternAsync(int days = 30);
}

/// <summary>Study recommendation DTO</summary>
public class StudyRecommendationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Subject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationType Type { get; set; }
    public int Priority { get; set; } // 1-5, 5 = highest
    public Dictionary<string, object> Data { get; set; } = new();
    public int AppliedCount { get; set; }
    public DateTime? LastApplied { get; set; }
}

/// <summary>Learning path recommendation</summary>
public class LearningPathDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Subject { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public int TotalHoursNeeded { get; set; }
    public int HoursAvailable { get; set; }
    public List<StudyMilestoneDto> Milestones { get; set; } = new();
    public bool IsAchievable { get; set; }
    public string Strategy { get; set; } = string.Empty; // "intensive", "steady", "last-minute"
}

/// <summary>Milestone in a learning path</summary>
public class StudyMilestoneDto
{
    public int WeekNumber { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int RecommendedHours { get; set; }
    public string ActivityType { get; set; } = string.Empty; // "reading", "practice", "review"
}

/// <summary>Break activity recommendation</summary>
public class BreakActivityDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BreakActivityType Type { get; set; }
    public int DurationMinutes { get; set; }
    public string Url { get; set; } = string.Empty; // YouTube link or resource
    public double? EngagementScore { get; set; } // 0-100, how well user completed it
}

/// <summary>Optimal time slot for studying</summary>
public class OptimalTimeSlotDto
{
    public int Hour { get; set; } // 0-23
    public double AverageEffectiveness { get; set; }
    public int SessionCount { get; set; }
    public bool IsOptimal { get; set; }
}

/// <summary>Analysis of learning patterns</summary>
public class LearningAnalysisDto
{
    public int DaysAnalyzed { get; set; }
    public double ConsistencyScore { get; set; } // 0-100
    public List<string> WeakSubjects { get; set; } = new();
    public List<string> StrongSubjects { get; set; } = new();
    public string OverallPattern { get; set; } = string.Empty;
    public string RecommendedFocus { get; set; } = string.Empty;
    public int SuggestedSessionsPerWeek { get; set; }
    public int SuggestedSessionDurationMinutes { get; set; }
}

/// <summary>Types of recommendations</summary>
public enum RecommendationType
{
    StudyTiming,
    BreakActivity,
    FocusMusic,
    SubjectFocus,
    SessionDuration,
    StudyEnvironment,
    ReviewTiming
}

/// <summary>Types of break activities</summary>
public enum BreakActivityType
{
    PhysicalExercise,
    Meditation,
    Walk,
    Stretching,
    Music,
    Video,
    Game,
    Social,
    Snack,
    Hydrate
}
