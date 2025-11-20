using System;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Server.Jobs;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services
{
    public class VectorStoreStub : IVectorStore
    {
        private readonly ILogger<VectorStoreStub> _logger;

        public VectorStoreStub(ILogger<VectorStoreStub> logger)
        {
            _logger = logger;
        }

        public Task UpsertAsync(Guid snapshotId, string text)
        {
            _logger.LogInformation("Vectorizing and storing snapshot {SnapshotId}", snapshotId);
            return Task.CompletedTask;
        }

        public Task<System.Collections.Generic.List<FocusDeck.Domain.Entities.Context.ContextSnapshot>> GetNearestNeighborsAsync(float[] queryVector, int limit = 5)
        {
            return Task.FromResult(new System.Collections.Generic.List<FocusDeck.Domain.Entities.Context.ContextSnapshot>());
        }
    }
}
