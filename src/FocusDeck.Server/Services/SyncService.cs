using FocusDeck.Persistence;
using FocusDeck.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FocusDeck.Server.Services
{
    /// <summary>
    /// Manages cross-device synchronization using the server as the central hub
    /// </summary>
    public interface ISyncService
    {
        /// <summary>Register a new device for syncing</summary>
        Task<DeviceRegistration> RegisterDeviceAsync(string deviceId, string deviceName, DevicePlatform platform, string userId);

        /// <summary>Get all devices registered for a user</summary>
        Task<List<DeviceRegistration>> GetUserDevicesAsync(string userId);

        /// <summary>Unregister a device</summary>
        Task<bool> UnregisterDeviceAsync(string deviceId, string userId);

        /// <summary>Push local changes from device to server</summary>
        Task<SyncResult> PushChangesAsync(SyncPushRequest request, string userId);

    /// <summary>Pull changes from server to device</summary>
    Task<SyncPullResponse> PullChangesAsync(string deviceId, long lastKnownVersion, string userId, SyncEntityType? entityType = null);

        /// <summary>Perform full bidirectional sync</summary>
        Task<SyncResult> SyncAsync(SyncPushRequest request, string userId);

        /// <summary>Get sync statistics for a user</summary>
        Task<SyncStatistics> GetSyncStatisticsAsync(string userId);

        /// <summary>Resolve a conflict</summary>
        Task<bool> ResolveConflictAsync(string entityId, ConflictResolution resolution, string userId);
    }

    public class SyncService : ISyncService
    {
        private readonly AutomationDbContext _db;
        private readonly ILogger<SyncService> _logger;

        public SyncService(AutomationDbContext db, ILogger<SyncService> logger)
        {
            _db = db;
            _logger = logger;
        }

        private async Task<long> GetCurrentVersionAsync()
        {
            var max = await _db.Set<SyncVersion>()
                .OrderByDescending(v => v.Id)
                .Select(v => v.Id)
                .FirstOrDefaultAsync();
            return max;
        }

        private async Task<List<long>> AllocateVersionsAsync(int count)
        {
            if (count <= 0) return new List<long>();
            var stamps = new List<SyncVersion>(capacity: count);
            for (int i = 0; i < count; i++)
            {
                stamps.Add(new SyncVersion { CreatedAt = DateTime.UtcNow });
            }
            _db.Set<SyncVersion>().AddRange(stamps);
            await _db.SaveChangesAsync();
            return stamps.Select(s => s.Id).OrderBy(id => id).ToList();
        }

        public async Task<DeviceRegistration> RegisterDeviceAsync(string deviceId, string deviceName, DevicePlatform platform, string userId)
        {
            try
            {
                // Check if device already exists
                var existing = await _db.Set<DeviceRegistration>()
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId);

                if (existing != null)
                {
                    // Update existing registration
                    existing.DeviceName = deviceName;
                    existing.Platform = platform;
                    existing.LastSyncAt = DateTime.UtcNow;
                    existing.IsActive = true;
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Device {DeviceId} re-registered for user {UserId}", deviceId, userId);
                    return existing;
                }

                // Create new registration
                var device = new DeviceRegistration
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    Platform = platform,
                    UserId = userId,
                    RegisteredAt = DateTime.UtcNow,
                    LastSyncAt = DateTime.UtcNow,
                    IsActive = true
                };

                _db.Set<DeviceRegistration>().Add(device);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Device {DeviceId} registered for user {UserId}", deviceId, userId);
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register device {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<List<DeviceRegistration>> GetUserDevicesAsync(string userId)
        {
            return await _db.Set<DeviceRegistration>()
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderByDescending(d => d.LastSyncAt)
                .ToListAsync();
        }

        public async Task<bool> UnregisterDeviceAsync(string deviceId, string userId)
        {
            try
            {
                var device = await _db.Set<DeviceRegistration>()
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId);

                if (device == null)
                {
                    return false;
                }

                device.IsActive = false;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Device {DeviceId} unregistered for user {UserId}", deviceId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister device {DeviceId}", deviceId);
                return false;
            }
        }

        public async Task<SyncResult> PushChangesAsync(SyncPushRequest request, string userId)
        {
            var result = new SyncResult();

            try
            {
                // Verify device is registered
                var device = await _db.Set<DeviceRegistration>()
                    .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && d.UserId == userId);

                if (device == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Device not registered";
                    return result;
                }

                // Begin DB transaction for atomicity
                var dbtx = await _db.Database.BeginTransactionAsync();

                // Create transaction
                var transaction = new SyncTransaction
                {
                    Id = Guid.NewGuid(),
                    DeviceId = request.DeviceId,
                    Timestamp = DateTime.UtcNow,
                    Status = SyncStatus.Processing,
                    Changes = request.Changes
                };

                // Process each change and collect non-conflicting ones
                var acceptedChanges = new List<SyncChange>();
                foreach (var change in request.Changes)
                {
                    change.Id = Guid.NewGuid();
                    change.TransactionId = transaction.Id;

                    // Check for conflicts
                    var existingChange = await _db.Set<SyncChange>()
                        .Where(c => c.EntityId == change.EntityId && c.EntityType == change.EntityType)
                        .OrderByDescending(c => c.ChangeVersion)
                        .FirstOrDefaultAsync();

                    if (existingChange != null && existingChange.ChangeVersion > request.LastKnownVersion)
                    {
                        // Conflict detected
                        result.Conflicts.Add(new SyncConflict
                        {
                            EntityId = change.EntityId,
                            EntityType = change.EntityType,
                            LocalChange = change,
                            ServerChange = existingChange,
                            Resolution = ConflictResolution.Manual
                        });
                        continue;
                    }

                    acceptedChanges.Add(change);
                }

                // Allocate durable version numbers for accepted changes
                List<long> allocatedVersions = await AllocateVersionsAsync(acceptedChanges.Count);
                for (int i = 0; i < acceptedChanges.Count; i++)
                {
                    acceptedChanges[i].ChangeVersion = allocatedVersions[i];
                    _db.Set<SyncChange>().Add(acceptedChanges[i]);
                    result.ChangesPushed++;
                }

                transaction.Status = result.Conflicts.Any() ? SyncStatus.Conflict : SyncStatus.Completed;
                _db.Set<SyncTransaction>().Add(transaction);

                // Update device sync metadata
                device.LastSyncAt = DateTime.UtcNow;
                var metadata = await GetOrCreateMetadata(request.DeviceId);
                var lastAllocated = allocatedVersions.Count > 0 ? allocatedVersions[^1] : await GetCurrentVersionAsync();
                metadata.LastSyncVersion = lastAllocated;
                metadata.LastSyncTime = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await dbtx.CommitAsync();

                var currentVersion = await GetCurrentVersionAsync();
                result.Success = !result.Conflicts.Any();
                result.NewVersion = currentVersion;

                _logger.LogInformation("Pushed {Count} changes from device {DeviceId}", result.ChangesPushed, request.DeviceId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push changes from device {DeviceId}", request.DeviceId);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public async Task<SyncPullResponse> PullChangesAsync(string deviceId, long lastKnownVersion, string userId, SyncEntityType? entityType = null)
        {
            try
            {
                // Verify device is registered
                var device = await _db.Set<DeviceRegistration>()
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId);

                if (device == null)
                {
                    return new SyncPullResponse { CurrentVersion = await GetCurrentVersionAsync(), Changes = new List<SyncChange>() };
                }

                // Get changes since last known version (excluding changes from this device)
                var query = _db.Set<SyncChange>().AsQueryable();
                query = query.Where(c => c.ChangeVersion > lastKnownVersion);
                if (entityType.HasValue)
                {
                    query = query.Where(c => c.EntityType == entityType.Value);
                }

                var changes = await query
                    .Join(_db.Set<SyncTransaction>(),
                          change => change.TransactionId,
                          transaction => transaction.Id,
                          (change, transaction) => new { Change = change, Transaction = transaction })
                    .Where(x => x.Transaction.DeviceId != deviceId) // Don't send back device's own changes
                    .Select(x => x.Change)
                    .OrderBy(c => c.ChangeVersion)
                    .Take(100) // Limit batch size
                    .ToListAsync();

                device.LastSyncAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Pulled {Count} changes for device {DeviceId}", changes.Count, deviceId);

                return new SyncPullResponse
                {
                    CurrentVersion = await GetCurrentVersionAsync(),
                    Changes = changes,
                    HasMoreChanges = changes.Count == 100
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pull changes for device {DeviceId}", deviceId);
                return new SyncPullResponse { CurrentVersion = await GetCurrentVersionAsync(), Changes = new List<SyncChange>() };
            }
        }

        public async Task<SyncResult> SyncAsync(SyncPushRequest request, string userId)
        {
            // First push local changes
            var pushResult = await PushChangesAsync(request, userId);

            // Then pull server changes
            var pullResponse = await PullChangesAsync(request.DeviceId, request.LastKnownVersion, userId);

            pushResult.ChangesPulled = pullResponse.Changes.Count;
            pushResult.NewVersion = pullResponse.CurrentVersion;

            return pushResult;
        }

        public async Task<SyncStatistics> GetSyncStatisticsAsync(string userId)
        {
            try
            {
                var transactions = await _db.Set<SyncTransaction>()
                    .Join(_db.Set<DeviceRegistration>(),
                          t => t.DeviceId,
                          d => d.DeviceId,
                          (t, d) => new { Transaction = t, Device = d })
                    .Where(x => x.Device.UserId == userId)
                    .Select(x => x.Transaction)
                    .ToListAsync();

                var devices = await GetUserDevicesAsync(userId);

                return new SyncStatistics
                {
                    TotalSyncs = transactions.Count,
                    SuccessfulSyncs = transactions.Count(t => t.Status == SyncStatus.Completed),
                    FailedSyncs = transactions.Count(t => t.Status == SyncStatus.Failed),
                    ConflictsResolved = transactions.Count(t => t.Status == SyncStatus.Conflict),
                    LastSuccessfulSync = transactions
                        .Where(t => t.Status == SyncStatus.Completed)
                        .OrderByDescending(t => t.Timestamp)
                        .FirstOrDefault()?.Timestamp ?? DateTime.MinValue,
                    SyncsByPlatform = devices
                        .GroupBy(d => d.Platform)
                        .ToDictionary(g => g.Key, g => g.Count())
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sync statistics for user {UserId}", userId);
                return new SyncStatistics();
            }
        }

        public async Task<bool> ResolveConflictAsync(string entityId, ConflictResolution resolution, string userId)
        {
            try
            {
                // Find conflicts for this entity
                var changes = await _db.Set<SyncChange>()
                    .Where(c => c.EntityId == entityId)
                    .OrderByDescending(c => c.ChangeVersion)
                    .Take(2)
                    .ToListAsync();

                if (changes.Count < 2)
                {
                    return false;
                }

                // Apply resolution strategy
                switch (resolution)
                {
                    case ConflictResolution.UseServer:
                        // Keep the latest version (already in database)
                        break;

                    case ConflictResolution.UseLocal:
                        // Revert to the older local version and assign a new global version
                        var localChange = changes[1];
                        var newVersion = (await AllocateVersionsAsync(1)).First();
                        localChange.ChangeVersion = newVersion;
                        break;

                    case ConflictResolution.Merge:
                        // Attempt automatic merge (entity-specific logic needed)
                        _logger.LogWarning("Automatic merge not yet implemented for {EntityId}", entityId);
                        return false;

                    case ConflictResolution.Manual:
                        // Requires user intervention
                        return false;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Resolved conflict for entity {EntityId} using {Resolution}", entityId, resolution);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve conflict for entity {EntityId}", entityId);
                return false;
            }
        }

        private async Task<SyncMetadata> GetOrCreateMetadata(string deviceId)
        {
            var metadata = await _db.Set<SyncMetadata>()
                .FirstOrDefaultAsync(m => m.DeviceId == deviceId);

            if (metadata == null)
            {
                metadata = new SyncMetadata
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceId,
                    LastSyncVersion = 0,
                    LastSyncTime = DateTime.UtcNow,
                    EntityVersions = new Dictionary<SyncEntityType, long>()
                };
                _db.Set<SyncMetadata>().Add(metadata);
            }

            return metadata;
        }
    }
}
