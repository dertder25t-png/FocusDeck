using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FocusDeck.Server.Services.Auth;

public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private const string ValidationKeyCacheKey = "jwt:validation-keys";
    private static readonly TimeSpan ValidationCacheDuration = TimeSpan.FromMinutes(5);
    // Extended timeout for test environments where startup can be slower
    private static readonly TimeSpan KeyFetchTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan SecondaryKeyWait = TimeSpan.FromSeconds(5);

    private readonly ICryptographicKeyStore _keyStore;
    private readonly JwtSettings _settings;
    private readonly ILogger<JwtSigningKeyProvider> _logger;
    private readonly IMemoryCache _cache;
    private volatile IReadOnlyCollection<string> _versions = Array.Empty<string>();

    public JwtSigningKeyProvider(
        ICryptographicKeyStore keyStore,
        IOptions<JwtSettings> settings,
        IMemoryCache cache,
        ILogger<JwtSigningKeyProvider> logger)
    {
        _keyStore = keyStore;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
    }

    public IEnumerable<SecurityKey> GetValidationKeys()
    {
        var keys = _cache.GetOrCreate<IReadOnlyList<SecurityKey>>(ValidationKeyCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ValidationCacheDuration;
            _logger.LogInformation("Cache miss for JWT validation keys, loading from key store...");
            var (loadedKeys, loadedVersions) = LoadValidationKeys();
            _versions = loadedVersions;
            _logger.LogInformation("Loaded and cached {KeyCount} JWT validation keys", loadedKeys.Count);
            return loadedKeys;
        });

        _logger.LogDebug("GetValidationKeys returning {KeyCount} cached keys", keys?.Count ?? 0);
        return keys ?? Array.Empty<SecurityKey>();
    }

    public async Task<string> GetPrimaryKeyAsync(CancellationToken ct = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linkedCts.CancelAfter(KeyFetchTimeout);
        return await _keyStore.GetPrimaryKeyAsync(linkedCts.Token);
    }

    public bool ContainsVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        return _versions.Contains(version, StringComparer.OrdinalIgnoreCase);
    }

    public void InvalidateCache() => _cache.Remove(ValidationKeyCacheKey);

    private (IReadOnlyList<SecurityKey> Keys, IReadOnlyCollection<string> Versions) LoadValidationKeys()
    {
        var keys = new List<SecurityKey>();
        var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var primaryKey = RunWithTimeout(() => _keyStore.GetPrimaryKeyAsync(), KeyFetchTimeout);
        AddKey(primaryKey, keys, versions);

        try
        {
            var secondaryTask = Task.Run(() => _keyStore.GetSecondaryKeyAsync());
            if (secondaryTask.Wait(SecondaryKeyWait) && secondaryTask.Result is string secondaryKey && !string.IsNullOrWhiteSpace(secondaryKey))
            {
                AddKey(secondaryKey, keys, versions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Secondary JWT key load failed (ignored)");
        }

        _logger.LogInformation("Loaded {KeyCount} JWT validation keys", keys.Count);
        return (keys.AsReadOnly(), versions.ToList().AsReadOnly());
    }

    private static string RunWithTimeout(Func<Task<string>> factory, TimeSpan timeout)
    {
        var task = Task.Run(factory);
        if (!task.Wait(timeout))
        {
            throw new TimeoutException("Timed out while loading the JWT signing key.");
        }

        return task.GetAwaiter().GetResult();
    }

    private static void AddKey(string key, List<SecurityKey> keys, HashSet<string> versions)
    {
        var version = KeyRotationHelper.GetKeyVersion(key);
        if (versions.Contains(version))
        {
            return;
        }

        keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)));
        versions.Add(version);
    }
}
