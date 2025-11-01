using Microsoft.AspNetCore.Mvc;
using FocusDeck.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private static readonly List<TodoItem> _tasks = new List<TodoItem>();
        private readonly ILogger<TasksController> _logger;

        public TasksController(ILogger<TasksController> logger)
        {
            _logger = logger;
        }

        // GET: api/tasks
        [HttpGet]
        public ActionResult<IEnumerable<TodoItem>> GetTasks([FromQuery] bool? completed)
        {
            _logger.LogInformation("Getting tasks (completed filter: {Completed})", completed);
            
            var tasks = completed.HasValue 
                ? _tasks.Where(t => t.IsCompleted == completed.Value).ToList()
                : _tasks;

            return Ok(tasks);
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public ActionResult<TodoItem> GetTask(string id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", id);
                return NotFound();
            }
            return Ok(task);
        }

        // POST: api/tasks
        [HttpPost]
        public ActionResult<TodoItem> CreateTask([FromBody] TodoItem newTask)
        {
            if (newTask == null)
            {
                return BadRequest("Task object is null");
            }

            // Ensure ID is set
            if (string.IsNullOrEmpty(newTask.Id))
            {
                newTask.Id = Guid.NewGuid().ToString();
            }

            // Set timestamp
            newTask.CreatedDate = DateTime.UtcNow;

            _tasks.Add(newTask);
            _logger.LogInformation("Created task {TaskId}: {Title}", newTask.Id, newTask.Title);

            return CreatedAtAction(nameof(GetTask), new { id = newTask.Id }, newTask);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateTask(string id, [FromBody] TodoItem updatedTask)
        {
            if (updatedTask == null || id != updatedTask.Id)
            {
                return BadRequest("Invalid task data");
            }

            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for update", id);
                return NotFound();
            }

            // Update properties
            task.Title = updatedTask.Title;
            task.Description = updatedTask.Description;
            task.Priority = updatedTask.Priority;
            task.IsCompleted = updatedTask.IsCompleted;
            task.DueDate = updatedTask.DueDate;
            task.CompletedDate = updatedTask.CompletedDate;
            task.Source = updatedTask.Source;
            task.CanvasAssignmentId = updatedTask.CanvasAssignmentId;
            task.CanvasCourseId = updatedTask.CanvasCourseId;
            task.Tags = updatedTask.Tags;
            task.EstimatedMinutes = updatedTask.EstimatedMinutes;
            task.ActualMinutes = updatedTask.ActualMinutes;
            task.ShowReminder = updatedTask.ShowReminder;
            task.Repeat = updatedTask.Repeat;

            // Set completed date if task is being marked complete
            if (task.IsCompleted && !task.CompletedDate.HasValue)
            {
                task.CompletedDate = DateTime.UtcNow;
            }

            _logger.LogInformation("Updated task {TaskId}", id);
            return NoContent();
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteTask(string id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for deletion", id);
                return NotFound();
            }

            _tasks.Remove(task);
            _logger.LogInformation("Deleted task {TaskId}", id);
            return NoContent();
        }

        // GET: api/tasks/overdue
        [HttpGet("overdue")]
        public ActionResult<IEnumerable<TodoItem>> GetOverdueTasks()
        {
            var overdueTasks = _tasks.Where(t => t.IsOverdue()).ToList();
            return Ok(overdueTasks);
        }

        // GET: api/tasks/due-soon
        [HttpGet("due-soon")]
        public ActionResult<IEnumerable<TodoItem>> GetTasksDueSoon([FromQuery] int days = 7)
        {
            var dueSoonTasks = _tasks.Where(t => t.IsDueSoon(TimeSpan.FromDays(days))).ToList();
            return Ok(dueSoonTasks);
        }

        // GET: api/tasks/priority/{priority}
        [HttpGet("priority/{priority}")]
        public ActionResult<IEnumerable<TodoItem>> GetTasksByPriority(int priority)
        {
            var tasks = _tasks.Where(t => t.Priority == priority).ToList();
            return Ok(tasks);
        }

        // GET: api/tasks/tagged/{tag}
        [HttpGet("tagged/{tag}")]
        public ActionResult<IEnumerable<TodoItem>> GetTasksByTag(string tag)
        {
            var tasks = _tasks.Where(t => t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
            return Ok(tasks);
        }

        // PATCH: api/tasks/{id}/complete
        [HttpPatch("{id}/complete")]
        public IActionResult CompleteTask(string id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            task.IsCompleted = true;
            task.CompletedDate = DateTime.UtcNow;
            _logger.LogInformation("Completed task {TaskId}", id);
            
            return NoContent();
        }

        // PATCH: api/tasks/{id}/uncomplete
        [HttpPatch("{id}/uncomplete")]
        public IActionResult UncompleteTask(string id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            task.IsCompleted = false;
            task.CompletedDate = null;
            _logger.LogInformation("Uncompleted task {TaskId}", id);
            
            return NoContent();
        }
    }
}
