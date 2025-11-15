using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Services.Auth;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.HealthChecks;

public sealed class JwtKeyHealthCheck : IHealthCheck
{
    private readonly IJwtSigningKeyProvider _keyProvider;
    private readonly ILogger<JwtKeyHealthCheck> _logger;

    public JwtKeyHealthCheck(IJwtSigningKeyProvider keyProvider, ILogger<JwtKeyHealthCheck> logger)
    {
        _keyProvider = keyProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var keys = await Task.Run(() => _keyProvider.GetValidationKeys()).WaitAsync(TimeSpan.FromSeconds(3), ct);
            var keyCount = keys.Count();

            if (keyCount == 0)
            {
                return HealthCheckResult.Unhealthy("No JWT validation keys loaded");
            }

            return HealthCheckResult.Healthy($"Loaded {keyCount} JWT keys");
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("JWT key health check timed out");
            return HealthCheckResult.Unhealthy("JWT key loading timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT key health check failed");
            return HealthCheckResult.Unhealthy("JWT key loading failed", ex);
        }
    }
}
