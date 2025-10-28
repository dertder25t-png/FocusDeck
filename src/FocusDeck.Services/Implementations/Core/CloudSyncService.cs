namespace FocusDeck.Services.Implementations.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Services.Abstractions;

/// <summary>
/// Main cloud sync coordinator that manages syncing across multiple providers
/// Handles encryption, conflict resolution, and multi-device synchronization
/// </summary>
public class CloudSyncService : ICloudSyncService
{
    private ICloudProvider? _activeProvider;
    private IEncryptionService? _encryptionService;
    private IDeviceRegistryService? _deviceRegistry;
    
    private SyncStatus _currentStatus = SyncStatus.NotInitialized;
    private DateTime? _lastSyncTime;
    private Timer? _autoSyncTimer;
    private readonly object _syncLock = new();
    private readonly Queue<string> _syncQueue = new();
    
    private const string BACKUP_DIR = "/FocusDeck/backup/";
    private const string METADATA_DIR = "/FocusDeck/sync_metadata/";
    private const string VERSION_HISTORY_DIR = "/FocusDeck/version_history/";
    
    // Local data files to sync
    private readonly string[] _syncFiles = new[]
    {
        "study_sessions.json",
        "todos.json",
        "workspaces.json",
        "analytics.json",
        "settings.json"
    };

    public ICloudProvider? ActiveProvider => _activeProvider;

    public SyncStatus CurrentStatus
    {
        get => _currentStatus;
        private set
        {
            if (_currentStatus != value)
            {
                var oldStatus = _currentStatus;
                _currentStatus = value;
                SyncStatusChanged?.Invoke(this, new SyncStatusChangedEventArgs
                {
                    OldStatus = oldStatus,
                    NewStatus = value,
                    Timestamp = DateTime.Now
                });
            }
        }
    }

    public DateTime? LastSyncTime => _lastSyncTime;

    public event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;
    public event EventHandler<SyncConflictEventArgs>? SyncConflictDetected;

    public CloudSyncService(IEncryptionService encryptionService, IDeviceRegistryService? deviceRegistry = null)
    {
        _encryptionService = encryptionService;
        _deviceRegistry = deviceRegistry;
    }

    /// <summary>
    /// Initialize cloud sync with a specific provider
    /// </summary>
    public async Task<bool> InitializeSyncAsync(ICloudProvider provider)
    {
        lock (_syncLock)
        {
            if (CurrentStatus == SyncStatus.Syncing)
            {
                return false;
            }
        }

        try
        {
            CurrentStatus = SyncStatus.Idle;

            // Verify authentication
            if (!provider.IsAuthenticated)
            {
                bool authenticated = await provider.AuthenticateAsync();
                if (!authenticated)
                {
                    CurrentStatus = SyncStatus.Error;
                    return false;
                }
            }

            _activeProvider = provider;

            // Create necessary directories
            await provider.CreateDirectoryAsync(BACKUP_DIR);
            await provider.CreateDirectoryAsync(METADATA_DIR);
            await provider.CreateDirectoryAsync(VERSION_HISTORY_DIR);

            // Register device if service available
            if (_deviceRegistry != null)
            {
                await _deviceRegistry.RegisterDeviceAsync(provider);
            }

            // Ensure encryption key exists
            if (_encryptionService != null && !_encryptionService.KeyExists)
            {
                _encryptionService.GenerateKeyPair();
            }

            System.Diagnostics.Debug.WriteLine($"Cloud sync initialized with {provider.ProviderName}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize sync: {ex.Message}");
            CurrentStatus = SyncStatus.Error;
            return false;
        }
    }

