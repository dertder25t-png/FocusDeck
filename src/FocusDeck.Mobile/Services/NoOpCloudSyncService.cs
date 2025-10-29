namespace FocusDeck.Mobile.Services;

/// <summary>
/// No-operation implementation of ICloudSyncService.
/// Used when cloud sync is not configured or disabled.
/// All methods are no-ops and return appropriate empty/default values.
/// </summary>
public class NoOpCloudSyncService : ICloudSyncService
{
    private string _serverUrl = string.Empty;

    /// <summary>Server URL (always empty for no-op)</summary>
    public string ServerUrl
    {
        get => _serverUrl;
        set => _serverUrl = value;
    }

    /// <summary>No-op: Always returns null (not authenticated)</summary>
    public async Task<string?> AuthenticateAsync(string email, string password)
    {
        await Task.Delay(10); // Simulate minimal work
        return null;
    }

    /// <summary>No-op: Always succeeds silently</summary>
    public async Task<bool> SyncSessionAsync(FocusDeck.Shared.Models.StudySession session, string authToken)
    {
        await Task.Delay(10);
        return true; // Pretend success to not block local app
    }

    /// <summary>No-op: Returns empty list</summary>
    public async Task<List<FocusDeck.Shared.Models.StudySession>> GetRemoteSessionsAsync(string authToken)
    {
        await Task.Delay(10);
        return new List<FocusDeck.Shared.Models.StudySession>();
    }

    /// <summary>No-op: Returns empty list for date range</summary>
    public async Task<List<FocusDeck.Shared.Models.StudySession>> GetRemoteSessionsAsync(
        DateTime startDate, 
        DateTime endDate, 
        string authToken)
    {
        await Task.Delay(10);
        return new List<FocusDeck.Shared.Models.StudySession>();
    }

    /// <summary>No-op: Always returns false</summary>
    public async Task<bool> CheckServerHealthAsync()
    {
        await Task.Delay(10);
        return false;
    }

    /// <summary>No-op: Always succeeds silently</summary>
    public async Task<bool> DeleteRemoteSessionAsync(Guid sessionId, string authToken)
    {
        await Task.Delay(10);
        return true;
    }

    /// <summary>No-op: Returns empty/offline status</summary>
    public async Task<SyncStatus> GetSyncStatusAsync(string authToken)
    {
        await Task.Delay(10);
        return new SyncStatus
        {
            IsHealthy = false,
            LastSyncTime = null,
            SessionCount = 0,
            ServerVersion = "N/A"
        };
    }
}
