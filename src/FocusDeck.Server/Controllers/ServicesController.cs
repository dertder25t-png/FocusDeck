using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FocusDeck.Server.Controllers.Models;
using FocusDeck.Server.Controllers.Support;
using FocusDeck.Persistence;
using FocusDeck.Domain.Entities;
using FocusDeck.Domain.Entities.Automations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AutomationDbContext _context;
        private readonly ILogger<ServicesController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string DefaultUserId = "default_user";
        private static readonly string[] SensitiveMetadataIndicators = { "token", "secret", "password" };

        public ServicesController(
            AutomationDbContext context,
            ILogger<ServicesController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var services = await _context.ConnectedServices
                .AsNoTracking()
                .ToListAsync();

            var projections = services.Select(service =>
            {
                var metadata = TryParseMetadata(service.MetadataJson);
                if (metadata != null)
                {
                    RemoveSensitiveMetadata(metadata);
                }

                var configured = service.IsConfigured || !string.IsNullOrEmpty(service.AccessToken);
                return new
                {
                    id = service.Id,
                    service = service.Service.ToString(),
                    configured,
                    connectedAt = service.ConnectedAt,
                    status = configured ? "Connected" : "Not configured",
                    metadata
                };
            });

            return Ok(projections);
        }

        [HttpGet("{service}/setup")]
        public ActionResult<ServiceSetupGuide> GetSetupGuide(ServiceType service)
        {
            if (!ServiceSetupGuideFactory.TryCreate(service, Request, out var guide))
            {
                return NotFound(new { message = "Setup guide not available for this service." });
            }

            return Ok(guide);
        }

        [HttpPost("{service}/config")]
        public async Task<ActionResult> SaveServiceConfig(string service, [FromBody] ServiceConfigDto config)
        {
            try
            {
                var existing = await _context.ServiceConfigurations
                    .FirstOrDefaultAsync(s => s.ServiceName == service);

                if (existing != null)
                {
                    existing.ClientId = config.ClientId;
                    existing.ClientSecret = config.ClientSecret;
                    existing.ApiKey = config.ApiKey;
                    existing.AdditionalConfig = config.AdditionalConfig;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newConfig = new ServiceConfiguration
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
        private static JsonObject? TryParseMetadata(string? metadataJson)
        {
            if (string.IsNullOrWhiteSpace(metadataJson))
            {
                return null;
            }

            try
            {
                return JsonNode.Parse(metadataJson) as JsonObject;
            }
            catch
            {
                return null;
            }
        }

        private static void RemoveSensitiveMetadata(JsonObject metadata)
        {
            var keysToRemove = metadata
                .Where(kv => IsSensitiveKey(kv.Key))
                .Select(kv => kv.Key)
                .ToArray();

            foreach (var key in keysToRemove)
            {
                metadata.Remove(key);
            }
        }

        private static string? BuildMetadataJson(IDictionary<string, string> values)
        {
            if (values.Count == 0)
            {
                return null;
            }

            var metadata = new JsonObject();

            foreach (var (key, value) in values)
            {
                if (IsSensitiveKey(key) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                metadata[key] = value;
            }

            return metadata.Count > 0 ? metadata.ToJsonString() : null;
        }

        private static bool IsSensitiveKey(string key) =>
            SensitiveMetadataIndicators.Any(indicator =>
                key.IndexOf(indicator, StringComparison.OrdinalIgnoreCase) >= 0);

        private static bool DetermineIsConfigured(string? accessToken, string? metadataJson) =>
            !string.IsNullOrWhiteSpace(accessToken) || !string.IsNullOrWhiteSpace(metadataJson);

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
        public async Task<ActionResult> Connect(ServiceType service, [FromBody] Dictionary<string, string>? credentials)
        {
            credentials = credentials != null
                ? new Dictionary<string, string>(credentials, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var accessToken = credentials.GetValueOrDefault("access_token");
            var refreshToken = credentials.GetValueOrDefault("refresh_token");
            var metadataJson = BuildMetadataJson(credentials);

            var existing = await _context.ConnectedServices
                .FirstOrDefaultAsync(s => s.UserId == DefaultUserId && s.Service == service);

            if (existing != null)
            {
                existing.AccessToken = accessToken ?? existing.AccessToken;
                existing.RefreshToken = refreshToken ?? existing.RefreshToken;
                existing.MetadataJson = metadataJson ?? existing.MetadataJson;
                existing.ConnectedAt = DateTime.UtcNow;
                existing.ExpiresAt = null;
                existing.IsConfigured = DetermineIsConfigured(existing.AccessToken, existing.MetadataJson);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated connection for service {Service} ({ServiceId})",
                    service,
                    existing.Id);

                return Ok(new { message = $"{service} connection updated.", serviceId = existing.Id });
            }

            var connectedService = new ConnectedService
            {
                Id = Guid.NewGuid(),
                UserId = DefaultUserId,
                Service = service,
                AccessToken = accessToken ?? string.Empty,
                RefreshToken = refreshToken ?? string.Empty,
                ExpiresAt = null,
                ConnectedAt = DateTime.UtcNow,
                MetadataJson = metadataJson,
                IsConfigured = DetermineIsConfigured(accessToken, metadataJson)
            };

            _context.ConnectedServices.Add(connectedService);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created connection for service {Service} ({ServiceId})",
                service,
                connectedService.Id);

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
                    UserId = DefaultUserId,
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

        [HttpPost("{id}/metadata")]
        public async Task<ActionResult> UpdateMetadata(Guid id, [FromBody] JsonObject metadata)
        {
            var svc = await _context.ConnectedServices.FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();

            try
            {
                RemoveSensitiveMetadata(metadata);
                svc.MetadataJson = metadata.ToJsonString();
                svc.IsConfigured = DetermineIsConfigured(svc.AccessToken, svc.MetadataJson);
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
                case ServiceType.GoogleDrive:
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
                var metadata = TryParseMetadata(service.MetadataJson);
                var baseUrl = metadata?["haBaseUrl"]?.ToString();
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
}


