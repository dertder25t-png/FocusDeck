# Phase 6a: Cloud Backup & Sync - Technical Design

## Overview
Phase 6a enables FocusDeck data to sync across devices via cloud providers (OneDrive, Google Drive) with encryption, conflict resolution, and real-time updates.

## Architecture

### Service Layer Design

```
┌─────────────────────────────────────────────────────┐
│           Application Layer (WPF/MAUI)              │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────────┐         ┌──────────────────┐  │
│  │ StudySessionService      │ Analytics Service│  │
│  └────────┬────────┘         └────────┬─────────┘  │
│           │                           │             │
│  ┌────────▼──────────────────────────▼──────────┐  │
│  │   CloudSyncService (Coordinator)             │  │
│  │  - Manages sync lifecycle                    │  │
│  │  - Conflict resolution                       │  │
│  │  - Encryption/Decryption                     │  │
│  └──────────────┬─────────────────────┬─────────┘  │
│                 │                     │             │
│  ┌──────────────▼──┐      ┌──────────▼──────────┐ │
│  │ OneDriveProvider│      │GoogleDriveProvider  │ │
│  │  - OAuth2       │      │  - OAuth2           │ │
│  │  - Upload/Sync  │      │  - Upload/Sync      │ │
│  └─────────────────┘      └─────────────────────┘ │
│                                                     │
└─────────────────────────────────────────────────────┘
         │                           │
         ▼                           ▼
    ☁️ OneDrive                  ☁️ Google Drive
```

### Data Sync Flow

```
Local Data Change
       │
       ▼
┌─────────────────────────────┐
│ Change Detection Layer      │
│ (FileSystemWatcher)         │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│ Encryption Service          │
│ (AES-256 encryption)        │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│ Cloud Upload                │
│ (OneDrive/Google Drive)     │
└──────────┬──────────────────┘
           │
           ▼
    ✅ Data in Cloud

Remote Data Change (Other Device)
       │
       ▼
┌─────────────────────────────┐
│ Cloud Polling/Events        │
│ (Check for updates)         │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│ Download & Decrypt          │
│ (AES-256 decryption)        │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│ Conflict Resolution         │
│ (Last-write-wins/Merge)     │
└──────────┬──────────────────┘
           │
           ▼
    ✅ Local Data Updated
```

## Implementation Details

### 1. Cloud Sync Service Interface

```csharp
public interface ICloudProvider
{
    // Authentication
    Task<bool> AuthenticateAsync();
    Task RevokeAuthAsync();
    bool IsAuthenticated { get; }

    // File Operations
    Task<string> UploadFileAsync(string localPath, string remotePath);
    Task DownloadFileAsync(string remotePath, string localPath);
    Task DeleteFileAsync(string remotePath);
    
    // Sync Status
    Task<CloudFileInfo[]> ListFilesAsync(string remotePath);
    Task<DateTime> GetLastModifiedAsync(string remotePath);
}

public interface ICloudSyncService
{
    // Sync Control
    Task<bool> InitializeSyncAsync();
    Task SyncNowAsync();
    void EnableAutoSync(TimeSpan interval);
    void DisableAutoSync();
    
    // Conflict Resolution
    Task<SyncConflictResolution> ResolveConflictAsync(
        string filePath, 
        LocalData local, 
        RemoteData remote);
    
    // Status
    event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;
    SyncStatus CurrentStatus { get; }
}

public interface IEncryptionService
{
    // Symmetric encryption for sensitive data
    byte[] Encrypt(string plainText);
    string Decrypt(byte[] cipherText);
    
    // Generate and manage keys
    void GenerateKeyPair();
    bool KeyExists { get; }
}
```

### 2. Encryption Strategy (AES-256)

- **Algorithm**: AES-256 (Advanced Encryption Standard)
- **Mode**: GCM (Galois/Counter Mode) for authenticated encryption
- **Key Storage**: Secure DPAPI (Data Protection API) on Windows
- **IV**: Randomly generated for each encryption

