using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FocusDeck.Server.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FocusDeck.Server.Services.Auth;

public sealed class JwtBearerOptionsConfigurator : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IJwtSigningKeyProvider _keyProvider;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly ILogger<JwtBearerOptionsConfigurator> _logger;
    private readonly IOptions<JwtSettings> _jwtSettings;

    public JwtBearerOptionsConfigurator(
        IJwtSigningKeyProvider keyProvider,
        TokenValidationParameters tokenValidationParameters,
        ILogger<JwtBearerOptionsConfigurator> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _keyProvider = keyProvider;
        _tokenValidationParameters = tokenValidationParameters;
        _logger = logger;
        _jwtSettings = jwtSettings;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Create new TokenValidationParameters with dynamic key resolution
        var parameters = _tokenValidationParameters;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = parameters.ValidateIssuer,
            ValidIssuers = parameters.ValidIssuers,
            ValidateAudience = parameters.ValidateAudience,
            ValidAudiences = parameters.ValidAudiences,
            ValidateLifetime = parameters.ValidateLifetime,
            ValidateIssuerSigningKey = parameters.ValidateIssuerSigningKey,
            // Provide an empty collection - the resolver will be called for actual key lookup
            IssuerSigningKeys = new List<SecurityKey>(),
            // Dynamically fetch keys at validation time - this ensures keys are always fresh
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                // First try the provider's cached keys
                var keys = _keyProvider.GetValidationKeys().ToList();
                _logger.LogDebug("JwtBearer validation: Resolved {KeyCount} signing keys from provider", keys.Count);
                
                // If provider returns no keys, construct from settings as fallback
                if (keys.Count == 0)
                {
                    _logger.LogWarning("JwtBearer validation: No keys from provider, constructing from settings");
                    var primaryKey = _jwtSettings.Value.PrimaryKey;
                    if (!string.IsNullOrWhiteSpace(primaryKey))
                    {
                        keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(primaryKey)));
                        _logger.LogDebug("JwtBearer validation: Added primary key from settings");
                    }
                    
                    var secondaryKey = _jwtSettings.Value.SecondaryKey;
                    if (!string.IsNullOrWhiteSpace(secondaryKey))
                    {
                        keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secondaryKey)));
                        _logger.LogDebug("JwtBearer validation: Added secondary key from settings");
                    }
                }
                
                if (keys.Count == 0)
                {
                    _logger.LogError("JwtBearer validation: CRITICAL - No signing keys available!");
                }
                
                return keys;
            },
            ClockSkew = parameters.ClockSkew
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Support SignalR access token via query string for WebSockets/SSE
                // The JS client appends ?access_token=... when connecting to hubs.
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(path) && path.StartsWithSegments("/hubs"))
                {
                    var token = ctx.Request.Query["access_token"].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        ctx.Token = token;
                    }
                }

                // Fallback: read JWT from cookie if Authorization header is missing
                if (string.IsNullOrWhiteSpace(ctx.Token))
                {
                    var cookieToken = ctx.Request.Cookies["focusdeck_access_token"];
                    if (!string.IsNullOrWhiteSpace(cookieToken))
                    {
                        ctx.Token = cookieToken;
                    }
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async ctx =>
            {
                var keyVersion = ctx.Principal?.FindFirst("key_version")?.Value;
                if (!string.IsNullOrWhiteSpace(keyVersion) && !_keyProvider.ContainsVersion(keyVersion))
                {
                    ctx.Fail("Token signed with revoked key");
                    return;
                }

                try
                {
                    var revocation = ctx.HttpContext.RequestServices.GetRequiredService<IAccessTokenRevocationService>();
                    var jti = ctx.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti) && await revocation.IsRevokedAsync(jti, ctx.HttpContext.RequestAborted))
                    {
                        ctx.Fail("Token revoked");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Revocation check failed");
                }
            },
            OnAuthenticationFailed = ctx =>
            {
                var reason = ctx.Exception switch
                {
                    SecurityTokenExpiredException => "expired",
                    SecurityTokenException => "invalid",
                    _ => "invalid"
                };
                AuthTelemetry.RecordJwtValidationFailure(reason);
                _logger.LogWarning(ctx.Exception, "JWT authentication failed ({Reason})", reason);
                return Task.CompletedTask;
            }
        };
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}
