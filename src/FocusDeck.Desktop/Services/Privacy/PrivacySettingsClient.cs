using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Desktop.Services.Privacy;

internal sealed class PrivacySettingsClient : IPrivacySettingsClient
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<PrivacySettingsClient> _logger;

    public PrivacySettingsClient(IApiClient apiClient, ILogger<PrivacySettingsClient> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PrivacySettingDto>> GetConsentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = await _apiClient.GetAsync<List<PrivacySettingDto>>("/v1/privacy/consent", cancellationToken);
            return payload ?? Array.Empty<PrivacySettingDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load privacy settings");
            return Array.Empty<PrivacySettingDto>();
        }
    }

    public async Task<PrivacySettingDto?> UpdateConsentAsync(string contextType, bool isEnabled, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.PostAsync<PrivacySettingDto>(
                "/v1/privacy/consent",
                new PrivacySettingUpdateDto(contextType, isEnabled),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update privacy setting {ContextType}", contextType);
            return null;
        }
    }
}
