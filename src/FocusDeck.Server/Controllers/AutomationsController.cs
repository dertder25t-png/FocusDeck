using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FocusDeck.Shared.Models.Automations;
using FocusDeck.Server.Data;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutomationsController : ControllerBase
    {
        private readonly AutomationDbContext _db;
        private readonly ILogger<AutomationsController> _logger;

        public AutomationsController(AutomationDbContext db, ILogger<AutomationsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Automation>>> GetAll()
        {
            try
            {
                var automations = await _db.Automations
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
                return Ok(automations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving automations");
                return StatusCode(500, new { message = "Error retrieving automations" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Automation>> GetById(Guid id)
        {
            try
            {
                var automation = await _db.Automations.FindAsync(id);
                if (automation == null)
                    return NotFound(new { message = "Automation not found" });

                return Ok(automation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving automation {Id}", id);
                return StatusCode(500, new { message = "Error retrieving automation" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Automation>> Create([FromBody] Automation automation)
        {
            try
            {
                automation.Id = Guid.NewGuid();
                automation.CreatedAt = DateTime.UtcNow;
                automation.UpdatedAt = DateTime.UtcNow;
                
                _db.Automations.Add(automation);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Created automation: {Name} (ID: {Id})", automation.Name, automation.Id);
                return CreatedAtAction(nameof(GetById), new { id = automation.Id }, automation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating automation");
                return StatusCode(500, new { message = "Error creating automation" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Automation>> Update(Guid id, [FromBody] Automation automation)
        {
            if (id != automation.Id)
                return BadRequest(new { message = "ID mismatch" });

            try
            {
                var existing = await _db.Automations.FindAsync(id);
                if (existing == null)
                    return NotFound(new { message = "Automation not found" });

                existing.Name = automation.Name;
                existing.Description = automation.Description;
                existing.IsEnabled = automation.IsEnabled;
                existing.Trigger = automation.Trigger;
                existing.Actions = automation.Actions;
                existing.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Updated automation: {Name} (ID: {Id})", automation.Name, id);
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating automation {Id}", id);
                return StatusCode(500, new { message = "Error updating automation" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var automation = await _db.Automations.FindAsync(id);
                if (automation == null)
                    return NotFound(new { message = "Automation not found" });

                _db.Automations.Remove(automation);
                
                // Also delete execution history
                var executions = await _db.AutomationExecutions
                    .Where(e => e.AutomationId == id)
                    .ToListAsync();
                _db.AutomationExecutions.RemoveRange(executions);
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("Deleted automation: {Name} (ID: {Id})", automation.Name, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting automation {Id}", id);
                return StatusCode(500, new { message = "Error deleting automation" });
            }
        }

        [HttpPost("{id}/toggle")]
        public async Task<ActionResult<Automation>> ToggleEnabled(Guid id)
        {
            try
            {
                var automation = await _db.Automations.FindAsync(id);
                if (automation == null)
                    return NotFound(new { message = "Automation not found" });

                automation.IsEnabled = !automation.IsEnabled;
                automation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Toggled automation: {Name} (ID: {Id}) to {State}", 
                    automation.Name, id, automation.IsEnabled ? "enabled" : "disabled");
                
                return Ok(automation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling automation {Id}", id);
                return StatusCode(500, new { message = "Error toggling automation" });
            }
        }

        [HttpPost("{id}/run")]
        public async Task<ActionResult> RunManually(Guid id)
        {
            try
            {
                var automation = await _db.Automations.FindAsync(id);
                if (automation == null)
                    return NotFound(new { message = "Automation not found" });

                // Trigger the automation manually
                automation.LastRunAt = DateTime.UtcNow;
                automation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                
                _logger.LogInformation("Manually triggered automation: {Name} (ID: {Id})", automation.Name, id);
                return Ok(new { message = "Automation triggered successfully", automation });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running automation {Id}", id);
                return StatusCode(500, new { message = "Error running automation" });
            }
        }

        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<AutomationExecution>>> GetHistory(Guid id, [FromQuery] int limit = 50)
        {
            try
            {
                var automation = await _db.Automations.FindAsync(id);
                if (automation == null)
                    return NotFound(new { message = "Automation not found" });

                var history = await _db.AutomationExecutions
                    .Where(e => e.AutomationId == id)
                    .OrderByDescending(e => e.ExecutedAt)
                    .Take(limit)
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving automation history for {Id}", id);
                return StatusCode(500, new { message = "Error retrieving automation history" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetStats()
        {
            try
            {
                var totalAutomations = await _db.Automations.CountAsync();
                var enabledAutomations = await _db.Automations.CountAsync(a => a.IsEnabled);
                var totalExecutions = await _db.AutomationExecutions.CountAsync();
                var successfulExecutions = await _db.AutomationExecutions.CountAsync(e => e.Success);
                
                var last24Hours = DateTime.UtcNow.AddHours(-24);
                var executionsLast24h = await _db.AutomationExecutions
                    .CountAsync(e => e.ExecutedAt >= last24Hours);

                return Ok(new
                {
                    totalAutomations,
                    enabledAutomations,
                    totalExecutions,
                    successfulExecutions,
                    failedExecutions = totalExecutions - successfulExecutions,
                    successRate = totalExecutions > 0 ? (double)successfulExecutions / totalExecutions * 100 : 0,
                    executionsLast24h
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving automation stats");
                return StatusCode(500, new { message = "Error retrieving stats" });
            }
        }
    }
}
