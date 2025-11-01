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

                case ServiceType.Spotify:
                    guide = new ServiceSetupGuide
                    {
                        SetupType = "OAuth",
                        Title = "Connect Spotify",
                        Description = "Spotify uses OAuth 2.0. Create a Spotify Developer App and paste your credentials below.",
                        Steps = new List<string>
                        {
                            "Go to https://developer.spotify.com/dashboard and log in",
                            "Click 'Create an App'",
                            "Name: 'FocusDeck' | Description: 'Study session automation'",
                            "Accept Terms of Service",
                            "After creation, click 'Edit Settings'",
                            $"Add Redirect URI: {Request.Scheme}://{Request.Host}/api/services/oauth/Spotify/callback",
                            "Save settings",
                            "Copy your Client ID and Client Secret (click 'Show Client Secret')",
                            "Paste them in the fields below",
                            "Click 'Save Configuration'",
                            "Then click 'Start OAuth Flow' to connect your Spotify account"
                        },
                        Links = new List<SetupLink>
                        {
                            new SetupLink { Label = "Spotify Developer Dashboard", Url = "https://developer.spotify.com/dashboard" },
                            new SetupLink { Label = "Spotify API Documentation", Url = "https://developer.spotify.com/documentation/web-api" }
                        },
                        Fields = new List<SetupField>
                        {
                            new SetupField
                            {
                                Key = "clientId",
                                Label = "Client ID",
                                HelpText = "From your Spotify Developer Dashboard",
                                InputType = "text"
                            },
                            new SetupField
                            {
                                Key = "clientSecret",
                                Label = "Client Secret",
                                HelpText = "Click 'Show Client Secret' in the dashboard",
                                InputType = "password"
                            }
                        },
                        OAuthButtonText = "Start OAuth Flow"
                    };
                    break;

                case ServiceType.GoogleCalendar:
                    guide = new ServiceSetupGuide
                    {
                        SetupType = "OAuth",
                        Title = "Connect Google Calendar",
                        Description = "Google Calendar uses OAuth 2.0. Create a Google Cloud Project and paste your credentials below.",
                        Steps = new List<string>
                        {
                            "Go to https://console.cloud.google.com/ and log in",
                            "Create a new project (or select existing)",
                            "Enable the Google Calendar API: Navigate to 'APIs & Services' -> 'Library' -> Search 'Google Calendar API' -> Enable",
                            "Configure OAuth consent screen: 'APIs & Services' -> 'OAuth consent screen'",
                            "Choose 'External' user type (unless using Workspace)",
                            "Fill in App name: 'FocusDeck', User support email, Developer contact",
                            "Add scopes: Click 'Add or Remove Scopes' -> Select '.../auth/calendar.readonly'",
                            "Add test users (your Google account email) if in testing mode",
                            "Create OAuth 2.0 credentials: 'APIs & Services' -> 'Credentials' -> 'Create Credentials' -> 'OAuth client ID'",
                            "Application type: 'Web application'",
                            $"Add Authorized redirect URI: {Request.Scheme}://{Request.Host}/api/services/oauth/GoogleCalendar/callback",
                            "Copy your Client ID and Client Secret",
                            "Paste them in the fields below",
                            "Click 'Save Configuration'",
                            "Then click 'Start OAuth Flow' to connect your Google account"
                        },
                        Links = new List<SetupLink>
                        {
                            new SetupLink { Label = "Google Cloud Console", Url = "https://console.cloud.google.com/" },
                            new SetupLink { Label = "Enable Calendar API", Url = "https://console.cloud.google.com/apis/library/calendar-json.googleapis.com" },
                            new SetupLink { Label = "OAuth Credentials", Url = "https://console.cloud.google.com/apis/credentials" }
                        },
                        Fields = new List<SetupField>
                        {
                            new SetupField
                            {
                                Key = "clientId",
                                Label = "Client ID",
                                HelpText = "From Google Cloud Console (ends with .apps.googleusercontent.com)",
                                InputType = "text"
                            },
                            new SetupField
                            {
                                Key = "clientSecret",
                                Label = "Client Secret",
                                HelpText = "From OAuth 2.0 credentials page",
                                InputType = "password"
                            }
                        },
                        OAuthButtonText = "Start OAuth Flow"
                    };
                    break;

                case ServiceType.GoogleDrive:
                    guide = new ServiceSetupGuide
                    {
                        SetupType = "OAuth",
                        Title = "Connect Google Drive",
                        Description = "Google Drive uses OAuth 2.0. Create a Google Cloud Project and paste your credentials below.",
                        Steps = new List<string>
                        {
                            "Go to https://console.cloud.google.com/ and log in",
                            "Create a new project (or select existing)",
                            "Enable the Google Drive API: Navigate to 'APIs & Services' -> 'Library' -> Search 'Google Drive API' -> Enable",
                            "Configure OAuth consent screen: 'APIs & Services' -> 'OAuth consent screen'",
                            "Choose 'External' user type (unless using Workspace)",
                            "Fill in App name: 'FocusDeck', User support email, Developer contact",
                            "Add scopes: Click 'Add or Remove Scopes' -> Select '.../auth/drive.readonly'",
                            "Add test users (your Google account email) if in testing mode",
                            "Create OAuth 2.0 credentials: 'APIs & Services' -> 'Credentials' -> 'Create Credentials' -> 'OAuth client ID'",
                            "Application type: 'Web application'",
                            $"Add Authorized redirect URI: {Request.Scheme}://{Request.Host}/api/services/oauth/GoogleDrive/callback",
                            "Copy your Client ID and Client Secret",
                            "Paste them in the fields below",
                            "Click 'Save Configuration'",
                            "Then click 'Start OAuth Flow' to connect your Google account"
                        },
                        Links = new List<SetupLink>
                        {
                            new SetupLink { Label = "Google Cloud Console", Url = "https://console.cloud.google.com/" },
                            new SetupLink { Label = "Enable Drive API", Url = "https://console.cloud.google.com/apis/library/drive.googleapis.com" },
                            new SetupLink { Label = "OAuth Credentials", Url = "https://console.cloud.google.com/apis/credentials" }
                        },
                        Fields = new List<SetupField>
                        {
                            new SetupField
                            {
                                Key = "clientId",
                                Label = "Client ID",
                                HelpText = "From Google Cloud Console (ends with .apps.googleusercontent.com)",
                                InputType = "text"
                            },
                            new SetupField
                            {
                                Key = "clientSecret",
                                Label = "Client Secret",
                                HelpText = "From OAuth 2.0 credentials page",
                                InputType = "password"
                            }
                        },
                        OAuthButtonText = "Start OAuth Flow"
                    };
                    break;
                    
                default:
                    return NotFound(new { message = "Setup guide not available for this service." });
            }
            
            return Ok(guide);
        }

        // --- SAVE SERVICE CONFIGURATION (OAuth credentials) ---
        [HttpPost("{service}/config")]
        public async Task<ActionResult> SaveServiceConfig(string service, [FromBody] ServiceConfigDto config)
        {
            try
            {
                var existing = await _context.ServiceConfigurations
                    .FirstOrDefaultAsync(s => s.ServiceName == service);

                if (existing != null)
                {
                    // Update existing
                    existing.ClientId = config.ClientId;
                    existing.ClientSecret = config.ClientSecret;
                    existing.ApiKey = config.ApiKey;
                    existing.AdditionalConfig = config.AdditionalConfig;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new
                    var newConfig = new FocusDeck.Server.Models.ServiceConfiguration
                    {
                        Id = Guid.NewGuid(),
                        ServiceName = service,
                        ClientId = config.ClientId,
                        ClientSecret = config.ClientSecret,
                        ApiKey = config.ApiKey,
                        AdditionalConfig = config.AdditionalConfig,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ServiceConfigurations.Add(newConfig);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Configuration saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save service configuration for {Service}", service);
                return StatusCode(500, new { message = "Failed to save configuration" });
            }
        }

        // --- GET SERVICE CONFIGURATION ---
        [HttpGet("{service}/config")]
        public async Task<ActionResult> GetServiceConfig(string service)
        {
            var config = await _context.ServiceConfigurations
                .FirstOrDefaultAsync(s => s.ServiceName == service);

            if (config == null)
            {
                return Ok(new { configured = false });
            }

            // Return without exposing sensitive data fully (show masked versions)
            return Ok(new
            {
                configured = true,
                hasClientId = !string.IsNullOrEmpty(config.ClientId),
                hasClientSecret = !string.IsNullOrEmpty(config.ClientSecret),
                hasApiKey = !string.IsNullOrEmpty(config.ApiKey),
                clientIdPreview = MaskSecret(config.ClientId),
                updatedAt = config.UpdatedAt
            });
        }

        // --- DELETE SERVICE CONFIGURATION ---
        [HttpDelete("{service}/config")]
        public async Task<ActionResult> DeleteServiceConfig(string service)
        {
            var config = await _context.ServiceConfigurations
                .FirstOrDefaultAsync(s => s.ServiceName == service);

            if (config != null)
            {
                _context.ServiceConfigurations.Remove(config);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Configuration deleted" });
        }

        private string? MaskSecret(string? secret)
        {
            if (string.IsNullOrEmpty(secret)) return null;
            if (secret.Length <= 8) return "****";
            return secret.Substring(0, 4) + "..." + secret.Substring(secret.Length - 4);
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
        public async Task<ActionResult<string>> GetOAuthUrl(ServiceType service)
        {
            // Try to get credentials from database first, fall back to appsettings
            var config = await _context.ServiceConfigurations
                .FirstOrDefaultAsync(s => s.ServiceName == service.ToString());

            string? clientId = config?.ClientId ?? _configuration[$"{service}:ClientId"];
            
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest(new { 
                    message = $"OAuth credentials not configured for {service}. Please configure them in the UI first.",
                    needsConfig = true 
                });
            }

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
            // Try to get credentials from database first, fall back to appsettings
            var config = await _context.ServiceConfigurations
                .FirstOrDefaultAsync(s => s.ServiceName == service.ToString());

            var clientId = config?.ClientId ?? _configuration[$"{service}:ClientId"];
            var clientSecret = config?.ClientSecret ?? _configuration[$"{service}:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException($"OAuth credentials not configured for {service}.");
            }

            string redirectUri = $"{Request.Scheme}://{Request.Host}/api/services/oauth/{service}/callback";

            string tokenUrl;
            var tokenParams = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
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
        /// Step-by-step instructions for setting up the service.
        /// </summary>
        public List<string>? Steps { get; set; }

        /// <summary>
        /// Helpful documentation links.
        /// </summary>
        public List<SetupLink>? Links { get; set; }

        /// <summary>
        /// Required server-side configuration (appsettings.json).
        /// </summary>
        public List<string>? RequiredServerConfig { get; set; }

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
    /// Represents a documentation link.
    /// </summary>
    public class SetupLink
    {
        public string Label { get; set; } = null!;
        public string Url { get; set; } = null!;
    }

    /// <summary>
    /// DTO for saving service configuration from the UI
    /// </summary>
    public class ServiceConfigDto
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ApiKey { get; set; }
        public string? AdditionalConfig { get; set; }
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