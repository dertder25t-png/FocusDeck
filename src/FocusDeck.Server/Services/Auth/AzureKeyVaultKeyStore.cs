using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Auth;

public sealed class AzureKeyVaultKeyStore : ICryptographicKeyStore
{
    private const string CachePrimaryKey = "jwt:primary-key";
    private const string CacheSecondaryKey = "jwt:secondary-key";
    private readonly SecretClient _secretClient;
    private readonly ILogger<AzureKeyVaultKeyStore> _logger;
    private readonly IMemoryCache _cache;

    public AzureKeyVaultKeyStore(
        SecretClient secretClient,
        ILogger<AzureKeyVaultKeyStore> logger,
        IMemoryCache cache)
    {
        _secretClient = secretClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<string> GetPrimaryKeyAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(CachePrimaryKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            try
            {
                var secret = await _secretClient.GetSecretAsync("jwt-primary-key", cancellationToken: ct);
                return secret.Value.Value ?? throw new InvalidOperationException("Primary JWT key is missing in Key Vault");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch primary JWT key from Key Vault");
                throw new InvalidOperationException("JWT signing key unavailable", ex);
            }
        }) ?? throw new InvalidOperationException("Primary JWT key cannot be null");
    }

    public async Task<string?> GetSecondaryKeyAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(CacheSecondaryKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            try
            {
                var secret = await _secretClient.GetSecretAsync("jwt-secondary-key", cancellationToken: ct);
                return string.IsNullOrWhiteSpace(secret.Value.Value) ? null : secret.Value.Value;
            }
            catch (RequestFailedException rfEx) when (rfEx.Status == 404)
            {
                _logger.LogWarning("Secondary JWT key not found in Key Vault: {Message}", rfEx.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch secondary JWT key from Key Vault");
                return null;
            }
        });
    }

    public async Task RotateKeyAsync(string newPrimaryKey, CancellationToken ct = default)
    {
        await _secretClient.SetSecretAsync(new KeyVaultSecret("jwt-secondary-key", newPrimaryKey), ct);
        _cache.Remove(CacheSecondaryKey);
        _logger.LogInformation("JWT key rotation initiated. New key stored as secondary in Key Vault.");
    }
}
