using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace FocusDeck.Server.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connection;

    public RedisHealthCheck(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connection.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis ping failed", ex);
        }
    }
}

