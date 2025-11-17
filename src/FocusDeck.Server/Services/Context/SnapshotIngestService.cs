using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;

namespace FocusDeck.Server.Services.Context;

public class SnapshotIngestService : ISnapshotIngestService
{
    private readonly ILogger<SnapshotIngestService> _logger;
    private readonly IBackgroundJobClient _jobClient;

    public SnapshotIngestService(ILogger<SnapshotIngestService> logger, IBackgroundJobClient jobClient)
    {
        _logger = logger;
        _jobClient = jobClient;
    }

    public Task IngestSnapshotAsync(ContextSnapshotDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting snapshot: {EventType} at {Timestamp}", dto.EventType, dto.Timestamp);

        // This is a placeholder implementation. The actual implementation will first save the snapshot
        // to the database to get a unique ID. For now, we'll use a new Guid.
        var snapshotId = Guid.NewGuid();

        // After saving the snapshot, enqueue a background job to vectorize it.
        // This ensures the API request returns quickly while the heavy lifting is done in the background.
        _jobClient.Enqueue<IVectorizeSnapshotJob>(job => job.Execute(snapshotId));

        _logger.LogInformation("Enqueued vectorization job for snapshot {SnapshotId}", snapshotId);

        return Task.CompletedTask;
    }
}
