namespace FocusDock.Data;

using System.Text.Json;
using System.Threading.Tasks;
using FocusDock.Data.Models;

public static class TodoStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusDeck"
    );
    
    private static readonly string TodosPath = Path.Combine(StorePath, "todos.json");
    private static readonly string PlansPath = Path.Combine(StorePath, "study_plans.json");
    private static readonly string SessionLogsPath = Path.Combine(StorePath, "study_sessions.json");

    static TodoStore()
    {
        Directory.CreateDirectory(StorePath);
    }

    // Async versions for better performance
    public static async Task<List<TodoItem>> LoadTodosAsync()
    {
        try
        {
            if (!File.Exists(TodosPath))
                return new();

            var json = await File.ReadAllTextAsync(TodosPath);
            return JsonSerializer.Deserialize<List<TodoItem>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static async Task SaveTodosAsync(List<TodoItem> todos)
    {
        try
        {
            var json = JsonSerializer.Serialize(todos, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(TodosPath, json);
        }
        catch { }
    }

    public static async Task<List<StudyPlan>> LoadPlansAsync()
    {
        try
        {
            if (!File.Exists(PlansPath))
                return new();

            var json = await File.ReadAllTextAsync(PlansPath);
            return JsonSerializer.Deserialize<List<StudyPlan>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static async Task SavePlansAsync(List<StudyPlan> plans)
    {
        try
        {
            var json = JsonSerializer.Serialize(plans, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(PlansPath, json);
        }
        catch { }
    }

    public static async Task<List<StudySessionLog>> LoadSessionLogsAsync()
    {
        try
        {
            if (!File.Exists(SessionLogsPath))
                return new();

            var json = await File.ReadAllTextAsync(SessionLogsPath);
            return JsonSerializer.Deserialize<List<StudySessionLog>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static async Task SaveSessionLogsAsync(List<StudySessionLog> logs)
    {
        try
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SessionLogsPath, json);
        }
        catch { }
    }

    /// <summary>
    /// Add or update a single todo item (async)
    /// </summary>
    public static async Task SaveTodoAsync(TodoItem todo)
    {
        var todos = await LoadTodosAsync();
        var existing = todos.FirstOrDefault(t => t.Id == todo.Id);
        
        if (existing != null)
            todos.Remove(existing);
        
        todos.Add(todo);
        await SaveTodosAsync(todos);
    }

    /// <summary>
    /// Delete a todo by ID (async)
    /// </summary>
    public static async Task DeleteTodoAsync(string id)
    {
        var todos = await LoadTodosAsync();
        todos.RemoveAll(t => t.Id == id);
        await SaveTodosAsync(todos);
    }
    
    // Keep synchronous versions for backward compatibility
    public static List<TodoItem> LoadTodos() => LoadTodosAsync().GetAwaiter().GetResult();
    public static void SaveTodos(List<TodoItem> todos) => SaveTodosAsync(todos).GetAwaiter().GetResult();
    public static List<StudyPlan> LoadPlans() => LoadPlansAsync().GetAwaiter().GetResult();
    public static void SavePlans(List<StudyPlan> plans) => SavePlansAsync(plans).GetAwaiter().GetResult();
    public static List<StudySessionLog> LoadSessionLogs() => LoadSessionLogsAsync().GetAwaiter().GetResult();
    public static void SaveSessionLogs(List<StudySessionLog> logs) => SaveSessionLogsAsync(logs).GetAwaiter().GetResult();
    public static void SaveTodo(TodoItem todo) => SaveTodoAsync(todo).GetAwaiter().GetResult();
    public static void DeleteTodo(string id) => DeleteTodoAsync(id).GetAwaiter().GetResult();
}
