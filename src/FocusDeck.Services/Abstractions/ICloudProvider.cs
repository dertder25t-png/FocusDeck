namespace FocusDeck.Services.Abstractions;

/// <summary>
/// Represents cloud file metadata information
/// </summary>
public class CloudFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime ModifiedTime { get; set; }
    public string Hash { get; set; } = string.Empty;
}

/// <summary>
/// Represents sync conflict information
/// </summary>
public class SyncConflict
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime LocalTimestamp { get; set; }
    public DateTime RemoteTimestamp { get; set; }
    public string LocalHash { get; set; } = string.Empty;
    public string RemoteHash { get; set; } = string.Empty;
}

/// <summary>
/// Resolution strategy for sync conflicts
/// </summary>
public enum ConflictResolution
{
    KeepLocal,      // Use local version
    KeepRemote,     // Download remote version
    MergeData,      // Intelligent merge (for JSON)
    UserChoose      // Prompt user to decide
}

/// <summary>
/// Base interface for cloud storage providers (OneDrive, Google Drive, etc.)
/// Handles authentication, file operations, and sync coordination
/// </summary>
public interface ICloudProvider
{
    /// <summary>Gets the provider name (e.g., "OneDrive", "Google Drive")</summary>
    string ProviderName { get; }

    /// <summary>Gets whether the provider is currently authenticated</summary>
    bool IsAuthenticated { get; }

    /// <summary>Authenticate with the cloud provider (OAuth2)</summary>
    /// <returns>True if authentication succeeded</returns>
    Task<bool> AuthenticateAsync();

    /// <summary>Revoke authentication credentials</summary>
    Task RevokeAuthAsync();

    /// <summary>Check if we have valid authentication tokens</summary>
    /// <returns>True if tokens are valid and not expired</returns>
    Task<bool> IsTokenValidAsync();

    // File Operations

    /// <summary>Upload a file to cloud storage</summary>
    /// <param name="localPath">Path to local file</param>
    /// <param name="remotePath">Target path in cloud (e.g., "/FocusDeck/data.json")</param>
    /// <returns>Cloud file ID for the uploaded file</returns>
    Task<string> UploadFileAsync(string localPath, string remotePath);

    /// <summary>Download a file from cloud storage</summary>
    /// <param name="remotePath">Path to file in cloud</param>
    /// <param name="localPath">Local path where file will be saved</param>
    Task DownloadFileAsync(string remotePath, string localPath);

    /// <summary>Delete a file from cloud storage</summary>
    /// <param name="remotePath">Path to file in cloud</param>
    Task DeleteFileAsync(string remotePath);

    /// <summary>List all files in a cloud directory</summary>
    /// <param name="remotePath">Path to directory in cloud (e.g., "/FocusDeck/")</param>
    /// <returns>Array of file info for files in the directory</returns>
    Task<CloudFileInfo[]> ListFilesAsync(string remotePath);

    /// <summary>Get the last modified time of a cloud file</summary>
    /// <param name="remotePath">Path to file in cloud</param>
    Task<DateTime> GetLastModifiedAsync(string remotePath);

    /// <summary>Get hash of a cloud file for integrity checking</summary>
    /// <param name="remotePath">Path to file in cloud</param>
    Task<string> GetFileHashAsync(string remotePath);

    /// <summary>Check if a file exists in cloud storage</summary>
    /// <param name="remotePath">Path to file in cloud</param>
    Task<bool> FileExistsAsync(string remotePath);

    /// <summary>Create a directory in cloud storage</summary>
    /// <param name="remotePath">Path to create (e.g., "/FocusDeck/backup/")</param>
    Task CreateDirectoryAsync(string remotePath);
}

/// <summary>
/// Main cloud sync service that coordinates syncing across multiple providers
/// Handles encryption, conflict resolution, and multi-device synchronization
/// </summary>
public interface ICloudSyncService
{
    /// <summary>Gets the currently active cloud provider</summary>
    ICloudProvider? ActiveProvider { get; }

    /// <summary>Gets the current sync status</summary>
    SyncStatus CurrentStatus { get; }

    /// <summary>Fired when sync status changes</summary>
    event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;

