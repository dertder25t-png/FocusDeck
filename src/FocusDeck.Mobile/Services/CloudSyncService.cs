using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using FocusDeck.Shared.Models;
using System.Diagnostics;

namespace FocusDeck.Mobile.Services;

/// <summary>
/// Interface for cloud synchronization of study sessions.
/// Abstracts backend implementation (could be PocketBase, ASP.NET Core, etc.)
/// </summary>
public interface ICloudSyncService
{
    /// <summary>
    /// Gets or sets the server URL (e.g., https://server.com)
    /// </summary>
    string ServerUrl { get; set; }

    /// <summary>
    /// Authenticates user and returns auth token
    /// </summary>
    Task<string?> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Uploads a completed study session to the cloud backend
    /// </summary>
    Task<bool> SyncSessionAsync(StudySession session, string authToken);

    /// <summary>
    /// Downloads all study sessions from cloud backend
    /// </summary>
    Task<List<StudySession>> GetRemoteSessionsAsync(string authToken);

    /// <summary>
    /// Downloads sessions within a date range from cloud
    /// </summary>
    Task<List<StudySession>> GetRemoteSessionsAsync(DateTime startDate, DateTime endDate, string authToken);

    /// <summary>
    /// Checks if server is reachable and healthy
    /// </summary>
    Task<bool> CheckServerHealthAsync();

    /// <summary>
    /// Deletes a session from cloud backend
    /// </summary>
    Task<bool> DeleteRemoteSessionAsync(Guid sessionId, string authToken);

    /// <summary>
    /// Gets sync status information
    /// </summary>
    Task<SyncStatus> GetSyncStatusAsync(string authToken);
}

/// <summary>
/// Cloud synchronization status information
/// </summary>
public class SyncStatus
{
    [JsonPropertyName("serverVersion")]
    public string ServerVersion { get; set; } = string.Empty;

    [JsonPropertyName("lastSyncTime")]
    public DateTime? LastSyncTime { get; set; }

    [JsonPropertyName("sessionCount")]
    public int SessionCount { get; set; }

    [JsonPropertyName("isHealthy")]
    public bool IsHealthy { get; set; }
}

/// <summary>
/// PocketBase implementation of cloud sync service
/// Handles authentication, upload, download, and synchronization with PocketBase backend
/// </summary>
public class PocketBaseCloudSyncService : ICloudSyncService
{
    private readonly HttpClient _httpClient;
    private string _serverUrl = string.Empty;

    public string ServerUrl
    {
        get => _serverUrl;
        set => _serverUrl = value.TrimEnd('/');
    }

    public PocketBaseCloudSyncService(string serverUrl = "")
    {
        _httpClient = new HttpClient();
        ServerUrl = serverUrl;

        // Configure HTTP client
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FocusDeck-Mobile/1.0");
    }

