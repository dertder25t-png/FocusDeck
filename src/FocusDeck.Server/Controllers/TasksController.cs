using Microsoft.AspNetCore.Mvc;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly AutomationDbContext _context;
        private readonly ILogger<TasksController> _logger;

        public TasksController(AutomationDbContext context, ILogger<TasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTasks([FromQuery] bool? completed)
        {
            _logger.LogInformation("Getting tasks (completed filter: {Completed})", completed);
            
            var query = _context.TodoItems.AsNoTracking();

            if (completed.HasValue)
            {
                query = query.Where(t => t.IsCompleted == completed.Value);
            }

            var tasks = await query
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            return Ok(tasks);
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTask(string id)
        {
            var task = await _context.TodoItems.FindAsync(id);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", id);
                return NotFound();
            }
            return Ok(task);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateTask([FromBody] TodoItem newTask)
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

            _context.TodoItems.Add(newTask);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created task {TaskId}: {Title}", newTask.Id, newTask.Title);

            return CreatedAtAction(nameof(GetTask), new { id = newTask.Id }, newTask);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(string id, [FromBody] TodoItem updatedTask)
        {
            if (updatedTask == null || id != updatedTask.Id)
            {
                return BadRequest("Invalid task data");
            }

            var task = await _context.TodoItems.FindAsync(id);
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

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated task {TaskId}", id);
            return NoContent();
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(string id)
        {
            var task = await _context.TodoItems.FindAsync(id);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for deletion", id);
                return NotFound();
            }

            _context.TodoItems.Remove(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted task {TaskId}", id);
            return NoContent();
        }

        // GET: api/tasks/overdue
        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetOverdueTasks()
        {
            var now = DateTime.UtcNow;
            var overdueTasks = await _context.TodoItems
                .Where(t => !t.IsCompleted && t.DueDate.HasValue && now > t.DueDate.Value.AddDays(1))
                .ToListAsync();

            return Ok(overdueTasks);
        }

        // GET: api/tasks/due-soon
        [HttpGet("due-soon")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTasksDueSoon([FromQuery] int days = 7)
        {
            var now = DateTime.UtcNow;
            var threshold = now.AddDays(days);

            var dueSoonTasks = await _context.TodoItems
                .Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value > now && t.DueDate.Value <= threshold)
                .ToListAsync();

            return Ok(dueSoonTasks);
        }

        // GET: api/tasks/priority/{priority}
        [HttpGet("priority/{priority}")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTasksByPriority(int priority)
        {
            var tasks = await _context.TodoItems
                .Where(t => t.Priority == priority)
                .ToListAsync();
            return Ok(tasks);
        }

        // GET: api/tasks/tagged/{tag}
        [HttpGet("tagged/{tag}")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTasksByTag(string tag)
        {
            // Ensure tenant filter is applied via Global Query Filter in DbContext
            // We fetch all items *for this tenant* first, then filter by tag in memory
            // because List<string> JSON translation in SQLite can be tricky.
            // This is safe because the tenant filter reduces the dataset significantly.

            var tenantTasks = await _context.TodoItems.AsNoTracking().ToListAsync();

            var taggedTasks = tenantTasks
                .Where(t => t.Tags != null && t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .ToList();

            return Ok(taggedTasks);
        }

        // PATCH: api/tasks/{id}/complete
        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> CompleteTask(string id)
        {
            var task = await _context.TodoItems.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            task.IsCompleted = true;
            task.CompletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Completed task {TaskId}", id);
            
            return NoContent();
        }

        // PATCH: api/tasks/{id}/uncomplete
        [HttpPatch("{id}/uncomplete")]
        public async Task<IActionResult> UncompleteTask(string id)
        {
            var task = await _context.TodoItems.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            task.IsCompleted = false;
            task.CompletedDate = null;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Uncompleted task {TaskId}", id);
            
            return NoContent();
        }
    }
}
