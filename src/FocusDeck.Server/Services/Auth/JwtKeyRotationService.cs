using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FocusDeck.Server.Services.Auth;

public sealed class JwtKeyRotationService : BackgroundService
{
    private readonly ICryptographicKeyStore _keyStore;
    private readonly JwtSettings _settings;
    private readonly ILogger<JwtKeyRotationService> _logger;
    private readonly IJwtSigningKeyProvider _signingKeyProvider;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IHostEnvironment _environment;

    public JwtKeyRotationService(
        ICryptographicKeyStore keyStore,
        IOptions<JwtSettings> settings,
        ILogger<JwtKeyRotationService> logger,
        IJwtSigningKeyProvider signingKeyProvider,
        TokenValidationParameters tokenValidationParameters,
        IHostEnvironment environment)
    {
        _keyStore = keyStore;
        _settings = settings.Value;
        _logger = logger;
        _signingKeyProvider = signingKeyProvider;
        _tokenValidationParameters = tokenValidationParameters;
        _environment = environment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_environment.IsEnvironment("Testing"))
        {
            _logger.LogInformation("JWT key rotation disabled in the testing environment");
            return;
        }

        var interval = _settings.GetKeyRotationInterval();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Task.Delay supports max ~24 days. If interval is larger, we need to loop.
                var remaining = interval;
                while (remaining > TimeSpan.Zero)
                {
                    var currentDelay = remaining > TimeSpan.FromDays(24) ? TimeSpan.FromDays(24) : remaining;
                    await Task.Delay(currentDelay, stoppingToken);
                    remaining -= currentDelay;
                }
                await RotateKeysAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT key rotation run failed");
            }
        }
    }

    private async Task RotateKeysAsync(CancellationToken ct)
    {
        var newKey = KeyRotationHelper.GenerateSecureKey();
        await _keyStore.RotateKeyAsync(newKey, ct);
        _signingKeyProvider.InvalidateCache();
        _tokenValidationParameters.IssuerSigningKeys = _signingKeyProvider.GetValidationKeys();

        _logger.LogWarning("JWT key rotation staged: new key stored as secondary. Promote after validation.");
    }
}
