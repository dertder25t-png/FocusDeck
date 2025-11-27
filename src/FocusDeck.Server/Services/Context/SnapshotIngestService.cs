using FocusDeck.Contracts.DTOs;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Server.Jobs;
using FocusDeck.Services.Context;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace FocusDeck.Server.Services.Context;

public class SnapshotIngestService : ISnapshotIngestService
{
    private readonly ILogger<SnapshotIngestService> _logger;
    private readonly IBackgroundJobClient _jobClient;
    private readonly IContextSnapshotRepository _snapshotRepository;
    private readonly IContextEventBus _eventBus;

    public SnapshotIngestService(
        ILogger<SnapshotIngestService> logger,
        IBackgroundJobClient jobClient,
        IContextSnapshotRepository snapshotRepository,
        IContextEventBus eventBus)
    {
        _logger = logger;
        _jobClient = jobClient;
        _snapshotRepository = snapshotRepository;
        _eventBus = eventBus;
    }

    public async Task IngestSnapshotAsync(ContextSnapshotDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting snapshot: {EventType} at {Timestamp}", dto.EventType, dto.Timestamp);

        var slices = new List<FocusDeck.Domain.Entities.Context.ContextSlice>();
        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(dto.ActiveApplication) || !string.IsNullOrEmpty(dto.ActiveWindowTitle))
        {
            slices.Add(new FocusDeck.Domain.Entities.Context.ContextSlice
            {
                Id = Guid.NewGuid(),
                SourceType = FocusDeck.Domain.Entities.Context.ContextSourceType.DesktopActiveWindow,
                Timestamp = dto.Timestamp,
                Data = (System.Text.Json.Nodes.JsonObject?)System.Text.Json.Nodes.JsonNode.Parse(
                    JsonSerializer.Serialize(new { App = dto.ActiveApplication, Title = dto.ActiveWindowTitle }))
            });
        }

        if (!string.IsNullOrEmpty(dto.CalendarEventId))
        {
            slices.Add(new FocusDeck.Domain.Entities.Context.ContextSlice
            {
                Id = Guid.NewGuid(),
                SourceType = FocusDeck.Domain.Entities.Context.ContextSourceType.GoogleCalendar,
                Timestamp = dto.Timestamp,
                Data = (System.Text.Json.Nodes.JsonObject?)System.Text.Json.Nodes.JsonNode.Parse(
                    JsonSerializer.Serialize(new { EventId = dto.CalendarEventId }))
            });
        }

        if (dto.FeatureSummary != null)
        {
             slices.Add(new FocusDeck.Domain.Entities.Context.ContextSlice
            {
                Id = Guid.NewGuid(),
                SourceType = FocusDeck.Domain.Entities.Context.ContextSourceType.DeviceActivity,
                Timestamp = dto.Timestamp,
                Data = (System.Text.Json.Nodes.JsonObject?)System.Text.Json.Nodes.JsonNode.Parse(
                    JsonSerializer.Serialize(dto.FeatureSummary))
            });
        }

        var snapshot = new FocusDeck.Domain.Entities.Context.ContextSnapshot
        {
            Id = Guid.NewGuid(),
            Timestamp = dto.Timestamp,
            // Metadata is not present in DTO, assuming null or default
            Metadata = null,
            Slices = slices,
            VectorizationState = FocusDeck.Domain.Entities.Context.VectorizationState.Pending
        };

        // Save to database directly.
        await _snapshotRepository.AddAsync(snapshot, cancellationToken);

        _logger.LogInformation("Saved snapshot {SnapshotId} with status Pending", snapshot.Id);

        // Publish event for real-time automation engine
        await _eventBus.PublishAsync(snapshot);
    }
}
