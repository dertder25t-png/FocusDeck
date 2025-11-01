using Microsoft.AspNetCore.Mvc;
using FocusDeck.Shared.Models.Automations;
using FocusDeck.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http; // Added
using Microsoft.Extensions.Configuration; // Added
using System.Collections.Generic; // Added
using System.Threading.Tasks; // Added
using System.Linq; // Added
using System; // Added

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AutomationDbContext _context;
        private readonly ILogger<ServicesController> _logger;
        private readonly IConfiguration _configuration; // Added
        private readonly IHttpClientFactory _httpClientFactory; // Added

        public ServicesController(
            AutomationDbContext context,
            ILogger<ServicesController> logger,
            IConfiguration configuration, // Added
            IHttpClientFactory httpClientFactory) // Added
        {
            _context = context;
            _logger = logger;
            _configuration = configuration; // Added
            _httpClientFactory = httpClientFactory; // Added
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

        // --- NEW ENDPOINT TO GUIDE THE USER ---
        [HttpGet("{service}/setup")]
        public ActionResult<ServiceSetupGuide> GetSetupGuide(ServiceType service)
        {
            ServiceSetupGuide guide;
            switch (service)
            {
                case ServiceType.HomeAssistant:
                    guide = new ServiceSetupGuide
                    {
                        SetupType = "Simple",
                        Title = "Connect Home Assistant",
                        Description = "Please provide your Home Assistant instance URL and a Long-Lived Access Token.",
                        Fields = new List<SetupField>
                        {
                            new SetupField
                            {
                                Key = "haBaseUrl",
                                Label = "Home Assistant URL",
                                HelpText = "The full URL you use to access Home Assistant (e.g., http://homeassistant.local:8123 or https://my-ha.duckdns.org).",
                                InputType = "text"
                            },
                            new SetupField
                            {
                                Key = "access_token",
                                Label = "Long-Lived Access Token",
                                HelpText = "In Home Assistant, click your profile (bottom-left) -> 'Long-Lived Access Tokens' -> 'Create Token'. Give it a name (e.g., FocusDeck) and copy the token here.",
                                InputType = "password"
                            }
                        }
                    };
                    break;
                    
                case ServiceType.Canvas:
                    guide = new ServiceSetupGuide
                    {
                        SetupType = "Simple",
                        Title = "Connect Canvas",
                        Description = "Please provide your school's Canvas URL and a generated Access Token.",
                        Fields = new List<SetupField>
                        {
                            new SetupField
                            {
                                Key = "canvasBaseUrl",
                                Label = "Canvas URL",
                                HelpText = "Your school's Canvas instance URL (e.g., https://pcc.instructure.com).",
                                InputType = "text"
                            },
                            new SetupField
                            {
                                Key = "access_token",
                                Label = "Access Token",
                                HelpText = "In Canvas, go to Account -> Settings -> 'Approved Integrations' -> '+New Access Token'. Give it a name and copy the token here.",
                                InputType = "password"
                            }
                        }
                    };
                    break;

                case ServiceType.GoogleCalendar:
                case ServiceType.GoogleDrive:
                case ServiceType.Spotify:
                    guide = new ServiceSetupGuide
                    {
                        SetupType = "OAuth",
                        Title = $"Connect {service}",
                        Description = $"To connect {service}, you'll be redirected to their website to log in and grant FocusDeck permission.",
                        OAuthButtonText = $"Connect with {service}"
                    };
                    break;
                    
                default:
                    return NotFound(new { message = "Setup guide not available for this service." });
            }
            
            return Ok(guide);
        }

        [HttpPost("connect/{service}")]
        public async Task<ActionResult> Connect(ServiceType service, [FromBody] Dictionary<string, string> credentials)
        {
            // This endpoint is now used by your "Simple" setup
            var connectedService = new ConnectedService
            {
                Id = Guid.NewGuid(),
                UserId = "default_user", // Replace with actual user system
                Service = service,
                AccessToken = credentials.GetValueOrDefault("access_token", string.Empty),
                RefreshToken = credentials.GetValueOrDefault("refresh_token", string.Empty),
                ExpiresAt = null, // OAuth should set this
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
                        // This logic correctly saves haBaseUrl or canvasBaseUrl
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
            // IMPORTANT: Replace placeholders with real values from your appsettings.json
            string clientId = _configuration[$"{service}:ClientId"] ?? "YOUR_CLIENT_ID";
            string redirectUri = $"{Request.Scheme}://{Request.Host}/api/services/oauth/{service}/callback";
            
            string url;

            switch (service)
            {
                case ServiceType.GoogleCalendar:
                    url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope=https://www.googleapis.com/auth/calendar.readonly&access_type=offline&prompt=consent";
                    break;
                case ServiceType.GoogleDrive:
                     url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope=https://www.googleapis.com/auth/drive.readonly&access_type=offline&prompt=consent";
                    break;
                case ServiceType.Spotify:
                    url = $"https://accounts.spotify.com/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope=user-read-playback-state user-modify-playback-state";
                    break;
                default:
                    return BadRequest(new { message = "OAuth not supported for this service" });
            }

            return Ok(new { url });
        }
        
        // --- CRITICAL FIX: REPLACED OAUTH CALLBACK ---
        [HttpGet("oauth/{service}/callback")]
        public async Task<ActionResult> OAuthCallback(ServiceType service, [FromQuery] string code, [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("OAuth flow failed: No code provided.");
            }

            try
            {
                var (accessToken, refreshToken, expiresAt) = await ExchangeCodeForTokenAsync(service, code);

                var connected = new ConnectedService
                {
                    Id = Guid.NewGuid(),
                    UserId = "default_user", // Replace with actual user ID
                    Service = service,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken ?? string.Empty,
                    ExpiresAt = expiresAt,
                    ConnectedAt = DateTime.UtcNow,
                    IsConfigured = true
                };

                _context.ConnectedServices.Add(connected);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("OAuth callback successful for {Service}", service);
                
                // Redirect back to the root of your web app
                return Redirect("/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth callback failed for {Service}", service);
                return BadRequest($"OAuth flow failed: {ex.Message}");
            }
        }

        private async Task<(string AccessToken, string? RefreshToken, DateTime? ExpiresAt)> ExchangeCodeForTokenAsync(ServiceType service, string code)
        {
            var clientId = _configuration[$"{service}:ClientId"];
            var clientSecret = _configuration[$"{service}:ClientSecret"];
            string redirectUri = $"{Request.Scheme}://{Request.Host}/api/services/oauth/{service}/callback";

            string tokenUrl;
            var tokenParams = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId ?? "" },
                { "client_secret", clientSecret ?? "" }
            };

            switch (service)
            {
                case ServiceType.GoogleCalendar:
                case ServiceType.GoogleDrive:
                    tokenUrl = "https://oauth2.googleapis.com/token";
                    break;
                case ServiceType.Spotify:
                    tokenUrl = "https://accounts.spotify.com/api/token";
                    break;
                default:
                    throw new InvalidOperationException("Unsupported service for OAuth token exchange.");
            }

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenParams));

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to exchange token: {StatusCode} - {Body}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Failed to get token from {service}.");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonObject>();
            var accessToken = json?["access_token"]?.ToString() ?? throw new Exception("Missing access_token in response.");
            var refreshToken = json?["refresh_token"]?.ToString();
            var expiresIn = json?["expires_in"]?.GetValue<int>();
            var expiresAt = expiresIn.HasValue ? DateTime.UtcNow.AddSeconds(expiresIn.Value - 60) : (DateTime?)null; // 60s buffer

            return (accessToken, refreshToken, expiresAt);
        }

        // ... (rest of your controller methods: UpdateMetadata, CheckServiceHealth, etc.) ...
        // ... (They are unchanged and should remain) ...

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
                case ServiceType.GoogleDrive: // Added
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
                using var client = _httpClientFactory.CreateClient(); // Use factory
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {service.AccessToken}");
                
                var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/api/"); // Ensure single slash
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

    // --- NEW HELPER CLASSES FOR THE SETUP GUIDE ---

    /// <summary>
    /// Defines the UI and instructions needed to connect a service.
    /// </summary>
    public class ServiceSetupGuide
    {
        /// <summary>
        /// "Simple" (token/URL) or "OAuth" (button click).
        /// </summary>
        public string SetupType { get; set; } = null!;
        
        /// <summary>
        /// e.g., "Connect Home Assistant"
        /// </summary>
        public string Title { get; set; } = null!;
        
        /// <summary>
        /// Main description of the setup process.
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// A list of fields the user must fill out (for "Simple" setup).
        /// </summary>
        public List<SetupField>? Fields { get; set; }
        
        /// <summary>
        /// The text for the connect button (for "OAuth" setup).
        /// </summary>
        public string? OAuthButtonText { get; set; }
    }

    /// <summary>
    /// Represents a single field in a "Simple" setup form.
    /// </summary>
    public class SetupField
    {
        /// <summary>
        /// The key to use when sending data to the 'connect' endpoint (e.g., "haBaseUrl", "access_token").
        /// </summary>
        public string Key { get; set; } = null!; 
        
        /// <summary>
        /// The user-friendly label for the input (e.g., "Home Assistant URL").
        /// </summary>
        public string Label { get; set; } = null!;
        
        /// <summary>
        /// Instructions on how to find this value.
        /// </summary>
        public string HelpText { get; set; } = null!;
        
        /// <summary>
        /// "text" or "password"
        /// </summary>
        public string InputType { get; set; } = "text"; 
    }
}