    /// <summary>
    /// Perform a manual sync now
    /// </summary>
    public async Task<int> SyncNowAsync()
    {
        if (_activeProvider == null)
        {
            throw new InvalidOperationException("Cloud sync not initialized");
        }

        lock (_syncLock)
        {
            if (CurrentStatus == SyncStatus.Syncing)
            {
                return 0;
            }

            CurrentStatus = SyncStatus.Syncing;
        }

        try
        {
            int filesSynced = 0;

            // Sync each local data file
            foreach (var fileName in _syncFiles)
            {
                try
                {
                    bool synced = await SyncFileAsync(fileName);
                    if (synced) filesSynced++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to sync {fileName}: {ex.Message}");
                    _syncQueue.Enqueue(fileName); // Queue for retry
                }
            }

            // Process queued syncs
            while (_syncQueue.Count > 0)
            {
                var fileName = _syncQueue.Dequeue();
                try
                {
                    bool synced = await SyncFileAsync(fileName);
                    if (synced) filesSynced++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to sync queued file {fileName}: {ex.Message}");
                    _syncQueue.Enqueue(fileName); // Re-queue if still failing
                }
            }

            _lastSyncTime = DateTime.Now;
            await UpdateSyncMetadataAsync();
            
            CurrentStatus = SyncStatus.Idle;
            System.Diagnostics.Debug.WriteLine($"Sync completed: {filesSynced} files synced");
            return filesSynced;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync failed: {ex.Message}");
            CurrentStatus = SyncStatus.Error;
            return 0;
        }
    }

    /// <summary>
    /// Enable automatic syncing at specified interval
    /// </summary>
    public void EnableAutoSync(TimeSpan interval)
    {
        _autoSyncTimer?.Dispose();
        _autoSyncTimer = new Timer(
            async _ => await SyncNowAsync(),
            null,
            interval,
            interval
        );

        System.Diagnostics.Debug.WriteLine($"Auto-sync enabled with {interval.TotalSeconds}s interval");
    }

    /// <summary>
    /// Disable automatic syncing
    /// </summary>
    public void DisableAutoSync()
    {
        _autoSyncTimer?.Dispose();
        _autoSyncTimer = null;
        System.Diagnostics.Debug.WriteLine("Auto-sync disabled");
    }

    /// <summary>
    /// Get pending syncs waiting in queue
    /// </summary>
    public Task<string[]> GetPendingSyncsAsync()
    {
        lock (_syncLock)
        {
            return Task.FromResult(_syncQueue.ToArray());
        }
    }

    /// <summary>
    /// Pause all sync operations
    /// </summary>
    public void PauseSync()
    {
        CurrentStatus = SyncStatus.Paused;
        System.Diagnostics.Debug.WriteLine("Sync paused");
    }

    /// <summary>
    /// Resume sync operations
    /// </summary>
    public async Task ResumeSyncAsync()
    {
        if (CurrentStatus == SyncStatus.Paused)
        {
            CurrentStatus = SyncStatus.Idle;
            await SyncNowAsync();
            System.Diagnostics.Debug.WriteLine("Sync resumed");
        }
    }

