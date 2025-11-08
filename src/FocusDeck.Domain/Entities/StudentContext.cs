namespace FocusDeck.Domain.Entities;

public class StudentContext
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? FocusedAppName { get; set; }
    public string? FocusedWindowTitle { get; set; }
    public int ActivityIntensity { get; set; }
    public bool IsIdle { get; set; }

    public string? OpenContextsJson { get; set; }
}

