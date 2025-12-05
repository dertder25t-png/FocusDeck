namespace FocusDeck.Services.Abstractions;

public interface ICanvasService
{
    Task<List<CanvasAssignment>> GetUpcomingAssignments(string canvasDomain, string accessToken);
    Task<List<CanvasGrade>> GetRecentGrades(string canvasDomain, string accessToken, string courseId);
    Task<List<CanvasAnnouncement>> GetAnnouncements(string canvasDomain, string accessToken);
}

public class CanvasAssignment
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime? DueAt { get; set; }
    public string CourseId { get; set; } = null!;
    public string CourseName { get; set; } = null!;
}

public class CanvasGrade
{
    public string AssignmentId { get; set; } = null!;
    public string AssignmentName { get; set; } = null!;
    public double? Score { get; set; }
    public string? Grade { get; set; }
    public DateTime? GradedAt { get; set; }
}

public class CanvasAnnouncement
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime PostedAt { get; set; }
    public string CourseId { get; set; } = null!;
}