    /// <summary>
    /// Resolve a sync conflict
    /// </summary>
    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictResolution resolution)
    {
        if (_activeProvider == null)
        {
            return;
        }

        try
        {
            string cloudPath = $"{BACKUP_DIR}{conflict.FilePath}";

            switch (resolution)
            {
                case ConflictResolution.KeepLocal:
                    // Upload local file (overwrite cloud)
                    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var localPath = Path.Combine(appDataPath, "FocusDeck", "Data", conflict.FilePath);
                    if (File.Exists(localPath))
                    {
                        await _activeProvider.UploadFileAsync(localPath, cloudPath);
                    }
                    break;

                case ConflictResolution.KeepRemote:
                    // Download cloud file (overwrite local)
                    var downloadPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "FocusDeck", "Data", conflict.FilePath
                    );
                    await _activeProvider.DownloadFileAsync(cloudPath, downloadPath);
                    break;

                case ConflictResolution.MergeData:
                    // TODO: Implement intelligent JSON merge
                    System.Diagnostics.Debug.WriteLine("JSON merge not yet implemented");
                    break;

                case ConflictResolution.UserChoose:
                    // User should have already chosen, nothing to do
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"Conflict resolved: {conflict.FilePath} -> {resolution}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to resolve conflict: {ex.Message}");
        }
    }

    // Helper Methods

    private async Task<bool> SyncFileAsync(string fileName)
    {
        if (_activeProvider == null || _encryptionService == null)
        {
            return false;
        }

        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dataPath = Path.Combine(appDataPath, "FocusDeck", "Data");
            var localPath = Path.Combine(dataPath, fileName);
            var cloudPath = $"{BACKUP_DIR}{fileName}.encrypted";

            // Create data directory if it doesn't exist
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            if (!File.Exists(localPath))
            {
                // Try to download from cloud if local file doesn't exist
                if (await _activeProvider.FileExistsAsync(cloudPath))
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), fileName);
                    await _activeProvider.DownloadFileAsync(cloudPath, tempPath);

                    // Decrypt and save
                    string encryptedContent = File.ReadAllText(tempPath);
                    string decryptedContent = _encryptionService.Decrypt(encryptedContent);
                    File.WriteAllText(localPath, decryptedContent);
                    File.Delete(tempPath);
                }
                return true;
            }

            // Check for conflicts
            if (await _activeProvider.FileExistsAsync(cloudPath))
            {
                bool hasConflict = await CheckConflictAsync(fileName, localPath, cloudPath);
                if (hasConflict)
                {
                    CurrentStatus = SyncStatus.ConflictDetected;
                    return false;
                }
            }

            // Upload file (encrypted)
            string content = File.ReadAllText(localPath);
            string encrypted = _encryptionService.Encrypt(content);

            var tempUploadPath = Path.Combine(Path.GetTempPath(), fileName + ".encrypted");
            File.WriteAllText(tempUploadPath, encrypted);

            await _activeProvider.UploadFileAsync(tempUploadPath, cloudPath);
            File.Delete(tempUploadPath);

            // Keep version history
            await ArchiveVersionAsync(fileName, cloudPath);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing {fileName}: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> CheckConflictAsync(string fileName, string localPath, string cloudPath)
    {
        if (_activeProvider == null)
        {
            return false;
        }

        try
        {
            // Get file info
            var localLastModified = File.GetLastWriteTime(localPath);
            var remoteLastModified = await _activeProvider.GetLastModifiedAsync(cloudPath);

            // If timestamps differ by more than 1 second, consider it a conflict
            if (Math.Abs((localLastModified - remoteLastModified).TotalSeconds) > 1)
            {
                var localHash = ComputeFileHash(localPath);
                var remoteHash = await _activeProvider.GetFileHashAsync(cloudPath);

                if (localHash != remoteHash)
                {
                    var conflict = new SyncConflict
                    {
                        FilePath = fileName,
                        LocalTimestamp = localLastModified,
                        RemoteTimestamp = remoteLastModified,
                        LocalHash = localHash,
                        RemoteHash = remoteHash
                    };

                    SyncConflictDetected?.Invoke(this, new SyncConflictEventArgs { Conflict = conflict });
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking conflict: {ex.Message}");
            return false;
        }
    }

    private async Task ArchiveVersionAsync(string fileName, string cloudPath)
    {
        if (_activeProvider == null)
        {
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var archivePath = $"{VERSION_HISTORY_DIR}{fileName}_{timestamp}.encrypted";

            // Copy to version history
            var tempPath = Path.Combine(Path.GetTempPath(), $"{fileName}_{timestamp}");
            await _activeProvider.DownloadFileAsync(cloudPath, tempPath);
            await _activeProvider.UploadFileAsync(tempPath, archivePath);
            File.Delete(tempPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to archive version: {ex.Message}");
        }
    }

    private async Task UpdateSyncMetadataAsync()
    {
        if (_activeProvider == null)
        {
            return;
        }

        try
        {
            var metadata = new
            {
                LastSyncTime = DateTime.Now,
                DeviceId = Environment.MachineName,
                SyncStatus = CurrentStatus.ToString()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var metadataPath = Path.Combine(Path.GetTempPath(), "last_sync.json");
            File.WriteAllText(metadataPath, json);

            await _activeProvider.UploadFileAsync(metadataPath, $"{METADATA_DIR}last_sync.json");
            File.Delete(metadataPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update metadata: {ex.Message}");
        }
    }

    private string ComputeFileHash(string filePath)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha256.ComputeHash(stream);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
