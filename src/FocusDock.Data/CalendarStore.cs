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

    // Async versions for better performance
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

    public static async Task SaveEventsAsync(List<CalendarEvent> events)
    {
        try
        {
            var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(EventsPath, json);
        }
        catch { }
    }

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

    public static async Task SaveAssignmentsAsync(List<CanvasAssignment> assignments)
    {
        try
        {
            var json = JsonSerializer.Serialize(assignments, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(AssignmentsPath, json);
        }
        catch { }
    }

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

    public static async Task SaveSettingsAsync(CalendarSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsPath, json);
        }
        catch { }
    }

    public static async Task ClearCacheAsync()
    {
        try
        {
            // Delete files asynchronously if they exist
            var tasks = new List<Task>();
            
            if (File.Exists(EventsPath))
                tasks.Add(Task.Run(() => File.Delete(EventsPath)));
            
            if (File.Exists(AssignmentsPath))
                tasks.Add(Task.Run(() => File.Delete(AssignmentsPath)));
            
            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }
        catch { }
    }
    
    // Keep synchronous versions for backward compatibility
    public static List<CalendarEvent> LoadEvents() => LoadEventsAsync().GetAwaiter().GetResult();
    public static void SaveEvents(List<CalendarEvent> events) => SaveEventsAsync(events).GetAwaiter().GetResult();
    public static List<CanvasAssignment> LoadAssignments() => LoadAssignmentsAsync().GetAwaiter().GetResult();
    public static void SaveAssignments(List<CanvasAssignment> assignments) => SaveAssignmentsAsync(assignments).GetAwaiter().GetResult();
    public static CalendarSettings LoadSettings() => LoadSettingsAsync().GetAwaiter().GetResult();
    public static void SaveSettings(CalendarSettings settings) => SaveSettingsAsync(settings).GetAwaiter().GetResult();
    public static void ClearCache() => ClearCacheAsync().GetAwaiter().GetResult();
}
