using System;
using System.Collections.Generic;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Domain.Entities.Sync
{
    /// <summary>
    /// Represents a device registered for syncing
    /// </summary>
    public class DeviceRegistration : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = null!; // Unique identifier (MAC+Hostname hash)
        public string DeviceName { get; set; } = null!; // Friendly name
        public DevicePlatform Platform { get; set; }
        public string UserId { get; set; } = null!; // User who owns this device
        public DateTime RegisteredAt { get; set; }
        public DateTime LastSyncAt { get; set; }
        public bool IsActive { get; set; }
        public string? AppVersion { get; set; }
        public Guid TenantId { get; set; }
    }

    /// <summary>
    /// Platform types for device identification
    /// </summary>
    public enum DevicePlatform
    {
        Windows,
        Linux,
        MacOS,
        Android,
        iOS,
        Web
    }

    /// <summary>
    /// Represents a sync transaction - a batch of changes from a device
    /// </summary>
    public class SyncTransaction : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public List<SyncChange> Changes { get; set; } = new();
        public SyncStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid TenantId { get; set; }
    }

    /// <summary>
    /// Individual change within a sync transaction
    /// </summary>
    public class SyncChange : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public SyncEntityType EntityType { get; set; }
        public string EntityId { get; set; } = null!; // ID of the entity (SessionId, AutomationId, etc.)
        public SyncOperation Operation { get; set; }
        public string DataJson { get; set; } = null!; // Serialized entity data
        public DateTime ChangedAt { get; set; }
        public long ChangeVersion { get; set; } // Monotonically increasing version number
        public Guid TenantId { get; set; }
    }

    /// <summary>
    /// Types of entities that can be synced
    /// </summary>
    public enum SyncEntityType
    {
        StudySession,
        Task,
        Note,
        Deck,
        Automation,
        ServiceConfiguration,
        UserSettings
    }

    /// <summary>
    /// Type of operation performed on the entity
    /// </summary>
    public enum SyncOperation
    {
        Create,
        Update,
        Delete
    }

    /// <summary>
    /// Status of a sync transaction
    /// </summary>
    public enum SyncStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Conflict
    }

    /// <summary>
    /// Request to push local changes to server
    /// </summary>
    public class SyncPushRequest
    {
        public string DeviceId { get; set; } = null!;
        public long LastKnownVersion { get; set; } // Last version this device knows about
        public List<SyncChange> Changes { get; set; } = new();
    }

    /// <summary>
    /// Response from server with changes to pull
    /// </summary>
    public class SyncPullResponse
    {
        public long CurrentVersion { get; set; } // Latest version on server
        public List<SyncChange> Changes { get; set; } = new();
        public bool HasMoreChanges { get; set; }
    }

    /// <summary>
    /// Complete sync operation result
    /// </summary>
    public class SyncResult
    {
        public bool Success { get; set; }
        public long NewVersion { get; set; }
        public int ChangesPushed { get; set; }
        public int ChangesPulled { get; set; }
        public List<SyncConflict> Conflicts { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents a sync conflict that needs resolution
    /// </summary>
    public class SyncConflict
    {
        public string EntityId { get; set; } = null!;
        public SyncEntityType EntityType { get; set; }
        public SyncChange LocalChange { get; set; } = null!;
        public SyncChange ServerChange { get; set; } = null!;
        public ConflictResolution Resolution { get; set; }
    }

    /// <summary>
    /// How to resolve a conflict
    /// </summary>
    public enum ConflictResolution
    {
        UseLocal,       // Keep local version
        UseServer,      // Accept server version
        Merge,          // Attempt automatic merge
        Manual          // Requires user intervention
    }

    /// <summary>
    /// Metadata about sync state for a device
    /// </summary>
    public class SyncMetadata : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = null!;
        public long LastSyncVersion { get; set; } // Last version successfully synced
        public DateTime LastSyncTime { get; set; }
        public Dictionary<SyncEntityType, long> EntityVersions { get; set; } = new();
        public Guid TenantId { get; set; }
    }

    /// <summary>
    /// Statistics about sync operations
    /// </summary>
    public class SyncStatistics
    {
        public int TotalSyncs { get; set; }
        public int SuccessfulSyncs { get; set; }
        public int FailedSyncs { get; set; }
        public int ConflictsResolved { get; set; }
        public DateTime LastSuccessfulSync { get; set; }
        public long TotalDataSynced { get; set; } // bytes
        public Dictionary<DevicePlatform, int> SyncsByPlatform { get; set; } = new();
    }
}
