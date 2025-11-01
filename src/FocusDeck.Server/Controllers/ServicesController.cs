using Microsoft.AspNetCore.Mvc;
using FocusDeck.Shared.Models.Automations;
using FocusDeck.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AutomationDbContext _context;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(AutomationDbContext context, ILogger<ServicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var services = await _context.ConnectedServices.ToListAsync();
            
            var list = services.Select(s =>
            {
                // Sanitize metadata by stripping any token/secret/password keys
                JsonObject? meta = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(s.MetadataJson))
                    {
                        meta = JsonNode.Parse(s.MetadataJson) as JsonObject;
                        if (meta != null)
                        {
                            var keysToRemove = meta.Where(kv =>
                                kv.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                                kv.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                                kv.Key.Contains("password", StringComparison.OrdinalIgnoreCase)
                            ).Select(kv => kv.Key).ToList();
                            foreach (var k in keysToRemove) meta.Remove(k);
                        }
                    }
                }
                catch { meta = null; }

                var configured = s.IsConfigured || (!string.IsNullOrEmpty(s.AccessToken));
                return new
                {
                    id = s.Id,
                    service = s.Service.ToString(),
                    configured,
                    connectedAt = s.ConnectedAt,
                    status = configured ? "Connected" : "Not configured",
                    metadata = meta
                };
            }).ToList();

            return Ok(list);
        }

        [HttpPost("connect/{service}")]
        public async Task<ActionResult> Connect(ServiceType service, [FromBody] Dictionary<string, string> credentials)
        {
            // This would typically involve OAuth flow
            var connectedService = new ConnectedService
            {
                Id = Guid.NewGuid(),
                UserId = "default_user", // Replace with actual user system
                Service = service,
                AccessToken = credentials.GetValueOrDefault("access_token", string.Empty),
                RefreshToken = credentials.GetValueOrDefault("refresh_token", string.Empty),
                ExpiresAt = DateTime.UtcNow.AddDays(60),
                ConnectedAt = DateTime.UtcNow,
                IsConfigured = false
            };

            // Capture non-sensitive metadata (e.g., base URLs)
            try
            {
                if (credentials?.Count > 0)
                {
                    var meta = new JsonObject();
                    foreach (var kv in credentials)
                    {
                        if (kv.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                            kv.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                            kv.Key.Contains("password", StringComparison.OrdinalIgnoreCase))
                            continue;
                        meta[kv.Key] = kv.Value;
                    }
                    connectedService.MetadataJson = meta.ToJsonString();
                }
            }
            catch { /* ignore meta errors */ }

            connectedService.IsConfigured = !string.IsNullOrEmpty(connectedService.AccessToken) || !string.IsNullOrEmpty(connectedService.MetadataJson);

            _context.ConnectedServices.Add(connectedService);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully connected service {Service} for user {UserId}", service, connectedService.UserId);

            return Ok(new { message = $"{service} connected successfully", serviceId = connectedService.Id });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Disconnect(Guid id)
        {
            var service = await _context.ConnectedServices.FirstOrDefaultAsync(s => s.Id == id);
            if (service == null)
                return NotFound();

            _context.ConnectedServices.Remove(service);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Disconnected service {ServiceId}", id);
            return NoContent();
        }

        [HttpGet("oauth/{service}/url")]
        public ActionResult<string> GetOAuthUrl(ServiceType service)
        {
            // Generate OAuth URLs for different services
            var urls = new Dictionary<ServiceType, string>
            {
                { ServiceType.GoogleCalendar, "https://accounts.google.com/o/oauth2/v2/auth?client_id=YOUR_CLIENT_ID&redirect_uri=YOUR_REDIRECT&scope=https://www.googleapis.com/auth/calendar.readonly" },
                { ServiceType.Spotify, "https://accounts.spotify.com/authorize?client_id=YOUR_CLIENT_ID&response_type=code&redirect_uri=YOUR_REDIRECT&scope=user-modify-playback-state%20user-read-playback-state" },
                { ServiceType.Canvas, "/api/services/canvas/setup" },
                { ServiceType.HomeAssistant, "/api/services/homeassistant/setup" }
            };

            if (urls.TryGetValue(service, out var url))
                return Ok(new { url });

            return BadRequest(new { message = "OAuth not supported for this service" });
        }

        [HttpGet("oauth/{service}/callback")]
        public async Task<ActionResult> OAuthCallback(ServiceType service, [FromQuery] string code, [FromQuery] string state)
        {
            // NOTE: This is a simplified placeholder. A real implementation should exchange the code for tokens.
            var connected = new ConnectedService
            {
                Id = Guid.NewGuid(),
                UserId = "default_user",
                Service = service,
                AccessToken = $"code:{code}",
                RefreshToken = string.Empty,
                ConnectedAt = DateTime.UtcNow,
                IsConfigured = true
            };
            _context.ConnectedServices.Add(connected);
            await _context.SaveChangesAsync();
            _logger.LogInformation("OAuth callback stored placeholder token for {Service}", service);
            // Redirect back to app
            return Redirect("/");
        }

        [HttpPost("{id}/metadata")]
        public async Task<ActionResult> UpdateMetadata(Guid id, [FromBody] JsonObject metadata)
        {
            var svc = await _context.ConnectedServices.FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();

            try
            {
                // Sanitize
                var keysToRemove = metadata.Where(kv =>
                    kv.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Contains("password", StringComparison.OrdinalIgnoreCase)
                ).Select(kv => kv.Key).ToList();
                foreach (var k in keysToRemove) metadata.Remove(k);

                svc.MetadataJson = metadata.ToJsonString();
                svc.IsConfigured = svc.IsConfigured || !string.IsNullOrEmpty(svc.MetadataJson) || !string.IsNullOrEmpty(svc.AccessToken);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed updating metadata for {ServiceId}", id);
                return BadRequest(new { message = "Failed to update metadata" });
            }

            return Ok(new { message = "Metadata updated" });
        }
    }
}