    /// <summary>Fired when a sync conflict is detected</summary>
    event EventHandler<SyncConflictEventArgs>? SyncConflictDetected;

    /// <summary>Initialize cloud sync with specified provider</summary>
    /// <param name="provider">Cloud provider instance (OneDrive, Google Drive, etc.)</param>
    /// <returns>True if initialization succeeded</returns>
    Task<bool> InitializeSyncAsync(ICloudProvider provider);

    /// <summary>Perform a manual sync now</summary>
    /// <returns>Number of files synced</returns>
    Task<int> SyncNowAsync();

    /// <summary>Enable automatic sync at specified intervals</summary>
    /// <param name="interval">Time between sync operations</param>
    void EnableAutoSync(TimeSpan interval);

    /// <summary>Disable automatic syncing</summary>
    void DisableAutoSync();

    /// <summary>Get the last successful sync time</summary>
    DateTime? LastSyncTime { get; }

    /// <summary>Resolve a detected sync conflict</summary>
    /// <param name="conflict">Conflict information</param>
    /// <param name="resolution">How to resolve the conflict</param>
    Task ResolveConflictAsync(SyncConflict conflict, ConflictResolution resolution);

    /// <summary>Get all pending syncs in queue</summary>
    /// <returns>List of files awaiting sync</returns>
    Task<string[]> GetPendingSyncsAsync();

    /// <summary>Pause all sync operations</summary>
    void PauseSync();

    /// <summary>Resume sync operations</summary>
    Task ResumeSyncAsync();
}

/// <summary>
/// Current state of the sync service
/// </summary>
public enum SyncStatus
{
    NotInitialized,  // Sync not set up yet
    Idle,            // Ready but not currently syncing
    Syncing,         // Currently syncing
    Paused,          // Sync is paused
    ConflictDetected,// Waiting for conflict resolution
    Error            // Sync encountered an error
}

/// <summary>
/// Event args for sync status changes
/// </summary>
public class SyncStatusChangedEventArgs : EventArgs
{
    public SyncStatus OldStatus { get; set; }
    public SyncStatus NewStatus { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Event args for sync conflicts
/// </summary>
public class SyncConflictEventArgs : EventArgs
{
    public SyncConflict Conflict { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Service for managing encryption/decryption of cloud data
/// Uses AES-256 with GCM mode for authenticated encryption
/// </summary>
public interface IEncryptionService
{
    /// <summary>Encrypt plain text data</summary>
    /// <param name="plainText">Data to encrypt</param>
    /// <returns>Encrypted data (base64 encoded with IV prepended)</returns>
    string Encrypt(string plainText);

    /// <summary>Decrypt encrypted data</summary>
    /// <param name="cipherText">Encrypted data (base64 encoded)</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string cipherText);

    /// <summary>Generate new encryption key pair</summary>
    void GenerateKeyPair();

    /// <summary>Check if encryption key exists</summary>
    bool KeyExists { get; }

    /// <summary>Delete existing encryption key</summary>
    void DeleteKey();

    /// <summary>Export key for backup (encrypted)</summary>
    string ExportKeyEncrypted(string password);

    /// <summary>Import key from backup</summary>
    bool ImportKeyEncrypted(string encryptedKeyData, string password);
}

/// <summary>
/// Service for managing device registration and multi-device coordination
/// </summary>
public interface IDeviceRegistryService
{
    /// <summary>Get unique device ID (MAC address + hostname)</summary>
    string DeviceId { get; }

    /// <summary>Get friendly device name</summary>
    string DeviceName { get; }

    /// <summary>Register this device for cloud sync</summary>
    Task<bool> RegisterDeviceAsync(ICloudProvider provider);

    /// <summary>Get list of all devices synced to this account</summary>
    Task<DeviceInfo[]> GetRegisteredDevicesAsync(ICloudProvider provider);

    /// <summary>Remove a device from sync</summary>
    Task UnregisterDeviceAsync(string deviceId, ICloudProvider provider);
}

/// <summary>
/// Information about a registered device
/// </summary>
public class DeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;  // Windows, iOS, Android, Web
    public DateTime RegisteredTime { get; set; }
    public DateTime LastSyncTime { get; set; }
    public bool IsOnline { get; set; }
}
