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
