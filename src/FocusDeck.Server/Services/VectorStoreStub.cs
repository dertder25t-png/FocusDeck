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
    }
}
