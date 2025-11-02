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
            return BadRequest(new { message = "Username and password are required" });
        }

        // For development: accept any username/password
        // TODO: Replace with proper user authentication
        var userId = request.Username;
        var roles = new[] { "User" };

        var accessToken = _tokenService.GenerateAccessToken(userId, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7)),
            CreatedAt = DateTime.UtcNow
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
            return BadRequest(new { message = "Refresh token is required" });
        }

        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        // Get user ID from the old access token (even if expired)
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken ?? "");
        if (principal == null)
        {
            return Unauthorized(new { message = "Invalid access token" });
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null || userId != storedToken.UserId)
        {
            return Unauthorized(new { message = "Token mismatch" });
        }

        // Generate new tokens
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var newAccessToken = _tokenService.GenerateAccessToken(userId, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // Revoke old refresh token and create new one
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken;

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7)),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(newRefreshTokenEntity);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken,
            expiresIn = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60) * 60
        });
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (storedToken == null)
        {
            return NotFound(new { message = "Token not found" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (storedToken.UserId != userId)
        {
            return Forbid();
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Token revoked successfully" });
    }
}

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string? AccessToken, string RefreshToken);
public record RevokeRequest(string RefreshToken);