### 3. Sync Conflict Resolution

**Strategy**: Last-Write-Wins (LWW) with User Override

```csharp
public enum ConflictResolution
{
    KeepLocal,      // Use local version
    KeepRemote,     // Download remote version
    MergeData,      // Intelligent merge (for JSON files)
    UserChoose      // Prompt user
}

public class SyncConflictResolution
{
    public DateTime LocalTimestamp { get; set; }
    public DateTime RemoteTimestamp { get; set; }
    public ConflictResolution Resolution { get; set; }
    public string LocalHash { get; set; }
    public string RemoteHash { get; set; }
}
```

**Decision Tree**:
1. If timestamps differ by > 1 second → Use newer version
2. If timestamps within 1 second and hashes differ → User choice
3. If hashes match → No conflict, use either version

### 4. OneDrive Integration

- **OAuth2 Scope**: `files.readwrite` + `offline_access`
- **SDK**: Microsoft.Graph (NuGet)
- **Auth Flow**: Interactive browser-based for desktop
- **Sync Path**: `/drive/root/apps/FocusDeck/`

### 5. Google Drive Integration

- **OAuth2 Scope**: `https://www.googleapis.com/auth/drive.file`
- **SDK**: Google.Apis.Drive.v3
- **Auth Flow**: Desktop OAuth2 flow with system browser
- **Sync Path**: `/FocusDeck/` folder in Drive

## Data Backup Structure

```
OneDrive/Google Drive
├── FocusDeck/
│   ├── backup/
│   │   ├── study_sessions.json.encrypted
│   │   ├── todos.json.encrypted
│   │   ├── workspaces.json.encrypted
│   │   ├── analytics.json.encrypted
│   │   └── settings.json.encrypted
│   ├── sync_metadata/
│   │   ├── last_sync.json
│   │   ├── device_info.json
│   │   └── conflict_log.json
│   └── version_history/
│       └── [timestamped backups for recovery]
```

## Multi-Device Sync Coordination

**Device Lock System**:
1. Each device registers with a unique ID (MAC address + hostname)
2. Lock mechanism to prevent simultaneous writes
3. 5-minute lock timeout for stale locks
4. Queue system for pending syncs

**Sync Queue Priority**:
1. Study sessions (highest priority)
2. Analytics data
3. Todos and workspaces
4. Settings (lowest priority)

## Security Considerations

- ✅ End-to-end encryption (client-side encryption)
- ✅ OAuth2 for authentication (no password storage)
- ✅ Encrypted key storage using DPAPI
- ✅ HTTPS for all cloud transfers
- ✅ Checksum verification for data integrity
- ✅ Automatic sync verification

## Performance Optimization

- **Incremental Sync**: Only sync changed files (via timestamps)
- **Batching**: Group multiple small syncs into one
- **Compression**: Optional gzip compression for large files
- **Bandwidth Throttling**: Limit upload/download speed
- **Background Sync**: Non-blocking async operations

## Fallback Strategy

If cloud sync fails:
1. Retry with exponential backoff (1s, 2s, 4s, 8s, max 60s)
2. Queue changes locally
3. Retry on network reconnection
4. Notify user of sync issues
5. Allow manual retry

## Testing Strategy

- Unit tests for encryption/decryption
- Integration tests with mock cloud providers
- End-to-end tests with real OneDrive/Google Drive (sandbox)
- Conflict resolution scenarios
- Network failure simulation
- Multi-device sync verification

## Implementation Timeline

1. **Week 1**: Service interfaces + encryption service
2. **Week 2**: OneDrive provider implementation
3. **Week 3**: Google Drive provider + conflict resolution
4. **Week 4**: Testing + polish + documentation

---

## Next: Phase 6b - Mobile Companion App (MAUI)

Will implement iOS/Android app with:
- Shared sync logic with desktop
- Quick study timer UI
- Session tracking
- Push notifications
- Offline support
