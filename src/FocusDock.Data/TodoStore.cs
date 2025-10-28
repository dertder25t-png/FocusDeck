namespace FocusDock.Data;

using System.Text.Json;
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

    public static List<TodoItem> LoadTodos()
    {
        try
        {
            if (!File.Exists(TodosPath))
                return new();

            var json = File.ReadAllText(TodosPath);
            return JsonSerializer.Deserialize<List<TodoItem>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SaveTodos(List<TodoItem> todos)
    {
        try
        {
            var json = JsonSerializer.Serialize(todos, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(TodosPath, json);
        }
        catch { }
    }

    public static List<StudyPlan> LoadPlans()
    {
        try
        {
            if (!File.Exists(PlansPath))
                return new();

            var json = File.ReadAllText(PlansPath);
            return JsonSerializer.Deserialize<List<StudyPlan>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SavePlans(List<StudyPlan> plans)
    {
        try
        {
            var json = JsonSerializer.Serialize(plans, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PlansPath, json);
        }
        catch { }
    }

    public static List<StudySessionLog> LoadSessionLogs()
    {
        try
        {
            if (!File.Exists(SessionLogsPath))
                return new();

            var json = File.ReadAllText(SessionLogsPath);
            return JsonSerializer.Deserialize<List<StudySessionLog>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SaveSessionLogs(List<StudySessionLog> logs)
    {
        try
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SessionLogsPath, json);
        }
        catch { }
    }

    /// <summary>
    /// Add or update a single todo item
    /// </summary>
    public static void SaveTodo(TodoItem todo)
    {
        var todos = LoadTodos();
        var existing = todos.FirstOrDefault(t => t.Id == todo.Id);
        
        if (existing != null)
            todos.Remove(existing);
        
        todos.Add(todo);
        SaveTodos(todos);
    }

    /// <summary>
    /// Delete a todo by ID
    /// </summary>
    public static void DeleteTodo(string id)
    {
        var todos = LoadTodos();
        todos.RemoveAll(t => t.Id == id);
        SaveTodos(todos);
    }
}
