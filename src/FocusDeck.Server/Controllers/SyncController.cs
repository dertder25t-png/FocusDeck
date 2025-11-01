using FocusDeck.Server.Services;
using FocusDeck.Shared.Models.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SyncController : ControllerBase
    {
        private readonly ISyncService _syncService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(ISyncService syncService, ILogger<SyncController> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        private string GetUserId()
        {
            // Try common claim types
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value
                      ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(sub))
            {
                // Fallback for development (should be removed in production)
                sub = "default-user";
            }
            return sub!;
        }

        /// <summary>
        /// Register a device for syncing
        /// POST /api/sync/register
        /// </summary>
        [HttpPost("register")]
    public async Task<ActionResult<DeviceRegistration>> RegisterDevice([FromBody] DeviceRegistrationRequest request)
        {
            try
            {
        var userId = GetUserId();

                var device = await _syncService.RegisterDeviceAsync(
                    request.DeviceId,
                    request.DeviceName,
                    request.Platform,
                    userId
                );

                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register device");
                return StatusCode(500, new { error = "Failed to register device", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all devices for current user
        /// GET /api/sync/devices
        /// </summary>
        [HttpGet("devices")]
        public async Task<ActionResult<List<DeviceRegistration>>> GetDevices()
        {
            try
            {
                var userId = GetUserId();
                var devices = await _syncService.GetUserDevicesAsync(userId);
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get devices");
                return StatusCode(500, new { error = "Failed to get devices", details = ex.Message });
            }
        }

        /// <summary>
        /// Unregister a device
        /// DELETE /api/sync/devices/{deviceId}
        /// </summary>
        [HttpDelete("devices/{deviceId}")]
        public async Task<ActionResult> UnregisterDevice(string deviceId)
        {
            try
            {
                var userId = GetUserId();
                var success = await _syncService.UnregisterDeviceAsync(deviceId, userId);
                if (success)
                {
                    return Ok(new { message = "Device unregistered successfully" });
                }
                return NotFound(new { error = "Device not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister device");
                return StatusCode(500, new { error = "Failed to unregister device", details = ex.Message });
            }
        }

        /// <summary>
        /// Push local changes to server
        /// POST /api/sync/push
        /// </summary>
        [HttpPost("push")]
        public async Task<ActionResult<SyncResult>> PushChanges([FromBody] SyncPushRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _syncService.PushChangesAsync(request, userId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                if (result.Conflicts.Any())
                {
                    return StatusCode(409, result); // 409 Conflict
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push changes");
                return StatusCode(500, new { error = "Failed to push changes", details = ex.Message });
            }
        }

        /// <summary>
        /// Pull changes from server
        /// GET /api/sync/pull
        /// </summary>
        [HttpGet("pull")]
        public async Task<ActionResult<SyncPullResponse>> PullChanges(
            [FromQuery] string deviceId,
            [FromQuery] long lastKnownVersion = 0,
            [FromQuery] SyncEntityType? entityType = null)
        {
            try
            {
                var userId = GetUserId();
                var response = await _syncService.PullChangesAsync(deviceId, lastKnownVersion, userId, entityType);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pull changes");
                return StatusCode(500, new { error = "Failed to pull changes", details = ex.Message });
            }
        }

        /// <summary>
        /// Perform full bidirectional sync (push + pull)
        /// POST /api/sync
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SyncResult>> Sync([FromBody] SyncPushRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _syncService.SyncAsync(request, userId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                if (result.Conflicts.Any())
                {
                    return StatusCode(409, result); // 409 Conflict
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync");
                return StatusCode(500, new { error = "Failed to sync", details = ex.Message });
            }
        }

        /// <summary>
        /// Get sync statistics
        /// GET /api/sync/statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<SyncStatistics>> GetStatistics()
        {
            try
            {
                var userId = GetUserId();
                var stats = await _syncService.GetSyncStatisticsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sync statistics");
                return StatusCode(500, new { error = "Failed to get sync statistics", details = ex.Message });
            }
        }

        /// <summary>
        /// Resolve a conflict
        /// POST /api/sync/resolve
        /// </summary>
        [HttpPost("resolve")]
        public async Task<ActionResult> ResolveConflict([FromBody] ConflictResolutionRequest request)
        {
            try
            {
                var userId = GetUserId();
                var success = await _syncService.ResolveConflictAsync(request.EntityId, request.Resolution, userId);
                
                if (success)
                {
                    return Ok(new { message = "Conflict resolved successfully" });
                }
                
                return BadRequest(new { error = "Failed to resolve conflict" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve conflict");
                return StatusCode(500, new { error = "Failed to resolve conflict", details = ex.Message });
            }
        }
    }

    // Request DTOs
    public class DeviceRegistrationRequest
    {
        public string DeviceId { get; set; } = null!;
        public string DeviceName { get; set; } = null!;
        public DevicePlatform Platform { get; set; }
    }

    public class ConflictResolutionRequest
    {
        public string EntityId { get; set; } = null!;
        public ConflictResolution Resolution { get; set; }
    }
}
