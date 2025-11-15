using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Desktop.Services.Privacy;

internal sealed class SensorPrivacyGate : ISensorPrivacyGate
{
    private readonly IPrivacySettingsClient _client;
    private readonly ILogger<SensorPrivacyGate> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyDictionary<string, PrivacySettingDto> _cache = new Dictionary<string, PrivacySettingDto>(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastRefresh = DateTime.MinValue;

    public SensorPrivacyGate(IPrivacySettingsClient client, ILogger<SensorPrivacyGate> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string contextType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contextType))
        {
            return false;
        }

        if (DateTime.UtcNow - _lastRefresh > _cacheDuration)
        {
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
        }

        return _cache.TryGetValue(contextType, out var setting) && setting.IsEnabled;
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (DateTime.UtcNow - _lastRefresh <= _cacheDuration)
            {
                return;
            }

            var settings = await _client.GetConsentAsync(cancellationToken).ConfigureAwait(false);
            _cache = settings.ToDictionary(s => s.ContextType, StringComparer.OrdinalIgnoreCase);
            _lastRefresh = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh privacy cache");
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
