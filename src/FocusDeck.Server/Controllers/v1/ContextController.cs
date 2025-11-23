using Asp.Versioning;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace FocusDeck.Server.Controllers.v1;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/context")]
[ApiController]
public class ContextController : ControllerBase
{
    private readonly IContextAggregationService _aggregator;
    private readonly FocusDeck.Services.Context.IContextSnapshotService _snapshotService;
    private readonly AutomationDbContext _db;
    private readonly ILogger<ContextController> _logger;

    public ContextController(
        IContextAggregationService aggregator,
        FocusDeck.Services.Context.IContextSnapshotService snapshotService,
        AutomationDbContext db,
        ILogger<ContextController> logger)
    {
        _aggregator = aggregator;
        _snapshotService = snapshotService;
        _db = db;
        _logger = logger;
    }

    private string GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No authenticated user found. Using test user (development only).");
            return "test-user";
        }
        return userId;
    }

    [HttpGet("latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityStateDto>> GetLatest(CancellationToken ct)
    {
        var state = await _aggregator.GetAggregatedActivityAsync(ct);
        return Ok(ActivityStateDto.FromState(state));
    }

    /// <summary>
    /// Trigger an immediate context capture (Phase 1.5)
    /// </summary>
    [HttpPost("capture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> CaptureNow(CancellationToken ct)
    {
        var userId = Guid.Parse(GetUserId());
        var snapshot = await _snapshotService.CaptureNowAsync(userId, ct);
        return Ok(snapshot.Id);
    }

    [HttpGet("timeline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ActivityStateDto>>> GetTimeline([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var query = _db.StudentContexts.AsNoTracking().Where(s => s.UserId == userId);
        if (from.HasValue) query = query.Where(s => s.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(s => s.Timestamp <= to.Value);

        var items = await query
            .OrderByDescending(s => s.Timestamp)
            .Take(Math.Min(limit, 1000))
            .ToListAsync(ct);

        var result = items.Select(ActivityStateDto.FromEntity).ToList();
        return Ok(result);
    }
}

public record ActivityStateDto(
    string? focusedAppName,
    string? focusedWindowTitle,
    int activityIntensity,
    bool isIdle,
    DateTime timestamp,
    List<ContextItemDto> openContexts
)
{
    public static ActivityStateDto FromState(FocusDeck.Services.Activity.ActivityState state)
    {
        return new ActivityStateDto(
            state.FocusedApp?.AppName,
            state.FocusedApp?.WindowTitle,
            state.ActivityIntensity,
            state.IsIdle,
            state.Timestamp,
            state.OpenContexts.Select(c => new ContextItemDto(c.Type, c.Title, c.RelatedId)).ToList()
        );
    }

    public static ActivityStateDto FromEntity(FocusDeck.Domain.Entities.StudentContext s)
    {
        List<ContextItemDto> contexts = new();
        if (!string.IsNullOrWhiteSpace(s.OpenContextsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<ContextItemDto>>(s.OpenContextsJson);
                if (parsed != null) contexts = parsed;
            }
            catch { }
        }

        return new ActivityStateDto(
            s.FocusedAppName,
            s.FocusedWindowTitle,
            s.ActivityIntensity,
            s.IsIdle,
            s.Timestamp,
            contexts
        );
    }
}

public record ContextItemDto(string type, string title, Guid? relatedId);

