using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration config, ILogger<AuthController> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Generate a JWT token for development/testing
        /// POST /api/auth/token
        /// Body: { "username": "your-username" }
        /// </summary>
        [HttpPost("token")]
        public ActionResult<TokenResponse> GenerateToken([FromBody] TokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { error = "Username is required" });
            }

            try
            {
                var jwtKey = _config["Jwt:Key"] ?? "super_dev_secret_key_change_me_please_32chars";
                var jwtIssuer = _config["Jwt:Issuer"] ?? "FocusDeckDev";
                var jwtAudience = _config["Jwt:Audience"] ?? "FocusDeckClients";

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, request.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, request.Username),
                    new Claim(ClaimTypes.Name, request.Username)
                };

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(30), // 30 day expiration
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation("Generated token for user: {Username}", request.Username);

                return Ok(new TokenResponse
                {
                    Token = tokenString,
                    Username = request.Username,
                    ExpiresAt = token.ValidTo
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
