using FocusDeck.Server.Configuration;
using FocusDeck.Server.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ITokenService tokenService, JwtSettings jwtSettings, ILogger<AuthController> logger)
        {
            _tokenService = tokenService;
            _jwtSettings = jwtSettings;
            _logger = logger;
        }

        /// <summary>
        /// Generate a JWT token for development/testing
        /// POST /api/auth/token
        /// Body: { "username": "your-username" }
        /// </summary>
        [HttpPost("token")]
        public async Task<ActionResult<TokenResponse>> GenerateToken([FromBody] TokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { error = "Username is required" });
            }

            try
            {
                var tokenString = await _tokenService.GenerateAccessTokenAsync(request.Username, new[] { "Dev" }, Guid.NewGuid());

                _logger.LogInformation("Generated token for user: {Username}", request.Username);

                return Ok(new TokenResponse
                {
                    Token = tokenString,
                    Username = request.Username,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate token");
                return StatusCode(500, new { error = "Failed to generate token", details = ex.Message });
            }
        }

        /// <summary>
        /// Validate and decode a JWT token (for debugging)
        /// GET /api/auth/validate?token=xxx
        /// </summary>
        [HttpGet("validate")]
        public ActionResult<TokenInfo> ValidateToken([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { error = "Token is required" });
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                return Ok(new TokenInfo
                {
                    Username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                    IssuedAt = jwtToken.ValidFrom,
                    ExpiresAt = jwtToken.ValidTo,
                    IsExpired = jwtToken.ValidTo < DateTime.UtcNow,
                    Claims = jwtToken.Claims.Select(c => new ClaimInfo
                    {
                        Type = c.Type,
                        Value = c.Value
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Invalid token", details = ex.Message });
            }
        }
    }

    public class TokenRequest
    {
        public string Username { get; set; } = null!;
    }

    public class TokenResponse
    {
        public string Token { get; set; } = null!;
        public string Username { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

    public class TokenInfo
    {
        public string? Username { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
        public List<ClaimInfo> Claims { get; set; } = new();
    }

    public class ClaimInfo
    {
        public string Type { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
