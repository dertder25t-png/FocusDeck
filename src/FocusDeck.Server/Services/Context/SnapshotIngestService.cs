using FocusDeck.Contracts.DTOs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;

namespace FocusDeck.Server.Services.Context;

public class SnapshotIngestService : ISnapshotIngestService
{
    private readonly ILogger<SnapshotIngestService> _logger;

    public SnapshotIngestService(ILogger<SnapshotIngestService> logger)
    {
        _logger = logger;
    }

    public Task IngestSnapshotAsync(ContextSnapshotDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting snapshot: {EventType} at {Timestamp}", dto.EventType, dto.Timestamp);
        // This is a placeholder implementation. The actual implementation will ingest the snapshot into the database.
        return Task.CompletedTask;
    }
}
