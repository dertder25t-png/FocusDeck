using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Mobile.Services.Privacy;

internal sealed class MobilePrivacySettingsClient : IMobilePrivacySettingsClient
{
    private readonly IMobileCloudApiClient _cloudApiClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<MobilePrivacySettingsClient> _logger;

    public MobilePrivacySettingsClient(IMobileCloudApiClient cloudApiClient, ILogger<MobilePrivacySettingsClient> logger)
    {
        _cloudApiClient = cloudApiClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IReadOnlyList<PrivacySettingDto>> GetConsentAsync(CancellationToken cancellationToken = default)
    {
        var response = await _cloudApiClient.SendAsync(HttpMethod.Get, "/v1/privacy/consent", null, cancellationToken);
        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Unable to fetch privacy settings (status code: {StatusCode})", response?.StatusCode);
            return Array.Empty<PrivacySettingDto>();
        }

        return await response.Content.ReadFromJsonAsync<List<PrivacySettingDto>>(_jsonOptions, cancellationToken)
               ?? Array.Empty<PrivacySettingDto>();
    }

    public async Task<PrivacySettingDto?> UpdateConsentAsync(string contextType, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var payload = new PrivacySettingUpdateDto(contextType, isEnabled);
        var response = await _cloudApiClient.SendAsync(HttpMethod.Post, "/v1/privacy/consent", payload, cancellationToken);
        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to update privacy setting {ContextType}", contextType);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PrivacySettingDto>(_jsonOptions, cancellationToken);
    }
}
