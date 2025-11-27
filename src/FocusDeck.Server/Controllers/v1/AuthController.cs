using Asp.Versioning;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Configuration;
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

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth")]
[EnableRateLimiting("AuthBurst")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly AutomationDbContext _db;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly JwtSettings _jwtSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FocusDeck.Server.Services.Auth.IAccessTokenRevocationService _revocationService;
    private readonly IHubContext<NotificationsHub, INotificationClient> _notifications;
    private readonly ITenantMembershipService _tenantMembership;

    public AuthController(
        ITokenService tokenService,
        AutomationDbContext db,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        JwtSettings jwtSettings,
        IHttpClientFactory httpClientFactory,
        FocusDeck.Server.Services.Auth.IAccessTokenRevocationService revocationService,
        IHubContext<NotificationsHub, INotificationClient> notifications,
        ITenantMembershipService tenantMembership)
    {
        _tokenService = tokenService;
        _db = db;
        _logger = logger;
        _configuration = configuration;
        _jwtSettings = jwtSettings;
        _httpClientFactory = httpClientFactory;
        _revocationService = revocationService;
        _notifications = notifications;
        _tenantMembership = tenantMembership;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Simple dev authentication - replace with proper authentication
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { code = "INVALID_INPUT", message = "Username and password are required", traceId = HttpContext.TraceIdentifier });
        }

        // For development: accept any username/password
        // TODO: Replace with proper user authentication
        var userId = request.Username;
        var roles = new[] { "User" };

        var tenantId = await _tenantMembership.EnsureTenantAsync(userId, request.Username, request.Username, HttpContext.RequestAborted);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(userId, roles, tenantId, HttpContext.RequestAborted);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Compute device context
        var deviceId = request.ClientId ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-device";
        var deviceName = request.DeviceName ?? request.ClientId ?? request.Username ?? "unknown";
        var devicePlatform = request.DevicePlatform;

        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var clientFingerprint = _tokenService.ComputeClientFingerprint(deviceId, userAgent);

        // Store refresh token with hash
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = _tokenService.ComputeTokenHash(refreshToken),
            ClientFingerprint = clientFingerprint,
            IssuedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            DeviceId = deviceId,
            DeviceName = deviceName,
            DevicePlatform = devicePlatform,
            LastAccessUtc = DateTime.UtcNow,
            TenantId = tenantId
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await UpsertDeviceRegistrationAsync(userId, tenantId, deviceId, deviceName, devicePlatform);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken,
            expiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60 // in seconds
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { code = "INVALID_INPUT", message = "Refresh token is required", traceId = HttpContext.TraceIdentifier });
        }

        var tokenHash = _tokenService.ComputeTokenHash(request.RefreshToken);
        
        // Use transaction for atomic token rotation
        using var transaction = await _db.Database.BeginTransactionAsync();
        
        try
        {
            var storedToken = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
            var now = DateTime.UtcNow;

            if (storedToken == null)
            {
                await transaction.RollbackAsync();
                return Unauthorized(new { code = "INVALID_TOKEN", message = "Invalid refresh token", traceId = HttpContext.TraceIdentifier });
            }

            // Check if token was already used (replay attack detection)
            if (storedToken.IsRevoked)
            {
                _logger.LogWarning("Replay attack detected: Token {TokenId} already revoked for user {UserId}", 
                    storedToken.Id, storedToken.UserId);
                
                // Revoke all descendant tokens for this user (token family breach)
                var userTokens = await _db.RefreshTokens
                    .Where(t => t.UserId == storedToken.UserId && t.RevokedUtc == null && t.ExpiresUtc > now)
                    .ToListAsync();
                
                foreach (var token in userTokens)
                {
                    token.RevokedUtc = DateTime.UtcNow;
                }
                
                await _db.SaveChangesAsync();
                await _notifications.Clients.Group($"user:{storedToken.UserId}").ForceLogout(new ForceLogoutMessage("Refresh token reuse detected", storedToken.DeviceId));
                await transaction.CommitAsync();
                
                return Unauthorized(new { code = "TOKEN_REUSE", message = "Token reuse detected. All tokens revoked.", traceId = HttpContext.TraceIdentifier });
            }

            if (storedToken.RevokedUtc != null || storedToken.ExpiresUtc <= now)
            {
                await transaction.RollbackAsync();
                return Unauthorized(new { code = "EXPIRED_TOKEN", message = "Refresh token expired", traceId = HttpContext.TraceIdentifier });
            }

            // Verify client fingerprint
            var deviceId = request.ClientId ?? storedToken.DeviceId ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-device";
            var deviceName = request.DeviceName ?? storedToken.DeviceName ?? deviceId;
            var devicePlatform = request.DevicePlatform ?? storedToken.DevicePlatform;
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var clientFingerprint = _tokenService.ComputeClientFingerprint(deviceId, userAgent);

            if (storedToken.ClientFingerprint != clientFingerprint)
            {
                _logger.LogWarning("Client fingerprint mismatch for token {TokenId}. Expected: {Expected}, Got: {Got}", 
                    storedToken.Id, storedToken.ClientFingerprint, clientFingerprint);
                await transaction.RollbackAsync();
                return Unauthorized(new { code = "FINGERPRINT_MISMATCH", message = "Client fingerprint mismatch", traceId = HttpContext.TraceIdentifier });
            }

            if (!string.IsNullOrWhiteSpace(storedToken.DeviceId) && !string.Equals(storedToken.DeviceId, deviceId, StringComparison.Ordinal))
            {
                _logger.LogWarning("DeviceId mismatch for token {TokenId}. Expected: {Expected}, Got: {Got}", storedToken.Id, storedToken.DeviceId, deviceId);
                await transaction.RollbackAsync();
                return Unauthorized(new { code = "DEVICE_MISMATCH", message = "Device mismatch", traceId = HttpContext.TraceIdentifier });
            }

            // Get user ID from the old access token (even if expired)
            var principal = await _tokenService.GetPrincipalFromExpiredTokenAsync(request.AccessToken ?? "", HttpContext.RequestAborted);
            if (principal == null)
            {
                await transaction.RollbackAsync();
                return Unauthorized(new { code = "INVALID_ACCESS_TOKEN", message = "Invalid access token", traceId = HttpContext.TraceIdentifier });
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || userId != storedToken.UserId)
            {
                await transaction.RollbackAsync();
                return Unauthorized(new { code = "TOKEN_MISMATCH", message = "Token mismatch", traceId = HttpContext.TraceIdentifier });
            }

            var tenantId = storedToken.TenantId;
            if (tenantId == Guid.Empty)
            {
                var tenantClaim = principal.FindFirst("app_tenant_id")?.Value;
                if (!Guid.TryParse(tenantClaim, out tenantId))
                {
                    var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                    var displayName = principal.FindFirst(ClaimTypes.Name)?.Value;
                    tenantId = await _tenantMembership.EnsureTenantAsync(userId, email, displayName, HttpContext.RequestAborted);
                }
                storedToken.TenantId = tenantId;
            }

            // Generate new tokens
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
                var newAccessToken = await _tokenService.GenerateAccessTokenAsync(userId, roles, tenantId, HttpContext.RequestAborted);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newTokenHash = _tokenService.ComputeTokenHash(newRefreshToken);

            // Revoke old refresh token atomically
            storedToken.RevokedUtc = DateTime.UtcNow;
            storedToken.LastAccessUtc = DateTime.UtcNow;
            storedToken.ReplacedByTokenHash = newTokenHash;

            // Create new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = newTokenHash,
                ClientFingerprint = clientFingerprint,
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                DeviceId = deviceId,
                DeviceName = deviceName,
                DevicePlatform = devicePlatform,
                LastAccessUtc = DateTime.UtcNow,
                TenantId = tenantId
            };

            _db.RefreshTokens.Add(newRefreshTokenEntity);
            await UpsertDeviceRegistrationAsync(userId, tenantId, deviceId, deviceName, devicePlatform);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken,
                expiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            await transaction.RollbackAsync();
            return StatusCode(500, new { code = "INTERNAL_ERROR", message = "Token refresh failed", traceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
    {
        var tokenHash = _tokenService.ComputeTokenHash(request.RefreshToken);
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken == null)
        {
            return NotFound(new { code = "TOKEN_NOT_FOUND", message = "Token not found", traceId = HttpContext.TraceIdentifier });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (storedToken.UserId != userId)
        {
            return Forbid();
        }

        storedToken.RevokedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _notifications.Clients.Group($"user:{storedToken.UserId}").ForceLogout(new ForceLogoutMessage("Session revoked", storedToken.DeviceId));

        return Ok(new { message = "Token revoked successfully" });
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
            var roles = new[] { "User" };
            var tenantId = await _tenantMembership.EnsureTenantAsync(userId, tokenInfo.Email, tokenInfo.Name, HttpContext.RequestAborted);

            var accessToken = await _tokenService.GenerateAccessTokenAsync(userId, roles, tenantId, HttpContext.RequestAborted);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var deviceId = request.ClientId ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-device";
            var deviceName = request.DeviceName ?? request.ClientId ?? tokenInfo.Name ?? userId;
            var devicePlatform = request.DevicePlatform;
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var clientFingerprint = _tokenService.ComputeClientFingerprint(deviceId, userAgent);

            // Store refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = _tokenService.ComputeTokenHash(refreshToken),
                ClientFingerprint = clientFingerprint,
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                DeviceId = deviceId,
                DeviceName = deviceName,
                DevicePlatform = devicePlatform,
                LastAccessUtc = DateTime.UtcNow,
                TenantId = tenantId
            };

            _db.RefreshTokens.Add(refreshTokenEntity);
            await UpsertDeviceRegistrationAsync(userId, tenantId, deviceId, deviceName, devicePlatform);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} authenticated via Google OAuth", userId);

            return Ok(new
            {
                accessToken,
                refreshToken,
                expiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
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
    /// Logout current access token (revokes JTI)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var expStr = User.FindFirst("exp")?.Value;
        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(expStr))
        {
            return BadRequest(new { error = "Missing token claims" });
        }
        var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expStr)).UtcDateTime;
        await _revocationService.RevokeAsync(jti, userId, exp, HttpContext.RequestAborted);
        return Ok(new { success = true });
    }

    // List devices (refresh tokens) for the current user
    [HttpGet("devices")]
    [Authorize]
    public async Task<IActionResult> GetDevices()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.IssuedUtc)
            .ToListAsync();

        var deviceIds = tokens
            .Select(t => t.DeviceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var registrations = await _db.DeviceRegistrations
            .Where(d => d.UserId == userId && deviceIds.Contains(d.DeviceId))
            .ToDictionaryAsync(d => d.DeviceId, d => d, StringComparer.Ordinal);

        var summaries = tokens
            .GroupBy(t => string.IsNullOrWhiteSpace(t.DeviceId) ? $"legacy:{t.Id}" : t.DeviceId!, StringComparer.Ordinal)
            .Select(group =>
            {
                registrations.TryGetValue(group.Key, out var registration);

                var orderedTokens = group.OrderByDescending(t => t.LastAccessUtc ?? t.IssuedUtc).ToList();
                var representative = orderedTokens.First();

                var lastSeen = registration?.LastSyncAt ?? orderedTokens.First().LastAccessUtc ?? orderedTokens.First().IssuedUtc;
                var activeCount = orderedTokens.Count(t => t.IsActive);

                return new
                {
                    DeviceId = group.Key.StartsWith("legacy:", StringComparison.Ordinal) ? null : group.Key,
                    DeviceName = registration?.DeviceName ?? representative.DeviceName ?? representative.DeviceId ?? "unknown",
                    DevicePlatform = (registration?.Platform.ToString() ?? representative.DevicePlatform) ?? "unknown",
                    RegisteredAt = registration?.RegisteredAt,
                    LastSeenUtc = lastSeen,
                    AppVersion = registration?.AppVersion,
                    ActiveTokenCount = activeCount,
                    Tokens = orderedTokens.Select(t => new
                    {
                        t.Id,
                        t.IssuedUtc,
                        t.ExpiresUtc,
                        t.LastAccessUtc,
                        t.RevokedUtc,
                        t.DeviceId,
                        t.DeviceName,
                        t.DevicePlatform,
                        IsActive = t.IsActive
                    }).ToList()
                };
            })
            .OrderByDescending(d => d.LastSeenUtc)
            .ToList();

        return Ok(summaries);
    }

    // Revoke a specific refresh token
    [HttpPost("devices/tokens/{id}/revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeDeviceToken(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (token == null) return NotFound(new { code = "NOT_FOUND" });

        token.RevokedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _notifications.Clients.Group($"user:{userId}").ForceLogout(new ForceLogoutMessage("Session revoked", token.DeviceId));
        return Ok(new { success = true });
    }

    // Revoke all tokens associated with a device
    [HttpDelete("devices/{deviceId}")]
    [Authorize]
    public async Task<IActionResult> RevokeDeviceSessions(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return BadRequest(new { code = "INVALID_DEVICE" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var deviceLookupTime = DateTime.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.DeviceId == deviceId && t.RevokedUtc == null && t.ExpiresUtc > deviceLookupTime)
            .ToListAsync();

        if (tokens.Count == 0)
        {
            return NotFound(new { code = "NOT_FOUND", message = "No active tokens for device" });
        }

        foreach (var token in tokens)
        {
            token.RevokedUtc = DateTime.UtcNow;
        }

        var registration = await _db.DeviceRegistrations.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);
        if (registration != null)
        {
            registration.IsActive = false;
            registration.LastSyncAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _notifications.Clients.Group($"user:{userId}").ForceLogout(new ForceLogoutMessage("Device sessions revoked", deviceId));
        return Ok(new { success = true, revoked = tokens.Count });
    }

    // Revoke all devices (all active refresh tokens)
    [HttpPost("devices/revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllDevices()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var revokeLookupTime = DateTime.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > revokeLookupTime)
            .ToListAsync();
        foreach (var t in tokens) t.RevokedUtc = DateTime.UtcNow;

        var registrations = await _db.DeviceRegistrations.Where(d => d.UserId == userId && d.IsActive).ToListAsync();
        foreach (var registration in registrations)
        {
            registration.IsActive = false;
            registration.LastSyncAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _notifications.Clients.Group($"user:{userId}").ForceLogout(new ForceLogoutMessage("All sessions revoked", null));
        return Ok(new { success = true, count = tokens.Count });
    }

    private async Task UpsertDeviceRegistrationAsync(string userId, Guid tenantId, string deviceId, string? deviceName, string? devicePlatform)
    {
        deviceId = deviceId.Trim();
        var registration = await _db.DeviceRegistrations.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);
        var parsedPlatform = ParsePlatform(devicePlatform);

        if (registration == null)
        {
            registration = new DeviceRegistration
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceId = deviceId,
                DeviceName = string.IsNullOrWhiteSpace(deviceName) ? deviceId : deviceName!,
                Platform = parsedPlatform,
                RegisteredAt = DateTime.UtcNow,
                LastSyncAt = DateTime.UtcNow,
                IsActive = true,
                TenantId = tenantId
            };
            _db.DeviceRegistrations.Add(registration);
        }
        else
        {
            registration.DeviceName = string.IsNullOrWhiteSpace(deviceName) ? registration.DeviceName : deviceName!;
            registration.Platform = parsedPlatform;
            registration.LastSyncAt = DateTime.UtcNow;
            registration.IsActive = true;
            if (registration.TenantId == Guid.Empty)
            {
                registration.TenantId = tenantId;
            }
        }
    }

    private static DevicePlatform ParsePlatform(string? devicePlatform)
    {
        if (!string.IsNullOrWhiteSpace(devicePlatform) && Enum.TryParse<DevicePlatform>(devicePlatform, true, out var parsed))
        {
            return parsed;
        }

        return DevicePlatform.Windows;
    }

}

public record LoginRequest(string Username, string Password, string? ClientId = null, string? DeviceName = null, string? DevicePlatform = null);
public record RefreshRequest(string? AccessToken, string RefreshToken, string? ClientId = null, string? DeviceName = null, string? DevicePlatform = null);
public record RevokeRequest(string RefreshToken);
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
