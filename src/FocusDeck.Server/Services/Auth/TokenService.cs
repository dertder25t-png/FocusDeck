using FocusDeck.Server.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Server.Services.Auth;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(string userId, string[] roles, Guid tenantId, CancellationToken ct = default);
    string GenerateRefreshToken();
    Task<ClaimsPrincipal?> GetPrincipalFromExpiredTokenAsync(string token, CancellationToken ct = default);
    string ComputeTokenHash(string token);
    string ComputeClientFingerprint(string? clientId, string? userAgent);
}

public class TokenService : ITokenService
{
    private readonly ICryptographicKeyStore _keyStore;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<TokenService> _logger;
    private readonly IMemoryCache _credentialCache;
    private readonly IJwtSigningKeyProvider _signingKeyProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public TokenService(
        ICryptographicKeyStore keyStore,
        IOptions<JwtSettings> jwtSettings,
        IJwtSigningKeyProvider signingKeyProvider,
        ILogger<TokenService> logger,
        IMemoryCache credentialCache)
    {
        _keyStore = keyStore;
        _jwtSettings = jwtSettings.Value;
        _signingKeyProvider = signingKeyProvider;
        _logger = logger;
        _credentialCache = credentialCache;
    }

    public async Task<string> GenerateAccessTokenAsync(string userId, string[] roles, Guid tenantId, CancellationToken ct = default)
    {
        var issuer = _jwtSettings.Issuer;
        var audience = _jwtSettings.Audience;
        var expireMinutes = _jwtSettings.AccessTokenExpirationMinutes;

        var key = await _keyStore.GetPrimaryKeyAsync(ct).WaitAsync(TimeSpan.FromSeconds(5), ct);
        var credentials = GetSigningCredentials(key);
        var keyVersion = KeyRotationHelper.GetKeyVersion(key);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("app_tenant_id", tenantId.ToString()),
            new("key_version", keyVersion)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public Task<ClaimsPrincipal?> GetPrincipalFromExpiredTokenAsync(string token, CancellationToken ct = default)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = _jwtSettings.GetValidIssuers(),
            ValidateAudience = true,
            ValidAudiences = _jwtSettings.GetValidAudiences(),
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = _signingKeyProvider.GetValidationKeys(),
            ValidateLifetime = false,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return Task.FromResult<ClaimsPrincipal?>(principal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get principal from token");
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
    }

    public string ComputeTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    public string ComputeClientFingerprint(string? clientId, string? userAgent)
    {
        var input = $"{clientId ?? "unknown"}|{userAgent ?? "unknown"}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    private SigningCredentials GetSigningCredentials(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _credentialCache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        })!;
    }
}
