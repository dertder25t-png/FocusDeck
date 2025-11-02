using FocusDeck.Server.Controllers.Models;
using FocusDeck.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UpdateController : ControllerBase
    {
        private readonly IServerUpdateService _updateService;
        private readonly ILogger<UpdateController> _logger;

        public UpdateController(IServerUpdateService updateService, ILogger<UpdateController> logger)
        {
            _updateService = updateService;
            _logger = logger;
        }

        /// <summary>
        /// Trigger server update (Linux only)
        /// POST /api/update/trigger
        /// </summary>
        [HttpPost("trigger")]
        public async Task<ActionResult<UpdateResponse>> TriggerUpdate()
        {
            var result = await _updateService.TriggerUpdateAsync(HttpContext.RequestAborted);
            if (!result.Success && !result.IsUpdating)
            {
                _logger.LogWarning("Failed to start server update: {Message}", result.Message);
                return BadRequest(result);
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get update status
        /// GET /api/update/status
        /// </summary>
        [HttpGet("status")]
        public ActionResult<UpdateStatusResponse> GetStatus()
        {
            return Ok(_updateService.GetStatus());
        }

        /// <summary>
        /// Check update system configuration
        /// GET /api/update/check-config
        /// </summary>
        [HttpGet("check-config")]
        public async Task<ActionResult<ConfigCheckResponse>> CheckConfiguration()
        {
            var response = await _updateService.CheckConfigurationAsync(HttpContext.RequestAborted);
            return Ok(response);
        }

        /// <summary>
        /// Check for remote updates on GitHub
        /// GET /api/update/check-updates
        /// </summary>
        [HttpGet("check-updates")]
        public async Task<ActionResult<UpdateAvailabilityResult>> CheckForUpdates()
        {
            var result = await _updateService.CheckForUpdatesAsync(HttpContext.RequestAborted);
            return Ok(result);
        }
    }
}