    /// <summary>
    /// Authenticates with PocketBase and returns auth token
    /// </summary>
    public async Task<string?> AuthenticateAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(ServerUrl))
            {
                Debug.WriteLine("[PocketBase] Error: ServerUrl not configured");
                return null;
            }

            var payload = new
            {
                identity = email,
                password = password
            };

            var content = JsonContent.Create(payload);
            var response = await _httpClient.PostAsync(
                $"{ServerUrl}/api/collections/users/auth-with-password",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[PocketBase] Authentication failed: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<PocketBaseAuthResponse>(json);

            if (authResponse?.Token == null)
            {
                Debug.WriteLine("[PocketBase] No token in response");
                return null;
            }

            Debug.WriteLine($"[PocketBase] Authentication successful for {email}");
            return authResponse.Token;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PocketBase] Authentication error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Uploads a study session to PocketBase
    /// </summary>
    public async Task<bool> SyncSessionAsync(StudySession session, string authToken)
    {
        try
        {
            if (string.IsNullOrEmpty(authToken))
            {
                Debug.WriteLine("[PocketBase] Error: No auth token provided");
                return false;
            }

            // Prepare session data for upload
            var sessionData = new
            {
                sessionId = session.SessionId.ToString(),
                startTime = session.StartTime.ToString("o"),
                endTime = session.EndTime?.ToString("o"),
                durationMinutes = session.DurationMinutes,
                sessionNotes = session.SessionNotes,
                status = session.Status.ToString(),
                focusRate = session.FocusRate,
                breaksCount = session.BreaksCount,
                breakDurationMinutes = session.BreakDurationMinutes,
                category = session.Category
            };

            var content = JsonContent.Create(sessionData);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.PostAsync(
                $"{ServerUrl}/api/collections/StudySessions/records",
                content
            );

            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[PocketBase] Session synced: {session.SessionId}");
                return true;
            }
            else
            {
                Debug.WriteLine($"[PocketBase] Sync failed: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PocketBase] Sync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads all study sessions from PocketBase
    /// </summary>
    public async Task<List<StudySession>> GetRemoteSessionsAsync(string authToken)
    {
        try
        {
            if (string.IsNullOrEmpty(authToken))
                return new();

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.GetAsync(
                $"{ServerUrl}/api/collections/StudySessions/records?perPage=500"
            );

            if (!response.IsSuccessStatusCode)
                return new();

            var json = await response.Content.ReadAsStringAsync();
            var pocketbaseResponse = JsonSerializer.Deserialize<PocketBaseListResponse>(json);

            var sessions = pocketbaseResponse?.Items?.Select(item => new StudySession
            {
                SessionId = Guid.Parse(item.SessionId),
                StartTime = DateTime.Parse(item.StartTime),
                EndTime = item.EndTime == null ? null : DateTime.Parse(item.EndTime),
                DurationMinutes = item.DurationMinutes,
                SessionNotes = item.SessionNotes,
                Status = Enum.Parse<SessionStatus>(item.Status),
                FocusRate = item.FocusRate,
                BreaksCount = item.BreaksCount,
                BreakDurationMinutes = item.BreakDurationMinutes,
                Category = item.Category,
                CreatedAt = DateTime.Parse(item.CreatedAt),
                UpdatedAt = DateTime.Parse(item.UpdatedAt)
            }).ToList() ?? new();

            Debug.WriteLine($"[PocketBase] Downloaded {sessions.Count} sessions");
            return sessions;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PocketBase] Download error: {ex.Message}");
            return new();
        }
    }

    /// <summary>
    /// Downloads study sessions within a date range from PocketBase
    /// </summary>
    public async Task<List<StudySession>> GetRemoteSessionsAsync(DateTime startDate, DateTime endDate, string authToken)
    {
        try
        {
            if (string.IsNullOrEmpty(authToken))
                return new();

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            // PocketBase filter syntax for date range
            var filter = $"startTime >= '{startDate:o}' && startTime <= '{endDate:o}'";
            var encodedFilter = Uri.EscapeDataString(filter);

            var response = await _httpClient.GetAsync(
                $"{ServerUrl}/api/collections/StudySessions/records?filter={encodedFilter}&perPage=500"
            );

            if (!response.IsSuccessStatusCode)
                return new();

            var json = await response.Content.ReadAsStringAsync();
            var pocketbaseResponse = JsonSerializer.Deserialize<PocketBaseListResponse>(json);

            var sessions = pocketbaseResponse?.Items?.Select(item => new StudySession
            {
                SessionId = Guid.Parse(item.SessionId),
                StartTime = DateTime.Parse(item.StartTime),
                EndTime = item.EndTime == null ? null : DateTime.Parse(item.EndTime),
                DurationMinutes = item.DurationMinutes,
                SessionNotes = item.SessionNotes,
                Status = Enum.Parse<SessionStatus>(item.Status),
                FocusRate = item.FocusRate,
                BreaksCount = item.BreaksCount,
                BreakDurationMinutes = item.BreakDurationMinutes,
                Category = item.Category,
                CreatedAt = DateTime.Parse(item.CreatedAt),
                UpdatedAt = DateTime.Parse(item.UpdatedAt)
            }).ToList() ?? new();

            Debug.WriteLine($"[PocketBase] Downloaded {sessions.Count} sessions for date range");
            return sessions;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PocketBase] Range download error: {ex.Message}");
            return new();
        }
    }

    /// <summary>
    /// Checks if the PocketBase server is healthy and reachable
    /// </summary>
    public async Task<bool> CheckServerHealthAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ServerUrl))
            {
                Debug.WriteLine("[PocketBase] Error: ServerUrl not configured");
                return false;
            }

            var response = await _httpClient.GetAsync($"{ServerUrl}/api/health");
            var isHealthy = response.IsSuccessStatusCode;

            if (isHealthy)
                Debug.WriteLine("[PocketBase] Server health check: OK");
            else
                Debug.WriteLine($"[PocketBase] Server health check failed: {response.StatusCode}");

            return isHealthy;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PocketBase] Health check error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes a session from PocketBase
    /// </summary>
    public async Task<bool> DeleteRemoteSessionAsync(Guid sessionId, string authToken)
    {
        try
        {
            if (string.IsNullOrEmpty(authToken))
                return false;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.DeleteAsync(
                $"{ServerUrl}/api/collections/StudySessions/records/{sessionId}"
            );

            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[PocketBase] Session deleted: {sessionId}");
                return true;
            }
            else
            {
                Debug.WriteLine($"[PocketBase] Delete failed: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PocketBase] Delete error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets sync status from PocketBase
    /// </summary>
    public async Task<SyncStatus> GetSyncStatusAsync(string authToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.GetAsync($"{ServerUrl}/api/health");
            var isHealthy = response.IsSuccessStatusCode;

            var sessionCount = 0;
            if (isHealthy && !string.IsNullOrEmpty(authToken))
            {
                var sessions = await GetRemoteSessionsAsync(authToken);
                sessionCount = sessions.Count;
            }

            return new SyncStatus
            {
                IsHealthy = isHealthy,
                LastSyncTime = DateTime.UtcNow,
                SessionCount = sessionCount,
                ServerVersion = "PocketBase"
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PocketBase] Status check error: {ex.Message}");
            return new SyncStatus { IsHealthy = false };
        }
    }
}

/// <summary>
/// PocketBase API response models (internal)
/// </summary>
internal class PocketBaseAuthResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("record")]
    public Dictionary<string, object>? Record { get; set; }
}

internal class PocketBaseListResponse
{
    [JsonPropertyName("items")]
    public List<PocketBaseSessionItem>? Items { get; set; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }
}

internal class PocketBaseSessionItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("sessionNotes")]
    public string? SessionNotes { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Active";

    [JsonPropertyName("focusRate")]
    public int? FocusRate { get; set; }

    [JsonPropertyName("breaksCount")]
    public int BreaksCount { get; set; }

    [JsonPropertyName("breakDurationMinutes")]
    public int BreakDurationMinutes { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("created")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("updated")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
