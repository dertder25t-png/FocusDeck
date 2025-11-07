using System.Net.Http.Json;
using System.Net.Http.Headers;
using FocusDeck.Shared.Models;
using System.Diagnostics;
using Microsoft.Maui.Storage;
using FocusDeck.Mobile.Services.Auth;

namespace FocusDeck.Mobile.Services;

public class FocusDeckServerSyncService : ICloudSyncService
{
    private readonly HttpClient _httpClient;
    private readonly MobileTokenStore _tokenStore;
    private string _serverUrl;

    public string ServerUrl
    {
        get => _serverUrl;
        set => _serverUrl = value.TrimEnd('/');
    }

    public FocusDeckServerSyncService(MobileTokenStore tokenStore, string serverUrl = "")
    {
        _tokenStore = tokenStore;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        ServerUrl = serverUrl;
    }

    public Task<string?> AuthenticateAsync(string email, string password) => Task.FromResult<string?>(null);

    public async Task<bool> SyncSessionAsync(StudySession session, string? authToken = null)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, "/api/study-sessions", authToken);
        if (request == null) return false;

        request.Content = JsonContent.Create(session);
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<StudySession>> GetRemoteSessionsAsync(string? authToken = null)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, "/api/study-sessions", authToken);
        if (request == null) return new List<StudySession>();

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<StudySession>();

        return (await response.Content.ReadFromJsonAsync<List<StudySession>>()) ?? new List<StudySession>();
    }

    public async Task<List<StudySession>> GetRemoteSessionsAsync(DateTime startDate, DateTime endDate, string? authToken = null)
    {
        var query = $"/api/study-sessions?from={startDate:O}&to={endDate:O}";
        var request = await CreateRequestAsync(HttpMethod.Get, query, authToken);
        if (request == null) return new List<StudySession>();

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<StudySession>();

        return (await response.Content.ReadFromJsonAsync<List<StudySession>>()) ?? new List<StudySession>();
    }

    public async Task<bool> CheckServerHealthAsync()
    {
        var endpoint = BuildUri("/health");
        if (endpoint == null) return false;

        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FocusDeckServer] Health check failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteRemoteSessionAsync(Guid sessionId, string? authToken = null)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete, $"/api/study-sessions/{sessionId}", authToken);
        if (request == null) return false;

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<SyncStatus> GetSyncStatusAsync(string? authToken = null)
    {
        var rangeEnd = DateTime.UtcNow;
        var rangeStart = rangeEnd.AddDays(-14);
        var request = await CreateRequestAsync(HttpMethod.Get, $"/api/study-sessions/summary?from={rangeStart:O}&to={rangeEnd:O}", authToken);
        if (request == null) return new SyncStatus { IsHealthy = false };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new SyncStatus { IsHealthy = false };

        var summary = await response.Content.ReadFromJsonAsync<SyncStatus>();
        if (summary == null) return new SyncStatus { IsHealthy = true };
        summary.IsHealthy = true;
        return summary;
    }

    private async Task<HttpRequestMessage?> CreateRequestAsync(HttpMethod method, string path, string? authToken = null)
    {
        var token = await ResolveAccessTokenAsync(authToken);
        if (string.IsNullOrWhiteSpace(token)) return null;

        var endpoint = BuildUri(path);
        if (endpoint == null) return null;

        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<string?> ResolveAccessTokenAsync(string? explicitToken)
    {
        if (!string.IsNullOrWhiteSpace(explicitToken))
        {
            return explicitToken;
        }

        var stored = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        var legacyToken = Preferences.Get("auth_token", string.Empty);
        return string.IsNullOrWhiteSpace(legacyToken) ? null : legacyToken;
    }

    private Uri? BuildUri(string path)
    {
        var effectiveUrl = Preferences.Get("cloud_server_url", ServerUrl);
        if (string.IsNullOrWhiteSpace(effectiveUrl)) return null;
        var baseUri = new Uri(effectiveUrl.TrimEnd('/') + "/");
        return new Uri(baseUri, path.TrimStart('/'));
    }
}
