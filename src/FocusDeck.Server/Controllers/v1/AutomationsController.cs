using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FocusDeck.Persistence;
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

        public AutomationsController(AutomationDbContext dbContext)
        {
            _dbContext = dbContext;
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
                    a.LastRunAt))
                .ToListAsync(cancellationToken);

            return Ok(automations);
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
    }

    public record AutomationDto(
        Guid Id,
        string Name,
        string? Description,
        bool IsEnabled,
        DateTime? LastRunAt
    );
}
