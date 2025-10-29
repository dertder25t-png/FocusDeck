namespace FocusDeck.Mobile.Services;

/// <summary>
/// Represents the current state of cloud synchronization.
/// </summary>
public enum CloudSyncStatus
{
    /// <summary>Not syncing, waiting for action</summary>
    Idle,

    /// <summary>Currently syncing to cloud</summary>
    Syncing,

    /// <summary>Successfully synced to cloud</summary>
    Synced,

    /// <summary>Sync failed, error occurred</summary>
    Error,

    /// <summary>Cloud sync is disabled or not configured</summary>
    Disabled
}
