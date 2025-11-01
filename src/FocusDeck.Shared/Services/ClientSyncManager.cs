using System.Text.Json;
using System.Net.Http.Json;
using FocusDeck.Shared.Models.Sync;

namespace FocusDeck.Shared.Services
{
    /// <summary>
    /// Client-side sync manager for desktop applications
    /// Handles communication with the server sync API
    /// </summary>
    public class ClientSyncManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;
        private readonly string _deviceId;
        private readonly string _deviceName;
        private readonly DevicePlatform _platform;
        private long _lastKnownVersion = 0;
        private string? _jwtToken;

        public ClientSyncManager(string serverUrl, string deviceId, string deviceName, DevicePlatform platform)
        {
            _httpClient = new HttpClient();
            _serverUrl = serverUrl.TrimEnd('/');
            _deviceId = deviceId;
            _deviceName = deviceName;
            _platform = platform;
        }

        /// <summary>
        /// Set a JWT bearer token for authenticated requests
        /// </summary>
        public void SetJwtToken(string? token)
        {
            _jwtToken = token;
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        /// <summary>
        /// Register this device with the server
        /// </summary>
        public async Task<bool> RegisterDeviceAsync(string? userId = null)
        {
            try
            {
                var request = new
                {
                    DeviceId = _deviceId,
                    DeviceName = _deviceName,
                    Platform = _platform,
                    UserId = userId
                };

                var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/sync/register", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Failed to register device: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Push local changes to server
        /// </summary>
        public async Task<SyncResult> PushChangesAsync(List<SyncChange> changes, string? userId = null)
        {
            try
            {
                var request = new SyncPushRequest
                {
                    DeviceId = _deviceId,
                    LastKnownVersion = _lastKnownVersion,
                    Changes = changes
                };

                var url = $"{_serverUrl}/api/sync/push";
                if (userId != null)
                {
                    url += $"?userId={Uri.EscapeDataString(userId)}";
                }

                var response = await _httpClient.PostAsJsonAsync(url, request);
                // Try to read body regardless of status to surface conflicts (409)
                SyncResult? parsed = null;
                try { parsed = await response.Content.ReadFromJsonAsync<SyncResult>(); } catch { /* ignore parse errors */ }

                if (parsed != null)
                {
                    _lastKnownVersion = parsed.NewVersion > 0 ? parsed.NewVersion : _lastKnownVersion;
                    return parsed;
                }

                return new SyncResult
                {
                    Success = response.IsSuccessStatusCode,
                    ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Failed to push changes: {ex.Message}");
                return new SyncResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Pull changes from server
        /// </summary>
        public async Task<SyncPullResponse> PullChangesAsync(string? userId = null, SyncEntityType? entityType = null)
        {
            try
            {
                var url = $"{_serverUrl}/api/sync/pull?deviceId={Uri.EscapeDataString(_deviceId)}&lastKnownVersion={_lastKnownVersion}";
                if (userId != null)
                {
                    url += $"&userId={Uri.EscapeDataString(userId)}";
                }
                if (entityType.HasValue)
                {
                    url += $"&entityType={(int)entityType.Value}";
                }

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SyncPullResponse>();
                    if (result != null)
                    {
                        _lastKnownVersion = result.CurrentVersion;
                        return result;
                    }
                }

                return new SyncPullResponse
                {
                    CurrentVersion = _lastKnownVersion,
                    Changes = new List<SyncChange>()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Failed to pull changes: {ex.Message}");
                return new SyncPullResponse
                {
                    CurrentVersion = _lastKnownVersion,
                    Changes = new List<SyncChange>()
                };
            }
        }

        /// <summary>
        /// Perform full bidirectional sync
        /// </summary>
        public async Task<SyncResult> SyncAsync(List<SyncChange> localChanges, string? userId = null)
        {
            try
            {
                var request = new SyncPushRequest
                {
                    DeviceId = _deviceId,
                    LastKnownVersion = _lastKnownVersion,
                    Changes = localChanges
                };

                var url = $"{_serverUrl}/api/sync";
                if (userId != null)
                {
                    url += $"?userId={Uri.EscapeDataString(userId)}";
                }

                var response = await _httpClient.PostAsJsonAsync(url, request);
                // Parse response even on 409 to surface conflicts
                var result = await response.Content.ReadFromJsonAsync<SyncResult>();
                if (result != null)
                {
                    _lastKnownVersion = result.NewVersion > 0 ? result.NewVersion : _lastKnownVersion;
                    return result;
                }

                return new SyncResult { Success = response.IsSuccessStatusCode, ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Failed to sync: {ex.Message}");
                return new SyncResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get all registered devices
        /// </summary>
        public async Task<List<DeviceRegistration>?> GetDevicesAsync(string? userId = null)
        {
            try
            {
                var url = $"{_serverUrl}/api/sync/devices";
                if (userId != null)
                {
                    url += $"?userId={Uri.EscapeDataString(userId)}";
                }

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<DeviceRegistration>>();
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Failed to get devices: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get sync statistics
        /// </summary>
        public async Task<SyncStatistics?> GetStatisticsAsync(string? userId = null)
        {
            try
            {
                var url = $"{_serverUrl}/api/sync/statistics";
                if (userId != null)
                {
                    url += $"?userId={Uri.EscapeDataString(userId)}";
                }

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<SyncStatistics>();
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Failed to get statistics: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resolve a conflict for an entity on the server
        /// </summary>
        public async Task<bool> ResolveConflictAsync(string entityId, ConflictResolution resolution, string? userId = null)
        {
            try
            {
                var url = $"{_serverUrl}/api/sync/resolve";
                if (userId != null)
                {
                    url += $"?userId={Uri.EscapeDataString(userId)}";
                }

                var payload = new { EntityId = entityId, Resolution = resolution };
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Failed to resolve conflict: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate device ID based on MAC address and hostname
        /// </summary>
        public static string GenerateDeviceId()
        {
            try
            {
                var macAddress = System.Net.NetworkInformation.NetworkInterface
                    .GetAllNetworkInterfaces()
                    .FirstOrDefault()?
                    .GetPhysicalAddress()
                    .ToString() ?? "UNKNOWN";

                var hostname = Environment.MachineName;
                var combined = $"{macAddress}_{hostname}";

                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
                    return Convert.ToBase64String(hash)
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .Replace("=", "")
                        .Substring(0, 32);
                }
            }
            catch
            {
                return Environment.MachineName.ToLower().Replace(" ", "-");
            }
        }
    }

    /// <summary>
    /// Helper for tracking local changes before sync
    /// </summary>
    public class LocalChangeTracker
    {
        private readonly List<SyncChange> _pendingChanges = new();
        private readonly object _lock = new();

        /// <summary>
        /// Track a change to an entity
        /// </summary>
        public void TrackChange(SyncEntityType entityType, string entityId, SyncOperation operation, object entityData)
        {
            lock (_lock)
            {
                var change = new SyncChange
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    Operation = operation,
                    DataJson = JsonSerializer.Serialize(entityData),
                    ChangedAt = DateTime.UtcNow
                };

                // Remove any previous change for the same entity (only keep latest)
                _pendingChanges.RemoveAll(c => c.EntityId == entityId && c.EntityType == entityType);
                _pendingChanges.Add(change);
            }
        }

        /// <summary>
        /// Get all pending changes and clear the list
        /// </summary>
        public List<SyncChange> GetPendingChanges()
        {
            lock (_lock)
            {
                var changes = new List<SyncChange>(_pendingChanges);
                _pendingChanges.Clear();
                return changes;
            }
        }

        /// <summary>
        /// Get count of pending changes
        /// </summary>
        public int GetPendingCount()
        {
            lock (_lock)
            {
                return _pendingChanges.Count;
            }
        }
    }
}
