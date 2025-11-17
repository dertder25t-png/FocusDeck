using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Server.Services.Context;

public interface ISnapshotIngestService
{
    Task IngestSnapshotAsync(ContextSnapshotDto dto, CancellationToken cancellationToken);
}
