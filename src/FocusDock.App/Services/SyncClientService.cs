using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Shared.Models.Sync;
using FocusDeck.Shared.Services;
using FocusDock.Data;

namespace FocusDock.App.Services
{
    /// <summary>
    /// Windows desktop sync client wrapper that uses the shared ClientSyncManager
    /// </summary>
    public class SyncClientService
    {
        private readonly ApiClient _apiClient;
        private ClientSyncManager? _client;
    private System.Threading.Timer? _pollTimer;
        public LocalChangeTracker ChangeTracker { get; } = new();

        public string DeviceId { get; private set; } = string.Empty;
        public string DeviceName { get; private set; } = Environment.MachineName;

        public SyncClientService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task InitializeAsync()
        {
            var settings = SettingsStore.LoadSettings();
            var serverUrl = settings.ServerUrl;
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                // No server configured yet; skip sync silently
                return;
            }

            DeviceId = ClientSyncManager.GenerateDeviceId();
            _client = new ClientSyncManager(serverUrl!, DeviceId, DeviceName, DevicePlatform.Windows);

            // Apply JWT token if configured
            if (!string.IsNullOrWhiteSpace(settings.JwtToken))
            {
                _client.SetJwtToken(settings.JwtToken);
            }

            // Register device
            await _client.RegisterDeviceAsync();

            // Start lightweight polling to receive server changes (every 60s)
            _pollTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    if (_client != null)
                    {
                        await _client.PullChangesAsync();
                    }
                }
                catch { /* ignore transient errors */ }
            }, null, dueTime: TimeSpan.FromSeconds(10), period: TimeSpan.FromSeconds(60));
        }

        /// <summary>
        /// Push local changes to server (call this when local entities change)
        /// </summary>
        public async Task PushAsync(LocalChangeTracker tracker)
        {
            if (_client == null) return;
            var changes = tracker.GetPendingChanges();
            if (changes.Count == 0) return;
            var result = await _client.PushChangesAsync(changes);
            if (!result.Success && result.Conflicts != null && result.Conflicts.Count > 0)
            {
                foreach (var conflict in result.Conflicts)
                {
                    try
                    {
                        var dialog = new ConflictResolutionDialog(conflict)
                        {
                            Owner = System.Windows.Application.Current?.MainWindow
                        };
                        var ok = dialog.ShowDialog();
                        if (ok == true)
                        {
                            var choice = dialog.GetChoice();
                            if (choice != FocusDeck.Shared.Models.Sync.ConflictResolution.Manual)
                            {
                                await _client.ResolveConflictAsync(conflict.EntityId, choice);
                            }
                        }
                        else
                        {
                            // User canceled; leave conflict unresolved
                        }
                    }
                    catch { /* ignore UI errors */ }
                }

                // After resolving, pull latest changes
                try { await _client.PullChangesAsync(); } catch { }
            }
        }

        public void Stop()
        {
            _pollTimer?.Dispose();
            _pollTimer = null;
        }

        public async Task<System.Collections.Generic.List<DeviceRegistration>?> GetDevicesAsync()
        {
            return _client == null ? null : await _client.GetDevicesAsync();
        }

        public async Task<SyncStatistics?> GetStatisticsAsync()
        {
            return _client == null ? null : await _client.GetStatisticsAsync();
        }

        // Convenience helpers for common entities
        public void TrackNoteCreated(object note)
        {
            // Note has string Id
            var id = note?.GetType().GetProperty("Id")?.GetValue(note)?.ToString() ?? Guid.NewGuid().ToString();
            ChangeTracker.TrackChange(SyncEntityType.Note, id, SyncOperation.Create, note!);
        }

        public void TrackNoteUpdated(object note)
        {
            var id = note?.GetType().GetProperty("Id")?.GetValue(note)?.ToString() ?? Guid.NewGuid().ToString();
            ChangeTracker.TrackChange(SyncEntityType.Note, id, SyncOperation.Update, note!);
        }

        public void TrackNoteDeleted(string id)
        {
            ChangeTracker.TrackChange(SyncEntityType.Note, id, SyncOperation.Delete, new { Id = id });
        }

        public void TrackTaskCreated(object task)
        {
            var id = task?.GetType().GetProperty("Id")?.GetValue(task)?.ToString() ?? Guid.NewGuid().ToString();
            ChangeTracker.TrackChange(SyncEntityType.Task, id, SyncOperation.Create, task!);
        }

        public void TrackTaskUpdated(object task)
        {
            var id = task?.GetType().GetProperty("Id")?.GetValue(task)?.ToString() ?? Guid.NewGuid().ToString();
            ChangeTracker.TrackChange(SyncEntityType.Task, id, SyncOperation.Update, task!);
        }

        public void TrackTaskDeleted(string id)
        {
            ChangeTracker.TrackChange(SyncEntityType.Task, id, SyncOperation.Delete, new { Id = id });
        }

        public void TrackDeckCreated(Guid id, object deck)
        {
            ChangeTracker.TrackChange(SyncEntityType.Deck, id.ToString(), SyncOperation.Create, deck!);
        }

        public void TrackDeckUpdated(Guid id, object deck)
        {
            ChangeTracker.TrackChange(SyncEntityType.Deck, id.ToString(), SyncOperation.Update, deck!);
        }

        public void TrackDeckDeleted(Guid id)
        {
            ChangeTracker.TrackChange(SyncEntityType.Deck, id.ToString(), SyncOperation.Delete, new { Id = id });
        }
    }
}
