using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;
using FocusDeck.Server.Services;
using FocusDeck.Server.Services.Automations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/automations")]
    [Authorize]
    public class AutomationsController : ControllerBase
    {
        private readonly AutomationDbContext _dbContext;
        private readonly IYamlAutomationLoader _yamlLoader;
        private readonly ActionExecutor _actionExecutor;

        public AutomationsController(AutomationDbContext dbContext, IYamlAutomationLoader yamlLoader, ActionExecutor actionExecutor)
        {
            _dbContext = dbContext;
            _yamlLoader = yamlLoader;
            _actionExecutor = actionExecutor;
        }

        /// <summary>
        /// Lists all active automations.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AutomationDto>>> GetAutomations(CancellationToken cancellationToken)
        {
            var automations = await _dbContext.Automations
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AutomationDto(
                    a.Id,
                    a.Name,
                    a.Description,
                    a.IsEnabled,
                    a.LastRunAt,
                    a.YamlDefinition))
                .ToListAsync(cancellationToken);

            return Ok(automations);
        }

        /// <summary>
        /// Gets a specific automation by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AutomationDto>> GetAutomation(Guid id, CancellationToken cancellationToken)
        {
            var a = await _dbContext.Automations.FindAsync(new object[] { id }, cancellationToken);
            if (a == null) return NotFound();

            return Ok(new AutomationDto(a.Id, a.Name, a.Description, a.IsEnabled, a.LastRunAt, a.YamlDefinition));
        }

        /// <summary>
        /// Creates a new automation.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AutomationDto>> CreateAutomation([FromBody] CreateAutomationRequest request, CancellationToken cancellationToken)
        {
            var automation = new Automation
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                YamlDefinition = request.YamlDefinition
            };

            try
            {
                _yamlLoader.UpdateAutomationFromYaml(automation, request.YamlDefinition);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Invalid YAML definition", details = ex.Message });
            }

            _dbContext.Automations.Add(automation);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetAutomation), new { id = automation.Id, version = "1" },
                new AutomationDto(automation.Id, automation.Name, automation.Description, automation.IsEnabled, automation.LastRunAt, automation.YamlDefinition));
        }

        /// <summary>
        /// Updates an existing automation.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAutomation(Guid id, [FromBody] UpdateAutomationRequest request, CancellationToken cancellationToken)
        {
            var automation = await _dbContext.Automations.FindAsync(new object[] { id }, cancellationToken);
            if (automation == null) return NotFound();

            automation.Name = request.Name;
            automation.Description = request.Description;
            automation.YamlDefinition = request.YamlDefinition;
            automation.UpdatedAt = DateTime.UtcNow;

            try
            {
                _yamlLoader.UpdateAutomationFromYaml(automation, request.YamlDefinition);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Invalid YAML definition", details = ex.Message });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new AutomationDto(automation.Id, automation.Name, automation.Description, automation.IsEnabled, automation.LastRunAt, automation.YamlDefinition));
        }

        /// <summary>
        /// Toggles an automation on or off.
        /// </summary>
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleAutomation(Guid id, CancellationToken cancellationToken)
        {
            var automation = await _dbContext.Automations.FindAsync(new object[] { id }, cancellationToken);
            if (automation == null)
            {
                return NotFound();
            }

            automation.IsEnabled = !automation.IsEnabled;
            automation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { IsEnabled = automation.IsEnabled });
        }

        /// <summary>
        /// Deletes an automation.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAutomation(Guid id, CancellationToken cancellationToken)
        {
            var automation = await _dbContext.Automations.FindAsync(new object[] { id }, cancellationToken);
            if (automation == null)
            {
                return NotFound();
            }

            _dbContext.Automations.Remove(automation);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Manually triggers an automation for testing purposes.
        /// </summary>
        [HttpPost("{id}/run")]
        public async Task<IActionResult> RunAutomation(Guid id, CancellationToken cancellationToken)
        {
            var automation = await _dbContext.Automations.FindAsync(new object[] { id }, cancellationToken);
            if (automation == null) return NotFound();

            // Refresh actions from YAML definition
            try
            {
                _yamlLoader.UpdateAutomationFromYaml(automation, automation.YamlDefinition);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Invalid YAML definition", details = ex.Message });
            }

            // Execute each action in the automation
            var results = new List<object>();
            var allSuccess = true;
            var startTime = DateTime.UtcNow;

            foreach (var action in automation.Actions)
            {
                var result = await _actionExecutor.ExecuteActionAsync(action, _dbContext);
                results.Add(new { action.ActionType, result });
                if (!result.Success) allSuccess = false;
            }

            var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Log execution
            var execution = new AutomationExecution
            {
                Id = 0, // Auto-increment
                AutomationId = automation.Id,
                ExecutedAt = DateTime.UtcNow,
                Success = allSuccess,
                ErrorMessage = allSuccess ? null : "Manual execution completed with errors",
                DurationMs = duration,
                TriggerData = JsonSerializer.Serialize(new { type = "Manual", results })
            };

            _dbContext.AutomationExecutions.Add(execution);
            automation.LastRunAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = allSuccess,
                durationMs = duration,
                results
            });
        }

        /// <summary>
        /// Gets execution history for an automation.
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<AutomationExecutionDto>>> GetHistory(Guid id, CancellationToken cancellationToken)
        {
            var history = await _dbContext.AutomationExecutions
                .AsNoTracking()
                .Where(e => e.AutomationId == id)
                .OrderByDescending(e => e.ExecutedAt)
                .Take(50)
                .Select(e => new AutomationExecutionDto(
                    e.Id,
                    e.ExecutedAt,
                    e.Success,
                    e.ErrorMessage,
                    e.DurationMs,
                    e.TriggerData))
                .ToListAsync(cancellationToken);

            return Ok(history);
        }

        /// <summary>
        /// Gets metadata about available triggers and actions for the builder.
        /// </summary>
        [HttpGet("metadata")]
        public ActionResult<AutomationMetadataDto> GetMetadata()
        {
            // In the future, this should be aggregated from registered services/plugins.
            var metadata = new AutomationMetadataDto(
                Triggers: new List<TriggerMetadataDto>
                {
                    new("AppOpen", "Application Opened", new List<FieldDto> { new("app", "string", "Application Name") }),
                    new("Time", "At Specific Time", new List<FieldDto> { new("time", "time", "Time (HH:mm)") }),
                    new("Interval", "Recurring Interval", new List<FieldDto> { new("minutes", "number", "Minutes") }),
                    new("CalendarEvent", "Calendar Event Started", new List<FieldDto>
                    {
                        new("keyword", "string", "Title Keyword (Optional)"),
                        new("calendar", "string", "Calendar Name (Optional)")
                    }),
                    new("StateChange", "System State Change", new List<FieldDto>
                    {
                        new("type", "string", "Entity Type (e.g. FocusSession)"),
                        new("state", "string", "State (e.g. Active)"),
                        new("change", "string", "Change Type (e.g. Started)")
                    })
                },
                Actions: new List<ActionMetadataDto>
                {
                    new("ShowToast", "Show Notification", new List<FieldDto> { new("message", "string", "Message") }),
                    new("OpenUrl", "Open URL", new List<FieldDto> { new("url", "string", "URL") }),
                    new("OpenNote", "Open Note", new List<FieldDto> { new("title", "string", "Note Title") }),
                    new("PlayMusic", "Play Music", new List<FieldDto> { new("track", "string", "Track URI") })
                }
            );

            return Ok(metadata);
        }
    }

    public record AutomationMetadataDto(List<TriggerMetadataDto> Triggers, List<ActionMetadataDto> Actions);
    public record TriggerMetadataDto(string Type, string Name, List<FieldDto> Fields);
    public record ActionMetadataDto(string Type, string Name, List<FieldDto> Fields);
    public record FieldDto(string Key, string Type, string Label);

    public record AutomationDto(
        Guid Id,
        string Name,
        string? Description,
        bool IsEnabled,
        DateTime? LastRunAt,
        string YamlDefinition
    );

    public record CreateAutomationRequest(string Name, string? Description, string YamlDefinition);
    public record UpdateAutomationRequest(string Name, string? Description, string YamlDefinition);

    public record AutomationExecutionDto(
        int Id,
        DateTime ExecutedAt,
        bool Success,
        string? ErrorMessage,
        long DurationMs,
        string? TriggerData
    );
}
