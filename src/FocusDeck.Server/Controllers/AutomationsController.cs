using Microsoft.AspNetCore.Mvc;
using FocusDeck.Shared.Models.Automations;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutomationsController : ControllerBase
    {
        private static readonly List<Automation> _automations = new();

        public static List<Automation> GetAutomations() => _automations;

        [HttpGet]
        public ActionResult<List<Automation>> GetAll()
        {
            return Ok(_automations);
        }

        [HttpGet("{id}")]
        public ActionResult<Automation> GetById(Guid id)
        {
            var automation = _automations.FirstOrDefault(a => a.Id == id);
            if (automation == null)
                return NotFound();

            return Ok(automation);
        }

        [HttpPost]
        public ActionResult<Automation> Create([FromBody] Automation automation)
        {
            automation.Id = Guid.NewGuid();
            automation.CreatedAt = DateTime.UtcNow;
            _automations.Add(automation);

            return CreatedAtAction(nameof(GetById), new { id = automation.Id }, automation);
        }

        [HttpPut("{id}")]
        public ActionResult<Automation> Update(Guid id, [FromBody] Automation automation)
        {
            var existing = _automations.FirstOrDefault(a => a.Id == id);
            if (existing == null)
                return NotFound();

            existing.Name = automation.Name;
            existing.IsEnabled = automation.IsEnabled;
            existing.Trigger = automation.Trigger;
            existing.Actions = automation.Actions;

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(Guid id)
        {
            var automation = _automations.FirstOrDefault(a => a.Id == id);
            if (automation == null)
                return NotFound();

            _automations.Remove(automation);
            return NoContent();
        }

        [HttpPost("{id}/toggle")]
        public ActionResult<Automation> ToggleEnabled(Guid id)
        {
            var automation = _automations.FirstOrDefault(a => a.Id == id);
            if (automation == null)
                return NotFound();

            automation.IsEnabled = !automation.IsEnabled;
            return Ok(automation);
        }

        [HttpPost("{id}/run")]
        public ActionResult RunManually(Guid id)
        {
            var automation = _automations.FirstOrDefault(a => a.Id == id);
            if (automation == null)
                return NotFound();

            // Trigger the automation manually
            automation.LastRunAt = DateTime.UtcNow;
            
            return Ok(new { message = "Automation triggered successfully", automation });
        }
    }
}
