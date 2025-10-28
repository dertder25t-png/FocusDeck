namespace FocusDock.Data.Models;

/// <summary>
/// Represents a calendar event from Google Calendar or Canvas
/// </summary>
public class CalendarEvent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// Source: "GoogleCalendar" or "Canvas"
    /// </summary>
    public string Source { get; set; } = "GoogleCalendar";
    
    /// <summary>
    /// Optional: Canvas course ID if from Canvas
    /// </summary>
    public string? CourseId { get; set; }
    
    /// <summary>
    /// Color for UI display
    /// </summary>
    public string? ColorHex { get; set; }
    
    /// <summary>
    /// If true, automatically apply a layout when this event starts
    /// </summary>
    public bool AutoApplyLayout { get; set; } = false;
    
    /// <summary>
    /// Layout preset name to apply (e.g., "Study Focus")
    /// </summary>
    public string? LayoutPresetName { get; set; }
    
    /// <summary>
    /// Whether user is attending
    /// </summary>
    public bool IsAttending { get; set; } = true;
    
    /// <summary>
    /// How many minutes before the event to trigger automation
    /// </summary>
    public int NotificationMinutesBefore { get; set; } = 5;

    public bool IsHappeningNow()
    {
        var now = DateTime.Now;
        return now >= StartTime && now < EndTime;
    }

    public bool IsUpcoming(TimeSpan within)
    {
        var now = DateTime.Now;
        return StartTime > now && StartTime <= now.Add(within);
    }

    public int MinutesUntilStart()
    {
        return (int)(StartTime - DateTime.Now).TotalMinutes;
    }

    public int DurationMinutes()
    {
        return (int)(EndTime - StartTime).TotalMinutes;
    }
}

/// <summary>
/// Represents a Canvas assignment or due date
/// </summary>
public class CanvasAssignment
{
    public string Id { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsSubmitted => SubmittedAt.HasValue;
    
    /// <summary>
    /// Points possible for this assignment
    /// </summary>
    public double? PointsPossible { get; set; }
    
    /// <summary>
    /// Points earned (null if not graded)
    /// </summary>
    public double? PointsEarned { get; set; }
    
    public string? SubmissionUrl { get; set; }
    
    /// <summary>
    /// How many days before due date to show reminder
    /// </summary>
    public int ReminderDaysBefore { get; set; } = 3;

    public bool IsOverdue()
    {
        return !IsSubmitted && DateTime.Now > DueDate;
    }

    public bool IsDueSoon(TimeSpan within)
    {
        return !IsSubmitted && DueDate > DateTime.Now && DueDate <= DateTime.Now.Add(within);
    }

    public int DaysDueIn()
    {
        if (IsSubmitted) return -1;
        return (int)(DueDate - DateTime.Now).TotalDays;
    }
}

/// <summary>
/// Calendar synchronization settings
/// </summary>
public class CalendarSettings
{
    /// <summary>
    /// Google OAuth2 Client ID
    /// </summary>
    public string? GoogleClientId { get; set; }
    
    /// <summary>
    /// Google OAuth2 Client Secret
    /// </summary>
    public string? GoogleClientSecret { get; set; }
    
    /// <summary>
    /// Google Calendar API access token
    /// </summary>
    public string? GoogleCalendarToken { get; set; }
    
    /// <summary>
    /// Google Calendar refresh token for token refresh
    /// </summary>
    public string? GoogleRefreshToken { get; set; }
    
    /// <summary>
    /// List of Google Calendar IDs to sync (space-separated)
    /// </summary>
    public string GoogleCalendarIds { get; set; } = string.Empty;
    
    /// <summary>
    /// Canvas API token
    /// </summary>
    public string? CanvasToken { get; set; }
    
    /// <summary>
    /// Canvas instance URL (e.g., https://canvas.instructure.com)
    /// </summary>
    public string CanvasInstanceUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Canvas base URL (alias for CanvasInstanceUrl for API provider)
    /// </summary>
    public string? CanvasBaseUrl => string.IsNullOrWhiteSpace(CanvasInstanceUrl) ? null : CanvasInstanceUrl;
    
    /// <summary>
    /// How often to sync calendar (minutes)
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 15;
    
    /// <summary>
    /// Enable Google Calendar sync
    /// </summary>
    public bool EnableGoogleCalendar { get; set; } = false;
    
    /// <summary>
    /// Enable Canvas sync
    /// </summary>
    public bool EnableCanvas { get; set; } = false;
    
    /// <summary>
    /// Show personal calendar events in dock
    /// </summary>
    public bool ShowPersonalEvents { get; set; } = true;
    
    /// <summary>
    /// Show class/course events in dock
    /// </summary>
    public bool ShowCourseEvents { get; set; } = true;
    
    /// <summary>
    /// Days in advance to fetch calendar events
    /// </summary>
    public int EventsLookAheadDays { get; set; } = 30;
}
