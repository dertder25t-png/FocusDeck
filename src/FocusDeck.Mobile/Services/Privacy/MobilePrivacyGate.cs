using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Mobile.Services.Privacy;

internal sealed class MobilePrivacyGate : IMobilePrivacyGate
{
    private readonly IMobilePrivacySettingsClient _settingsClient;
    private readonly ILogger<MobilePrivacyGate> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(60);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyDictionary<string, PrivacySettingDto> _cache = new Dictionary<string, PrivacySettingDto>(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastRefresh = DateTime.MinValue;

    public MobilePrivacyGate(IMobilePrivacySettingsClient settingsClient, ILogger<MobilePrivacyGate> logger)
    {
        _settingsClient = settingsClient;
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

            var settings = await _settingsClient.GetConsentAsync(cancellationToken).ConfigureAwait(false);
            _cache = settings.ToDictionary(setting => setting.ContextType, StringComparer.OrdinalIgnoreCase);
            _lastRefresh = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to refresh mobile privacy cache");
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
