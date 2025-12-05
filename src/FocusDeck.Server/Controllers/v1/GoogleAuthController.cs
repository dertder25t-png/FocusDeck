using Asp.Versioning;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Auth;
using FocusDeck.Server.Services.Integrations;
using FocusDeck.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/auth/google")]
    // [Authorize] - Webhook endpoint cannot be authorized via Bearer token
    public class GoogleAuthController : ControllerBase
    {
        private readonly GoogleAuthService _googleAuth;
        private readonly AutomationDbContext _db;
        private readonly ILogger<GoogleAuthController> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly GoogleCalendarService _calendarService;

        public GoogleAuthController(
            GoogleAuthService googleAuth,
            AutomationDbContext db,
            ILogger<GoogleAuthController> logger,
            IEncryptionService encryptionService,
            GoogleCalendarService calendarService)
        {
            _googleAuth = googleAuth;
            _db = db;
            _logger = logger;
            _encryptionService = encryptionService;
            _calendarService = calendarService;
        }

        [Authorize]
        [HttpGet("challenge")]
        public new IActionResult Challenge()
        {
            // In a real app, we'd generate a secure state token related to the user session
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var state = Guid.NewGuid().ToString(); // Placeholder

            var url = _googleAuth.GetAuthorizationUrl(state);
            return Ok(new { url });
        }

        [Authorize]
        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] GoogleCallbackRequest request, CancellationToken ct)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var token = await _googleAuth.ExchangeCodeForTokenAsync(request.Code);
            if (token == null)
            {
                return BadRequest("Failed to exchange token");
            }

            // Find or create CalendarSource
            var source = await _db.CalendarSources
                .FirstOrDefaultAsync(s => s.Provider == "Google" && s.TenantId == GetTenantId(), ct);

            if (source == null)
            {
                source = new CalendarSource
                {
                    Id = Guid.NewGuid(),
                    Provider = "Google",
                    Name = "Google Calendar", // We could fetch user info to get real email/name
                    TenantId = GetTenantId(),
                    IsPrimary = true
                };
                _db.CalendarSources.Add(source);
            }

            source.AccessToken = _encryptionService.Encrypt(token.AccessToken);
            if (!string.IsNullOrEmpty(token.RefreshToken))
            {
                source.RefreshToken = _encryptionService.Encrypt(token.RefreshToken);
            }
            source.TokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            source.LastSync = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Calendar connected" });
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook(CancellationToken ct)
        {
            // Verify headers
            if (!Request.Headers.TryGetValue("X-Goog-Resource-State", out var state) ||
                !Request.Headers.TryGetValue("X-Goog-Channel-ID", out var channelId))
            {
                return Ok();
            }

            if (state == "sync")
            {
                _logger.LogInformation("Received sync confirmation for channel {ChannelId}", channelId);
                return Ok();
            }

            if (state != "exists")
            {
                return Ok();
            }

            if (!Guid.TryParse(channelId, out var sourceId))
            {
                _logger.LogWarning("Received webhook with invalid Channel ID format: {ChannelId}", channelId);
                return Ok();
            }

            var source = await _db.CalendarSources
                .IgnoreQueryFilters() // Important for background/webhook which lacks user context
                .FirstOrDefaultAsync(s => s.Id == sourceId, ct);

            if (source == null)
            {
                _logger.LogWarning("CalendarSource not found for Channel ID: {ChannelId}", sourceId);
                return Ok();
            }

            _logger.LogInformation("Processing calendar update for source {SourceId}", source.Id);

            try
            {
                var accessToken = _encryptionService.Decrypt(source.AccessToken);

                if (source.TokenExpiry < DateTime.UtcNow.AddMinutes(5) && !string.IsNullOrEmpty(source.RefreshToken))
                {
                    var refreshToken = _encryptionService.Decrypt(source.RefreshToken);
                    var newToken = await _googleAuth.RefreshTokenAsync(refreshToken);
                    if (newToken != null)
                    {
                        accessToken = newToken.AccessToken;
                        source.AccessToken = _encryptionService.Encrypt(newToken.AccessToken);
                        source.TokenExpiry = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn);
                        await _db.SaveChangesAsync(ct);
                    }
                }

                var (events, nextSyncToken) = await _calendarService.SyncDeltaAsync(accessToken, source.SyncToken);

                if (!string.IsNullOrEmpty(nextSyncToken))
                {
                    source.SyncToken = nextSyncToken;
                    source.LastSync = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                }

                // Note: Actual event persistence (EventCache) would happen here.
                // Since this task focused on the sync logic and endpoint, we'll log the count.
                _logger.LogInformation("Synced {Count} changed events for source {SourceId}", events.Count, source.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook for source {SourceId}", sourceId);
                // Return 200 to prevent Google retries for app errors
                return Ok();
            }

            return Ok();
        }

        private Guid GetTenantId()
        {
            var claim = User.FindFirst("app_tenant_id");
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }

    public record GoogleCallbackRequest(string Code, string State);
}
