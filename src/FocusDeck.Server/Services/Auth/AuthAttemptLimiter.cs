using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace FocusDeck.Server.Services.Auth;

public interface IAuthAttemptLimiter
{
    Task<bool> IsBlockedAsync(string? userId, string? remoteIp, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(string? userId, string? remoteIp, CancellationToken cancellationToken = default);
    Task ResetAsync(string? userId, string? remoteIp, CancellationToken cancellationToken = default);
}

public class AuthAttemptLimiter : IAuthAttemptLimiter
{
    private const string FailureKeyPrefix = "auth:fail:";
    private const string BlockKeyPrefix = "auth:block:";
    private readonly TimeSpan _failureWindow = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _blockDuration = TimeSpan.FromMinutes(15);
    private readonly int _failureThreshold = 5;

    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _memoryCache;

    public AuthAttemptLimiter(IConnectionMultiplexer? redis = null, IMemoryCache? memoryCache = null)
    {
        _redis = redis;
        _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
    }

    public async Task<bool> IsBlockedAsync(string? userId, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var key = ComposeKey(BlockKeyPrefix, userId, remoteIp);

        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyExistsAsync(key);
            }
            catch
            {
                // Fall back to memory cache
            }
        }

        return _memoryCache.TryGetValue(key, out _);
    }

    public async Task RecordFailureAsync(string? userId, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var failureKey = ComposeKey(FailureKeyPrefix, userId, remoteIp);
        var blockKey = ComposeKey(BlockKeyPrefix, userId, remoteIp);

        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var failures = await db.StringIncrementAsync(failureKey);
                if (failures == 1)
                {
                    await db.KeyExpireAsync(failureKey, _failureWindow);
                }

                if (failures >= _failureThreshold)
                {
                    await db.StringSetAsync(blockKey, "1", _blockDuration);
                    await db.KeyDeleteAsync(failureKey);
                }
                return;
            }
            catch
            {
                // Fall back to memory
            }
        }

        var counter = _memoryCache.GetOrCreate(failureKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _failureWindow;
            return 0;
        });

        counter++;
        _memoryCache.Set(failureKey, counter, _failureWindow);

        if (counter >= _failureThreshold)
        {
            _memoryCache.Set(blockKey, true, _blockDuration);
            _memoryCache.Remove(failureKey);
        }
    }

    public async Task ResetAsync(string? userId, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var failureKey = ComposeKey(FailureKeyPrefix, userId, remoteIp);
        var blockKey = ComposeKey(BlockKeyPrefix, userId, remoteIp);

        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(failureKey);
                await db.KeyDeleteAsync(blockKey);
                return;
            }
            catch
            {
                // fall back
            }
        }

        _memoryCache.Remove(failureKey);
        _memoryCache.Remove(blockKey);
    }

    private static string ComposeKey(string prefix, string? userId, string? remoteIp)
    {
        var identity = string.IsNullOrWhiteSpace(userId) ? remoteIp ?? "unknown" : userId.Trim().ToLowerInvariant();
        return prefix + identity;
    }
}
