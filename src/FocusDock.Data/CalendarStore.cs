namespace FocusDock.Data;

using System.Text.Json;
using System.Threading.Tasks;
using FocusDock.Data.Models;

public static class CalendarStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusDeck"
    );
    
    private static readonly string EventsPath = Path.Combine(StorePath, "calendar_events.json");
    private static readonly string AssignmentsPath = Path.Combine(StorePath, "canvas_assignments.json");
    private static readonly string SettingsPath = Path.Combine(StorePath, "calendar_settings.json");

    static CalendarStore()
    {
        Directory.CreateDirectory(StorePath);
    }

    public static List<CalendarEvent> LoadEvents()
    {
        try
        {
            if (!File.Exists(EventsPath))
                return new();

            var json = File.ReadAllText(EventsPath);
            return JsonSerializer.Deserialize<List<CalendarEvent>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }
    
    /// <summary>
    /// Async version of LoadEvents for non-blocking file I/O
    /// </summary>
    public static async Task<List<CalendarEvent>> LoadEventsAsync()
    {
        try
        {
            if (!File.Exists(EventsPath))
                return new();

            var json = await File.ReadAllTextAsync(EventsPath);
            return JsonSerializer.Deserialize<List<CalendarEvent>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SaveEvents(List<CalendarEvent> events)
    {
        try
        {
            var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(EventsPath, json);
        }
        catch { }
    }
    
    /// <summary>
    /// Async version of SaveEvents for non-blocking file I/O
    /// </summary>
    public static async Task SaveEventsAsync(List<CalendarEvent> events)
    {
        try
        {
            var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(EventsPath, json);
        }
        catch { }
    }

    public static List<CanvasAssignment> LoadAssignments()
    {
        try
        {
            if (!File.Exists(AssignmentsPath))
                return new();

            var json = File.ReadAllText(AssignmentsPath);
            return JsonSerializer.Deserialize<List<CanvasAssignment>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }
    
    /// <summary>
    /// Async version of LoadAssignments for non-blocking file I/O
    /// </summary>
    public static async Task<List<CanvasAssignment>> LoadAssignmentsAsync()
    {
        try
        {
            if (!File.Exists(AssignmentsPath))
                return new();

            var json = await File.ReadAllTextAsync(AssignmentsPath);
            return JsonSerializer.Deserialize<List<CanvasAssignment>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SaveAssignments(List<CanvasAssignment> assignments)
    {
        try
        {
            var json = JsonSerializer.Serialize(assignments, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AssignmentsPath, json);
        }
        catch { }
    }
    
    /// <summary>
    /// Async version of SaveAssignments for non-blocking file I/O
    /// </summary>
    public static async Task SaveAssignmentsAsync(List<CanvasAssignment> assignments)
    {
        try
        {
            var json = JsonSerializer.Serialize(assignments, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(AssignmentsPath, json);
        }
        catch { }
    }

    public static CalendarSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<CalendarSettings>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }
    
    /// <summary>
    /// Async version of LoadSettings for non-blocking file I/O
    /// </summary>
    public static async Task<CalendarSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new();

            var json = await File.ReadAllTextAsync(SettingsPath);
            return JsonSerializer.Deserialize<CalendarSettings>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SaveSettings(CalendarSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
    
    /// <summary>
    /// Async version of SaveSettings for non-blocking file I/O
    /// </summary>
    public static async Task SaveSettingsAsync(CalendarSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsPath, json);
        }
        catch { }
    }

    public static void ClearCache()
    {
        try
        {
            if (File.Exists(EventsPath)) File.Delete(EventsPath);
            if (File.Exists(AssignmentsPath)) File.Delete(AssignmentsPath);
        }
        catch { }
    }
}

