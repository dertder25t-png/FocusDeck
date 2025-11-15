using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Mobile.Services.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace FocusDeck.Mobile.Services.Privacy;

internal sealed class MobileCloudApiClient : IMobileCloudApiClient
{
    private readonly MobileTokenStore _tokenStore;
    private readonly FocusDeckServerSyncService _syncService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MobileCloudApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MobileCloudApiClient(
        MobileTokenStore tokenStore,
        FocusDeckServerSyncService syncService,
        ILogger<MobileCloudApiClient> logger)
    {
        _tokenStore = tokenStore;
        _syncService = syncService;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<HttpResponseMessage?> SendAsync(HttpMethod method, string path, object? content = null, CancellationToken cancellationToken = default)
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Cloud API request skipped because access token is missing");
            return null;
        }

        var endpoint = BuildUri(path);
        if (endpoint == null)
        {
            _logger.LogWarning("Cloud API request skipped because server URL is unavailable");
            return null;
        }

        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (content != null)
        {
            request.Content = JsonContent.Create(content, options: _jsonOptions);
        }

        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cloud API call to {Path} failed", path);
            return null;
        }
    }

    private Uri? BuildUri(string path)
    {
        var effectiveUrl = Preferences.Get("cloud_server_url", _syncService.ServerUrl);
        if (string.IsNullOrWhiteSpace(effectiveUrl))
        {
            return null;
        }

        var baseUri = new Uri(effectiveUrl.TrimEnd('/') + "/");
        return new Uri(baseUri, path.TrimStart('/'));
    }
}
