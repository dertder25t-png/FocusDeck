using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Services.Context;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Jobs
{
    public class VectorizeSnapshotJob : IVectorizeSnapshotJob
    {
        private readonly IContextSnapshotRepository _snapshotRepository;
        private readonly IVectorStore _vectorStore;
        private readonly ILogger<VectorizeSnapshotJob> _logger;

        public VectorizeSnapshotJob(
            IContextSnapshotRepository snapshotRepository,
            IVectorStore vectorStore,
            ILogger<VectorizeSnapshotJob> logger)
        {
            _snapshotRepository = snapshotRepository;
            _vectorStore = vectorStore;
            _logger = logger;
        }

        public async Task Execute(Guid snapshotId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting vectorization for Snapshot ID: {SnapshotId}", snapshotId);

            var snapshot = await _snapshotRepository.GetByIdAsync(snapshotId, cancellationToken);
            if (snapshot == null)
            {
                _logger.LogWarning("ContextSnapshot with ID {SnapshotId} not found. Aborting vectorization.", snapshotId);
                return;
            }

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

            await _vectorStore.UpsertAsync(snapshotId, text.ToString());

            _logger.LogInformation("Successfully vectorized snapshot {SnapshotId}", snapshotId);
        }
    }


}
