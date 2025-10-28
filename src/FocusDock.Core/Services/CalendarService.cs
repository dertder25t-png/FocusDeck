namespace FocusDock.Core.Services;

using System.Collections.ObjectModel;
using FocusDock.Data;
using FocusDock.Data.Models;

public class CalendarService
{
    public event EventHandler<List<CalendarEvent>>? EventsUpdated;
    public event EventHandler<List<CanvasAssignment>>? AssignmentsUpdated;

    private List<CalendarEvent> _cachedEvents = new();
    private List<CanvasAssignment> _cachedAssignments = new();
    private CalendarSettings _settings;
    private System.Timers.Timer? _syncTimer;
    private bool _isSyncing = false;

    public CalendarService()
    {
        _settings = CalendarStore.LoadSettings();
        _cachedEvents = CalendarStore.LoadEvents();
        _cachedAssignments = CalendarStore.LoadAssignments();

        // Start sync timer if enabled
        if (_settings.EnableGoogleCalendar || _settings.EnableCanvas)
        {
            StartSync();
        }
    }

    public List<CalendarEvent> GetUpcomingEvents(int daysAhead = 7)
    {
        var cutoff = DateTime.Now.AddDays(daysAhead);
        return _cachedEvents
            .Where(e => e.StartTime > DateTime.Now && e.StartTime <= cutoff)
            .OrderBy(e => e.StartTime)
            .ToList();
    }

    public List<CalendarEvent> GetEventsByDay(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddMilliseconds(-1);
        
        return _cachedEvents
            .Where(e => e.StartTime >= startOfDay && e.StartTime <= endOfDay)
            .OrderBy(e => e.StartTime)
            .ToList();
    }

    public CalendarEvent? GetCurrentEvent()
    {
        return _cachedEvents.FirstOrDefault(e => e.IsHappeningNow());
    }

    public List<CalendarEvent> GetEventsBySource(string source)
    {
        return _cachedEvents
            .Where(e => e.Source == source)
            .OrderBy(e => e.StartTime)
            .ToList();
    }

    public List<CanvasAssignment> GetUpcomingAssignments(int daysAhead = 14)
    {
        var cutoff = DateTime.Now.AddDays(daysAhead);
        return _cachedAssignments
            .Where(a => !a.IsSubmitted && a.DueDate <= cutoff)
            .OrderBy(a => a.DueDate)
            .ToList();
    }

    public List<CanvasAssignment> GetOverdueAssignments()
    {
        return _cachedAssignments
            .Where(a => a.IsOverdue())
            .OrderBy(a => a.DueDate)
            .ToList();
    }

    public List<CanvasAssignment> GetAssignmentsByCourse(string courseId)
    {
        return _cachedAssignments
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.DueDate)
            .ToList();
    }

    public void UpdateSettings(CalendarSettings settings)
    {
        _settings = settings;
        CalendarStore.SaveSettings(settings);

        // Restart sync timer with new interval
        _syncTimer?.Stop();
        if (settings.EnableGoogleCalendar || settings.EnableCanvas)
        {
            StartSync();
        }
    }

    public CalendarSettings GetSettings() => _settings;

    /// <summary>
    /// Manually trigger a sync (e.g., from UI button)
    /// </summary>
    public async Task ManualSync()
    {
        await PerformSync();
    }

    private void StartSync()
    {
        _syncTimer = new System.Timers.Timer(_settings.SyncIntervalMinutes * 60 * 1000);
        _syncTimer.Elapsed += async (s, e) => await PerformSync();
        _syncTimer.AutoReset = true;
        _syncTimer.Start();
    }

    private async Task PerformSync()
    {
        if (_isSyncing) return;

        try
        {
            _isSyncing = true;

            var newEvents = new List<CalendarEvent>();
            var newAssignments = new List<CanvasAssignment>();

            // Integrate Google Calendar API
            if (_settings.EnableGoogleCalendar && !string.IsNullOrWhiteSpace(_settings.GoogleCalendarToken))
            {
                try
                {
                    var googleProvider = new GoogleCalendarProvider(
                        _settings.GoogleClientId ?? "",
                        _settings.GoogleClientSecret ?? ""
                    );

                    var googleEvents = await googleProvider.FetchCalendarEvents(_settings.GoogleCalendarToken);
                    newEvents.AddRange(googleEvents);
                    googleProvider.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Google Calendar sync error: {ex.Message}");
                }
            }

            // Integrate Canvas API
            if (_settings.EnableCanvas && !string.IsNullOrWhiteSpace(_settings.CanvasToken))
            {
                try
                {
                    var canvasProvider = new CanvasApiProvider(
                        _settings.CanvasBaseUrl ?? "https://canvas.instructure.com",
                        _settings.CanvasToken
                    );

                    var canvasAssignments = await canvasProvider.FetchAssignments();
                    newAssignments.AddRange(canvasAssignments);
                    canvasProvider.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Canvas sync error: {ex.Message}");
                }
            }

            // Merge with cached data (keep user-added events)
            if (newEvents.Any())
            {
                _cachedEvents = newEvents;
                CalendarStore.SaveEvents(_cachedEvents);
                EventsUpdated?.Invoke(this, _cachedEvents);
            }

            if (newAssignments.Any())
            {
                _cachedAssignments = newAssignments;
                CalendarStore.SaveAssignments(_cachedAssignments);
                AssignmentsUpdated?.Invoke(this, _cachedAssignments);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Calendar sync error: {ex.Message}");
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Add a manual calendar event (for testing or manual entry)
    /// </summary>
    public void AddEvent(CalendarEvent evt)
    {
        _cachedEvents.Add(evt);
        CalendarStore.SaveEvents(_cachedEvents);
        EventsUpdated?.Invoke(this, _cachedEvents);
    }

    /// <summary>
    /// Add a manual Canvas assignment entry (for testing)
    /// </summary>
    public void AddAssignment(CanvasAssignment assignment)
    {
        _cachedAssignments.Add(assignment);
        CalendarStore.SaveAssignments(_cachedAssignments);
        AssignmentsUpdated?.Invoke(this, _cachedAssignments);
    }

    public void Dispose()
    {
        _syncTimer?.Dispose();
    }
}
