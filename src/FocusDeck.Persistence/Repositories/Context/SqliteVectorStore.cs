using System;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Persistence.Repositories.Context
{
    public class SqliteVectorStore : IVectorStore
    {
        private readonly AutomationDbContext _dbContext;
        private readonly IEmbeddingGenerationService _embeddingService;
        private readonly ICurrentTenant _currentTenant;
        private readonly ILogger<SqliteVectorStore> _logger;

        public SqliteVectorStore(
            AutomationDbContext dbContext,
            IEmbeddingGenerationService embeddingService,
            ICurrentTenant currentTenant,
            ILogger<SqliteVectorStore> logger)
        {
            _dbContext = dbContext;
            _embeddingService = embeddingService;
            _currentTenant = currentTenant;
            _logger = logger;
        }

        public async Task<System.Collections.Generic.List<ContextSnapshot>> GetNearestNeighborsAsync(float[] queryVector, int limit = 5)
        {
            // 1. Load all vectors into memory (MVP approach for < 100k records)
            // Select strictly what we need to minimize memory footprint
            var vectors = await _dbContext.ContextVectors
                .AsNoTracking()
                .Select(v => new { v.SnapshotId, v.VectorData })
                .ToListAsync();

            var candidates = new System.Collections.Generic.List<(Guid SnapshotId, double Score)>();

            // 2. Calculate Cosine Similarity in-memory
            foreach (var v in vectors)
            {
                var targetVector = new float[queryVector.Length];
                Buffer.BlockCopy(v.VectorData, 0, targetVector, 0, v.VectorData.Length);

                // Assuming normalized vectors (which the service should guarantee)
                // Cosine Similarity = Dot Product
                double dotProduct = 0;
                for (int i = 0; i < queryVector.Length && i < targetVector.Length; i++)
                {
                    dotProduct += queryVector[i] * targetVector[i];
                }

                candidates.Add((v.SnapshotId, dotProduct));
            }

            // 3. Sort and take top N
            var topIds = candidates
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.SnapshotId)
                .ToList();

            // 4. Retrieve snapshots
            var snapshots = await _dbContext.ContextSnapshots
                .Include(s => s.Slices)
                .Where(s => topIds.Contains(s.Id))
                .ToListAsync();

            // Re-order to match relevance (DB query might scramble order)
            return topIds
                .Select(id => snapshots.FirstOrDefault(s => s.Id == id))
                .Where(s => s != null)
                .ToList()!;
        }

        public async Task UpsertAsync(Guid snapshotId, string text)
        {
            _logger.LogInformation("Generating embedding for snapshot {SnapshotId}", snapshotId);

            // 1. Generate embedding
            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);

            // 2. Convert float[] to byte[] for BLOB storage
            var vectorBytes = new byte[embedding.Length * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, vectorBytes, 0, vectorBytes.Length);

            // 3. Create or update ContextVector entity
            var existingVector = await _dbContext.ContextVectors
                .FirstOrDefaultAsync(v => v.SnapshotId == snapshotId);

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
                    TenantId = _currentTenant.TenantId ?? Guid.Empty, // Should be handled by db context but good to be explicit
                    SnapshotId = snapshotId,
                    VectorData = vectorBytes,
                    Dimensions = embedding.Length,
                    ModelName = _embeddingService.ModelName,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _dbContext.ContextVectors.Add(newVector);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Stored vector for snapshot {SnapshotId} in SQLite", snapshotId);
        }
    }
}
