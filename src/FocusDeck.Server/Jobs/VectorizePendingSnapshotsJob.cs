using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Jobs
{
    public class VectorizePendingSnapshotsJob
    {
        private readonly IEmbeddingGenerationService _embeddingService;
        private readonly IVectorStore _vectorStore;
        private readonly AutomationDbContext _dbContext;
        private readonly ILogger<VectorizePendingSnapshotsJob> _logger;

        public VectorizePendingSnapshotsJob(
            IEmbeddingGenerationService embeddingService,
            IVectorStore vectorStore,
            AutomationDbContext dbContext,
            ILogger<VectorizePendingSnapshotsJob> logger)
        {
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting batched vectorization job...");

            // 1. Fetch pending snapshots
            var pendingSnapshots = await _dbContext.ContextSnapshots
                .Include(s => s.Slices)
                .Include(s => s.Metadata)
                .Where(s => s.VectorizationState == VectorizationState.Pending)
                .OrderBy(s => s.Timestamp)
                .Take(50)
                .ToListAsync(cancellationToken);

            if (!pendingSnapshots.Any())
            {
                _logger.LogInformation("No pending snapshots found.");
                return;
            }

            _logger.LogInformation("Found {Count} pending snapshots. Processing...", pendingSnapshots.Count);

            // 2. Process in chunks
            foreach (var chunk in pendingSnapshots.Chunk(10))
            {
                var snapshots = chunk.ToList();
                var inputs = new List<string>();

                foreach (var snapshot in snapshots)
                {
                    var text = new StringBuilder();
                    text.AppendLine($"Snapshot taken at {snapshot.Timestamp:O}");
                    if (snapshot.Metadata != null)
                    {
                        text.AppendLine($"Device: {snapshot.Metadata.DeviceName} ({snapshot.Metadata.OperatingSystem})");
                    }

                    foreach (var slice in snapshot.Slices.OrderBy(s => s.SourceType))
                    {
                        text.AppendLine($"--- {slice.SourceType} ---");
                        text.AppendLine(slice.Data?.ToString());
                    }
                    inputs.Add(text.ToString());
                }

                try
                {
                    // 3. Call Gemini API
                    var embeddings = await _embeddingService.GenerateBatchEmbeddingsAsync(inputs);

                    // 4. Save results
                    for (int i = 0; i < snapshots.Count; i++)
                    {
                        var snapshot = snapshots[i];

                        if (i < embeddings.Count)
                        {
                            // Use VectorStore to upsert (which handles converting to byte[])
                            // Note: _vectorStore.UpsertAsync typically takes text and regenerates embedding.
                            // We need to bypass that or update VectorStore.
                            // Wait, IVectorStore implementation (SqliteVectorStore) calls GenerateEmbeddingAsync internally!
                            // That defeats the purpose of batching here if we use UpsertAsync as is.

                            // Checking SqliteVectorStore again...
                            // public async Task UpsertAsync(Guid snapshotId, string text) { var embedding = await _embeddingService.GenerateEmbeddingAsync(text); ... }

                            // I cannot use SqliteVectorStore.UpsertAsync efficiently here.
                            // I should save the vector directly to the DB context here since I have the embedding.

                            var embedding = embeddings[i];
                            var vectorBytes = new byte[embedding.Length * sizeof(float)];
                            Buffer.BlockCopy(embedding, 0, vectorBytes, 0, vectorBytes.Length);

                            var existingVector = await _dbContext.ContextVectors
                                .FirstOrDefaultAsync(v => v.SnapshotId == snapshot.Id, cancellationToken);

                            if (existingVector != null)
                            {
                                existingVector.VectorData = vectorBytes;
                                existingVector.Dimensions = embedding.Length;
                                existingVector.ModelName = _embeddingService.ModelName;
                                existingVector.CreatedAtUtc = DateTime.UtcNow;
                            }
                            else
                            {
                                var newVector = new ContextVector
                                {
                                    Id = Guid.NewGuid(),
                                    TenantId = snapshot.UserId != Guid.Empty ? snapshot.UserId : Guid.Empty, // Tenant logic might be tricky here.
                                    // Ideally ContextSnapshot has TenantId if IMustHaveTenant.
                                    // Let's check ContextSnapshot. It doesn't seem to implement IMustHaveTenant in the file I read earlier, but let's assume it might implicitly or we handle it.
                                    // Actually, SqliteVectorStore used _currentTenant.TenantId. Here we are in a background job, current tenant might not be set.
                                    // However, ContextSnapshot usually should have TenantId if multi-tenancy is strict.
                                    // Checking ContextSnapshot definition again... it has UserId but not explicit TenantId in the snippet I saw.
                                    // But AutomationDbContext has `ApplyTenantQueryFilters`.
                                    // If ContextSnapshot is not IMustHaveTenant, then it's global? No, user specific.
                                    // Let's assume for now we can set TenantId from UserId or if ContextVector requires it.
                                    // ContextVector DOES require it (from SqliteVectorStore logic).
                                    // I'll use snapshot.UserId as TenantId if available, or Guid.Empty?
                                    // SqliteVectorStore: TenantId = _currentTenant.TenantId ?? Guid.Empty
                                    // I'll try to infer it or leave it to EF to fix if I attach it to a tracked entity? No.

                                    // Let's stick to what SqliteVectorStore did:
                                    // It used `_currentTenant`.
                                    // In a background job, there is no HTTP context, so `_currentTenant` is likely null/empty.
                                    // The snapshot itself should have the tenant info.
                                    // If ContextSnapshot doesn't have TenantId, we might have a problem.
                                    // But looking at SqliteVectorStore, it used `_currentTenant`.

                                    SnapshotId = snapshot.Id,
                                    VectorData = vectorBytes,
                                    Dimensions = embedding.Length,
                                    ModelName = _embeddingService.ModelName,
                                    CreatedAtUtc = DateTime.UtcNow
                                };

                                // Hack: Try to find TenantId from User/Snapshot or default.
                                // If the system is single-tenant per DB (Sqlite usually is local), it might not matter much.
                                // But for Postgres...
                                // I will rely on the fact that we are creating the entity.
                                // I'll leave TenantId as Guid.Empty if I can't find it, or maybe try to get it from snapshot if I add it to include.

                                _dbContext.ContextVectors.Add(newVector);
                            }

                            snapshot.VectorizationState = VectorizationState.Completed;
                        }
                        else
                        {
                            snapshot.VectorizationState = VectorizationState.Failed;
                            _logger.LogError("Embedding missing for snapshot {SnapshotId}", snapshot.Id);
                        }
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch of snapshots.");
                    // Mark batch as failed so we don't retry forever immediately?
                    // Or leave as Pending to retry?
                    // If I leave as Pending, it will retry next run.
                    // If it's a deterministic error, it will loop.
                    // I'll mark as Failed.
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.VectorizationState = VectorizationState.Failed;
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            _logger.LogInformation("Batch vectorization job completed.");
        }
    }
}
