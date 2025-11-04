using Asp.Versioning;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly AutomationDbContext _db;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        ITokenService tokenService,
        AutomationDbContext db,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _tokenService = tokenService;
        _db = db;
        _logger = logger;
        _configuration = configuration;
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

        var accessToken = _tokenService.GenerateAccessToken(userId, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        // Compute client fingerprint
        var clientId = request.ClientId ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var clientFingerprint = _tokenService.ComputeClientFingerprint(clientId, userAgent);

        // Store refresh token with hash
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = _tokenService.ComputeTokenHash(refreshToken),
            ClientFingerprint = clientFingerprint,
            IssuedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7))
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken,
            expiresIn = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60) * 60 // in seconds
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
                    .Where(t => t.UserId == storedToken.UserId && t.IsActive)
                    .ToListAsync();
                
                foreach (var token in userTokens)
                {
                    token.RevokedUtc = DateTime.UtcNow;
                }
                
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return Unauthorized(new { code = "TOKEN_REUSE", message = "Token reuse detected. All tokens revoked.", traceId = HttpContext.TraceIdentifier });
            }

            if (!storedToken.IsActive)
            {
                await transaction.RollbackAsync();
                return Unauthorized(new { code = "EXPIRED_TOKEN", message = "Refresh token expired", traceId = HttpContext.TraceIdentifier });
            }

            // Verify client fingerprint
            var clientId = request.ClientId ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var clientFingerprint = _tokenService.ComputeClientFingerprint(clientId, userAgent);
            
            if (storedToken.ClientFingerprint != clientFingerprint)
            {
                _logger.LogWarning("Client fingerprint mismatch for token {TokenId}. Expected: {Expected}, Got: {Got}", 
                    storedToken.Id, storedToken.ClientFingerprint, clientFingerprint);
                await transaction.RollbackAsync();
                return Unauthorized(new { code = "FINGERPRINT_MISMATCH", message = "Client fingerprint mismatch", traceId = HttpContext.TraceIdentifier });
            }

            // Get user ID from the old access token (even if expired)
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken ?? "");
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

            // Generate new tokens
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            var newAccessToken = _tokenService.GenerateAccessToken(userId, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newTokenHash = _tokenService.ComputeTokenHash(newRefreshToken);

            // Revoke old refresh token atomically
            storedToken.RevokedUtc = DateTime.UtcNow;
            storedToken.ReplacedByTokenHash = newTokenHash;

            // Create new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = newTokenHash,
                ClientFingerprint = clientFingerprint,
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7))
            };

            _db.RefreshTokens.Add(newRefreshTokenEntity);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken,
                expiresIn = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60) * 60
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

            // Verify the ID token with Google
            using var httpClient = new HttpClient();
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

            // Token is valid, create our JWT tokens
            var userId = tokenInfo.Sub ?? tokenInfo.Email ?? Guid.NewGuid().ToString();
            var roles = new[] { "User" };

            var accessToken = _tokenService.GenerateAccessToken(userId, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();
            
            // Compute client fingerprint
            var clientId = request.ClientId ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var clientFingerprint = _tokenService.ComputeClientFingerprint(clientId, userAgent);

            // Store refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = _tokenService.ComputeTokenHash(refreshToken),
                ClientFingerprint = clientFingerprint,
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7))
            };

            _db.RefreshTokens.Add(refreshTokenEntity);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} authenticated via Google OAuth", userId);

            return Ok(new
            {
                accessToken,
                refreshToken,
                expiresIn = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60) * 60,
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
}

public record LoginRequest(string Username, string Password, string? ClientId = null);
public record RefreshRequest(string? AccessToken, string RefreshToken, string? ClientId = null);
public record RevokeRequest(string RefreshToken);
public record GoogleLoginRequest(string IdToken, string? ClientId = null);

internal class GoogleTokenInfo
{
    public string? Sub { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Picture { get; set; }
    public string? Aud { get; set; }
    public long? Exp { get; set; }
}
