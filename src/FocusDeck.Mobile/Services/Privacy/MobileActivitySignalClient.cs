using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Mobile.Services.Privacy;

internal sealed class MobileActivitySignalClient : IMobileActivitySignalClient
{
    private readonly IMobileCloudApiClient _cloudApiClient;
    private readonly ILogger<MobileActivitySignalClient> _logger;

    public MobileActivitySignalClient(IMobileCloudApiClient cloudApiClient, ILogger<MobileActivitySignalClient> logger)
    {
        _cloudApiClient = cloudApiClient;
        _logger = logger;
    }

    public async Task SendActivitySignalAsync(ActivitySignalDto signal, CancellationToken cancellationToken = default)
    {
        var response = await _cloudApiClient.SendAsync(HttpMethod.Post, "/v1/activity/signals", signal, cancellationToken);
        if (response == null)
        {
            _logger.LogWarning("Activity signal could not be delivered (no response)");
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Activity signal rejected ({StatusCode})", response.StatusCode);
        }
    }
}
