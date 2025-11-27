using Asp.Versioning;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Auth;
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
    [Authorize]
    public class GoogleAuthController : ControllerBase
    {
        private readonly GoogleAuthService _googleAuth;
        private readonly AutomationDbContext _db;
        private readonly ILogger<GoogleAuthController> _logger;
        private readonly IEncryptionService _encryptionService;

        public GoogleAuthController(GoogleAuthService googleAuth, AutomationDbContext db, ILogger<GoogleAuthController> logger, IEncryptionService encryptionService)
        {
            _googleAuth = googleAuth;
            _db = db;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        [HttpGet("challenge")]
        public new IActionResult Challenge()
        {
            // In a real app, we'd generate a secure state token related to the user session
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var state = Guid.NewGuid().ToString(); // Placeholder

            var url = _googleAuth.GetAuthorizationUrl(state);
            return Ok(new { url });
        }

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

        private Guid GetTenantId()
        {
            // Helper to get tenant ID from claims or context
            // Assuming standard claim or resolved via middleware
            var claim = User.FindFirst("app_tenant_id");
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }

    public record GoogleCallbackRequest(string Code, string State);
}
