namespace FocusDock.Core.Services;

using FocusDock.Data;
using FocusDock.Data.Models;

public class TodoService : ObservableObject
{
    public event EventHandler<TodoItem>? TodoAdded;
    public event EventHandler<TodoItem>? TodoUpdated;
    public event EventHandler<string>? TodoDeleted; // passes ID
    public event EventHandler<List<TodoItem>>? TodosChanged;

    private List<TodoItem> _todos;

    public TodoService()
    {
        _todos = TodoStore.LoadTodos();
    }

    public List<TodoItem> GetAllTodos() => _todos.ToList();

    public List<TodoItem> GetActiveTodos()
    {
        return _todos
            .Where(t => !t.IsCompleted)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToList();
    }

    public List<TodoItem> GetCompletedTodos()
    {
        return _todos
            .Where(t => t.IsCompleted)
            .OrderByDescending(t => t.CompletedDate)
            .ToList();
    }

    public List<TodoItem> GetOverdueTodos()
    {
        return _todos
            .Where(t => t.IsOverdue())
            .OrderByDescending(t => t.Priority)
            .ToList();
    }

    public List<TodoItem> GetDueSoonTodos(TimeSpan within)
    {
        return _todos
            .Where(t => t.IsDueSoon(within))
            .OrderBy(t => t.DueDate)
            .ToList();
    }

    public List<TodoItem> GetTodosByTag(string tag)
    {
        return _todos
            .Where(t => t.Tags.Contains(tag))
            .ToList();
    }

    public List<TodoItem> GetCanvasLinkedTodos()
    {
        return _todos
            .Where(t => !string.IsNullOrWhiteSpace(t.CanvasAssignmentId))
            .ToList();
    }

    public TodoItem? GetTodo(string id)
    {
        return _todos.FirstOrDefault(t => t.Id == id);
    }

    public void AddTodo(TodoItem todo)
    {
        _todos.Add(todo);
        TodoStore.SaveTodos(_todos);
        TodoAdded?.Invoke(this, todo);
        TodosChanged?.Invoke(this, _todos);
    }

    public void UpdateTodo(TodoItem todo)
    {
        var existing = _todos.FirstOrDefault(t => t.Id == todo.Id);
        if (existing == null) return;

        var index = _todos.IndexOf(existing);
        _todos[index] = todo;
        TodoStore.SaveTodos(_todos);
        TodoUpdated?.Invoke(this, todo);
        TodosChanged?.Invoke(this, _todos);
    }

    public void ToggleTodoComplete(string id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo == null) return;

        todo.IsCompleted = !todo.IsCompleted;
        if (todo.IsCompleted)
        {
            todo.CompletedDate = DateTime.Now;
        }
        else
        {
            todo.CompletedDate = null;
        }

        UpdateTodo(todo);
    }

    public void DeleteTodo(string id)
    {
        _todos.RemoveAll(t => t.Id == id);
        TodoStore.SaveTodos(_todos);
        TodoDeleted?.Invoke(this, id);
        TodosChanged?.Invoke(this, _todos);
    }

    public void DeleteCompleted()
    {
        _todos.RemoveAll(t => t.IsCompleted);
        TodoStore.SaveTodos(_todos);
        TodosChanged?.Invoke(this, _todos);
    }

    public void SetPriority(string id, int priority)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo == null) return;

        todo.Priority = Math.Clamp(priority, 1, 4);
        UpdateTodo(todo);
    }

    public void AddTag(string id, string tag)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo == null || todo.Tags.Contains(tag)) return;

        todo.Tags.Add(tag);
        UpdateTodo(todo);
    }

    public void RemoveTag(string id, string tag)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo == null) return;

        todo.Tags.Remove(tag);
        UpdateTodo(todo);
    }

    /// <summary>
    /// Sync Canvas assignments to todos
    /// </summary>
    public void SyncFromCanvasAssignments(List<CanvasAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            // Check if already have a todo for this assignment
            var existing = _todos.FirstOrDefault(t => t.CanvasAssignmentId == assignment.Id);

            var todo = new TodoItem
            {
                Id = existing?.Id ?? Guid.NewGuid().ToString(),
                Title = $"{assignment.CourseName}: {assignment.Title}",
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                CanvasAssignmentId = assignment.Id,
                CanvasCourseId = assignment.CourseId,
                Source = "Canvas",
                IsCompleted = assignment.IsSubmitted,
                Priority = assignment.IsOverdue() ? 4 : 3,
                Tags = new() { assignment.CourseName }
            };

            if (existing != null)
            {
                todo.CreatedDate = existing.CreatedDate;
                UpdateTodo(todo);
            }
            else
            {
                AddTodo(todo);
            }
        }
    }

    public int GetTotalEstimatedMinutes()
    {
        return _todos
            .Where(t => !t.IsCompleted && t.EstimatedMinutes.HasValue)
            .Sum(t => t.EstimatedMinutes!.Value);
    }

    public int GetTotalActualMinutes()
    {
        return _todos
            .Where(t => t.ActualMinutes.HasValue)
            .Sum(t => t.ActualMinutes!.Value);
    }

    public string GetStatsSummary()
    {
        var total = _todos.Count;
        var completed = _todos.Count(t => t.IsCompleted);
        var active = total - completed;
        var overdue = _todos.Count(t => t.IsOverdue());

        return $"{completed}/{total} completed • {active} active • {overdue} overdue";
    }
}
