using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Jarvis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Jobs.Jarvis
{
    public class PatternRecognitionJob
    {
        private readonly IVectorStore _vectorStore;
        private readonly IAutomationGeneratorService _generatorService;
        private readonly AutomationDbContext _dbContext;
        private readonly ILogger<PatternRecognitionJob> _logger;

        public PatternRecognitionJob(
            IVectorStore vectorStore,
            IAutomationGeneratorService generatorService,
            AutomationDbContext dbContext,
            ILogger<PatternRecognitionJob> logger)
        {
            _vectorStore = vectorStore;
            _generatorService = generatorService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Pattern Recognition Job...");

            // 1. Get the most recent snapshot
            var latestSnapshot = await _dbContext.ContextSnapshots
                .Include(s => s.Slices)
                .Include(s => s.Metadata)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestSnapshot == null)
            {
                _logger.LogInformation("No context history available for pattern recognition.");
                return;
            }

            // 2. Get its vector
            var vectorEntity = await _dbContext.ContextVectors
                .FirstOrDefaultAsync(v => v.SnapshotId == latestSnapshot.Id, cancellationToken);

            if (vectorEntity == null)
            {
                _logger.LogInformation("Latest snapshot is pending vectorization. Skipping.");
                return;
            }

            var queryVector = new float[vectorEntity.Dimensions];
            Buffer.BlockCopy(vectorEntity.VectorData, 0, queryVector, 0, vectorEntity.VectorData.Length);

            // 3. Find clusters (>0.8 relevance, limit 20 to find enough)
            var neighbors = await _vectorStore.GetNearestNeighborsAsync(queryVector, limit: 20, minRelevance: 0.8);

            // Filter out the query snapshot itself and duplicates close in time
            var cluster = neighbors
                .Where(n => n.Id != latestSnapshot.Id)
                .ToList();

            // 4. Check for correlation strength
            // "strong correlation ... >5 occurrences"
            if (cluster.Count >= 5)
            {
                _logger.LogInformation("Pattern detected! Found {Count} similar moments. Triggering Generator.", cluster.Count);

                // Include the anchor snapshot in the cluster data passed to generator
                var fullCluster = new System.Collections.Generic.List<FocusDeck.Domain.Entities.Context.ContextSnapshot> { latestSnapshot };
                fullCluster.AddRange(cluster);

                await _generatorService.GenerateProposalAsync(fullCluster);
            }
            else
            {
                _logger.LogInformation("No strong pattern detected (found {Count} neighbors > 0.8).", cluster.Count);
            }
        }
    }
}
