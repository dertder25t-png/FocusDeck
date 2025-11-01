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

        [HttpGet("{id}/health")]
        public async Task<ActionResult> CheckServiceHealth(Guid id)
        {
            var svc = await _context.ConnectedServices.FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();

            try
            {
                var health = await PerformHealthCheck(svc);
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for {ServiceId}", id);
                return Ok(new { healthy = false, message = ex.Message, status = "error" });
            }
        }

        private async Task<object> PerformHealthCheck(ConnectedService service)
        {
            // Service-specific health checks
            switch (service.Service)
            {
                case ServiceType.HomeAssistant:
                    return await CheckHomeAssistantHealth(service);
                
                case ServiceType.GoogleCalendar:
                case ServiceType.Spotify:
                    // OAuth services: check if token is expired
                    var isExpired = service.ExpiresAt.HasValue && service.ExpiresAt.Value < DateTime.UtcNow;
                    return new
                    {
                        healthy = !isExpired && !string.IsNullOrEmpty(service.AccessToken),
                        status = isExpired ? "token_expired" : "ok",
                        message = isExpired ? "Token expired, please reconnect" : "Connected",
                        expiresAt = service.ExpiresAt
                    };

                case ServiceType.Canvas:
                case ServiceType.GoogleDrive:
                    // Token-based services: validate token exists
                    return new
                    {
                        healthy = !string.IsNullOrEmpty(service.AccessToken),
                        status = string.IsNullOrEmpty(service.AccessToken) ? "not_configured" : "ok",
                        message = string.IsNullOrEmpty(service.AccessToken) ? "Token not configured" : "Connected"
                    };

                default:
                    // Generic check: ensure token exists
                    return new
                    {
                        healthy = !string.IsNullOrEmpty(service.AccessToken),
                        status = "ok",
                        message = "Service configured"
                    };
            }
        }

        private async Task<object> CheckHomeAssistantHealth(ConnectedService service)
        {
            try
            {
                // Parse metadata for base URL
                JsonObject? meta = null;
                if (!string.IsNullOrWhiteSpace(service.MetadataJson))
                {
                    meta = JsonNode.Parse(service.MetadataJson) as JsonObject;
                }

                var baseUrl = meta?["haBaseUrl"]?.ToString();
                if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(service.AccessToken))
                {
                    return new { healthy = false, status = "not_configured", message = "Missing base URL or token" };
                }

                // Try to ping Home Assistant API
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {service.AccessToken}");
                
                var response = await client.GetAsync($"{baseUrl}/api/");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonDocument.Parse(content);
                    var message = json.RootElement.GetProperty("message").GetString();
                    
                    return new
                    {
                        healthy = true,
                        status = "ok",
                        message = $"Connected: {message}",
                        baseUrl = baseUrl
                    };
                }
                else
                {
                    return new
                    {
                        healthy = false,
                        status = "connection_failed",
                        message = $"Failed to connect: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    healthy = false,
                    status = "error",
                    message = $"Health check error: {ex.Message}"
                };
            }
        }
    }
}
