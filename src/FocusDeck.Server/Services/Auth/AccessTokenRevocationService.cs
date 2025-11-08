using System.IdentityModel.Tokens.Jwt;
using FocusDeck.Persistence;
using FocusDeck.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using FocusDeck.Shared.SignalR.Notifications;

namespace FocusDeck.Server.Services.Auth;

public interface IAccessTokenRevocationService
{
    Task<bool> IsRevokedAsync(string jti, CancellationToken ct);
    Task RevokeAsync(string jti, string userId, DateTime expiresUtc, CancellationToken ct, string? reason = null, string? deviceId = null);
}

public class AccessTokenRevocationService : IAccessTokenRevocationService
{
    private const string RedisRevocationChannel = "auth:revocations";
    private readonly AutomationDbContext _db;
    private readonly ILogger<AccessTokenRevocationService> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hub;
    private readonly IConnectionMultiplexer? _redis;

    public AccessTokenRevocationService(
        AutomationDbContext db,
        ILogger<AccessTokenRevocationService> logger,
        IHubContext<NotificationsHub, INotificationClient> hub,
        IConnectionMultiplexer? redis = null)
    {
        _db = db;
        _logger = logger;
        _hub = hub;
        _redis = redis;
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken ct)
    {
        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var redisValue = await db.StringGetAsync(GetRedisKey(jti));
                if (redisValue.HasValue)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Redis IsRevokedAsync fallback to DB");
            }
        }

        return await _db.RevokedAccessTokens.AsNoTracking().AnyAsync(r => r.Jti == jti, ct);
    }

    public async Task RevokeAsync(string jti, string userId, DateTime expiresUtc, CancellationToken ct, string? reason = null, string? deviceId = null)
    {
        try
        {
            if (await IsRevokedAsync(jti, ct)) return;
            _db.RevokedAccessTokens.Add(new FocusDeck.Domain.Entities.Auth.RevokedAccessToken
            {
                Jti = jti,
                UserId = userId,
                ExpiresUtc = expiresUtc,
                RevokedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(reason))
            {
                await _hub.Clients.Group($"user:{userId}").ForceLogout(new ForceLogoutMessage(reason!, deviceId));
            }

            if (_redis != null)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var ttl = expiresUtc - DateTime.UtcNow;
                    if (ttl < TimeSpan.Zero) ttl = TimeSpan.FromMinutes(5);
                    await db.StringSetAsync(GetRedisKey(jti), "1", ttl);
                    var channel = new RedisChannel(RedisRevocationChannel, RedisChannel.PatternMode.Literal);
                    await _redis.GetSubscriber().PublishAsync(channel, jti);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to write revocation to Redis");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke token {Jti}", jti);
        }
    }

    private static string GetRedisKey(string jti) => $"auth:revoked:{jti}";
}
