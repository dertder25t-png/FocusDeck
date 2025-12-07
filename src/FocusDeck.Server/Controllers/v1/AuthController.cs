using Asp.Versioning;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Auth;
using FocusDeck.Domain.Entities.Sync;
using FocusDeck.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using FocusDeck.Shared.SignalR.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using FocusDeck.Server.Services.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth")]
[EnableRateLimiting("AuthBurst")]
public class AuthController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantMembershipService _tenantMembership;

    public AuthController(
        AutomationDbContext db,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ITenantMembershipService tenantMembership)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _tenantMembership = tenantMembership;
    }

    /// <summary>
    /// Authenticate with Google OAuth
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
        {
            return BadRequest(new { code = "INVALID_INPUT", message = "ID token is required", traceId = HttpContext.TraceIdentifier });
        }

        try
        {
            // Get Google OAuth configuration
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            
            if (string.IsNullOrEmpty(googleClientId))
            {
                _logger.LogError("Google OAuth is not configured. Missing Authentication:Google:ClientId");
                return StatusCode(501, new { code = "NOT_CONFIGURED", message = "Google OAuth is not configured", traceId = HttpContext.TraceIdentifier });
            }

            // Verify the ID token with Google using HttpClientFactory
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={request.IdToken}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token verification failed with status {StatusCode}", response.StatusCode);
                return Unauthorized(new { code = "INVALID_TOKEN", message = "Invalid Google ID token", traceId = HttpContext.TraceIdentifier });
            }

            var tokenInfo = await response.Content.ReadFromJsonAsync<GoogleTokenInfo>();
            
            if (tokenInfo == null || tokenInfo.Aud != googleClientId)
            {
                _logger.LogWarning("Google token audience mismatch. Expected: {Expected}, Got: {Got}", 
                    googleClientId, tokenInfo?.Aud);
                return Unauthorized(new { code = "INVALID_AUDIENCE", message = "Invalid token audience", traceId = HttpContext.TraceIdentifier });
            }

            // Token is valid, extract user ID (require Sub or Email)
            if (string.IsNullOrEmpty(tokenInfo.Sub) && string.IsNullOrEmpty(tokenInfo.Email))
            {
                _logger.LogError("Google token missing both Sub and Email claims");
                return Unauthorized(new { code = "INVALID_TOKEN", message = "Token missing required identity claims", traceId = HttpContext.TraceIdentifier });
            }

            var userId = tokenInfo.Sub ?? tokenInfo.Email!;
            var tenantId = await _tenantMembership.EnsureTenantAsync(userId, tokenInfo.Email, tokenInfo.Name, HttpContext.RequestAborted);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, tokenInfo.Name ?? userId),
                new Claim(ClaimTypes.Email, tokenInfo.Email ?? ""),
                new Claim("app_tenant_id", tenantId.ToString()),
                new Claim(ClaimTypes.Role, "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            _logger.LogInformation("User {UserId} authenticated via Google OAuth", userId);

            return Ok(new
            {
                user = new
                {
                    id = userId,
                    email = tokenInfo.Email,
                    name = tokenInfo.Name,
                    picture = tokenInfo.Picture
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google OAuth authentication");
            return StatusCode(500, new { code = "INTERNAL_ERROR", message = "Authentication failed", traceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Logout current session
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        return Ok(new { success = true });
    }
}

public record GoogleLoginRequest(string IdToken, string? ClientId = null, string? DeviceName = null, string? DevicePlatform = null);

internal class GoogleTokenInfo
{
    public string? Sub { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Picture { get; set; }
    public string? Aud { get; set; }
    public long? Exp { get; set; }
}
