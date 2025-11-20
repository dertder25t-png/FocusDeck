using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Domain.Entities.Context;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Context
{
    public class ContextRetrievalService : IContextRetrievalService
    {
        private readonly IVectorStore _vectorStore;
        private readonly IEmbeddingGenerationService _embeddingService;
        private readonly ILogger<ContextRetrievalService> _logger;

        public ContextRetrievalService(
            IVectorStore vectorStore,
            IEmbeddingGenerationService embeddingService,
            ILogger<ContextRetrievalService> logger)
        {
            _vectorStore = vectorStore;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task<List<ContextSnapshot>> GetSimilarMomentsAsync(ContextSnapshot current)
        {
            // 1. Convert current snapshot to text representation
            var text = new StringBuilder();
            text.AppendLine($"Snapshot taken at {current.Timestamp:O}");
            if (current.Metadata != null)
            {
                text.AppendLine($"Device: {current.Metadata.DeviceName} ({current.Metadata.OperatingSystem})");
            }

            foreach (var slice in current.Slices.OrderBy(s => s.SourceType))
            {
                text.AppendLine($"--- {slice.SourceType} ---");
                text.AppendLine(slice.Data?.ToString());
            }

            // 2. Generate embedding for query
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(text.ToString());

            // 3. Search vector store
            var neighbors = await _vectorStore.GetNearestNeighborsAsync(queryVector, limit: 10); // Fetch more to filter

            // 4. Filter
            var filtered = neighbors
                .Where(s => Math.Abs((s.Timestamp - current.Timestamp).TotalMinutes) > 1) // Filter out essentially the same moment
                .Take(5)
                .ToList();

            return filtered;
        }
    }
}
