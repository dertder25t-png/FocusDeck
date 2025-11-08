using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Persistence;
using FocusDeck.Services.Activity;
using FocusDeck.Server.Services.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Context;

public class ContextAggregationService : IContextAggregationService
{
    private readonly ILogger<ContextAggregationService> _logger;
    private readonly IEnumerable<IActivityDetectionService> _detectors;
    private readonly AutomationDbContext _db;
    private readonly CanvasService _canvasService;
    private readonly FocusDeck.Server.Services.Integrations.ICanvasCache? _canvasCache;

    public IObservable<ActivityState> AggregatedActivity { get; }

    public ContextAggregationService(
        ILogger<ContextAggregationService> logger,
        IEnumerable<IActivityDetectionService> detectors,
        AutomationDbContext db,
        CanvasService canvasService,
        FocusDeck.Server.Services.Integrations.ICanvasCache? canvasCache = null)
    {
        _logger = logger;
        _detectors = detectors;
        _db = db;
        _canvasService = canvasService;
        _canvasCache = canvasCache;

        if (!_detectors.Any())
        {
            AggregatedActivity = Observable.Empty<ActivityState>();
            return;
        }

        var merged = _detectors
            .Select(d => d.ActivityChanged)
            .Merge();

        AggregatedActivity = merged
            .Select(state => Observable.FromAsync(async ct => await EnrichWithAssignmentsAsync(state, ct)))
            .Concat();
    }

    public async Task<ActivityState> GetAggregatedActivityAsync(CancellationToken ct)
    {
        var states = new List<ActivityState>();
        foreach (var d in _detectors)
        {
            try
            {
                states.Add(await d.GetCurrentActivityAsync(ct));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Detector failed to produce activity state");
            }
        }

        if (states.Count == 0)
        {
            return new ActivityState
            {
                ActivityIntensity = 0,
                IsIdle = true,
                OpenContexts = new(),
                Timestamp = DateTime.UtcNow
            };
        }

        var latest = states.OrderByDescending(s => s.Timestamp).First();
        var maxIntensity = states.Max(s => s.ActivityIntensity);
        var allIdle = states.All(s => s.IsIdle);

        var aggregated = new ActivityState
        {
            FocusedApp = latest.FocusedApp,
            ActivityIntensity = maxIntensity,
            IsIdle = allIdle,
            OpenContexts = new(),
            Timestamp = DateTime.UtcNow
        };

        return await EnrichWithAssignmentsAsync(aggregated, ct);
    }

    public async Task PersistSnapshotAsync(ActivityState state, string userId, CancellationToken ct)
    {
        try
        {
            var ctx = new FocusDeck.Domain.Entities.StudentContext
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                FocusedAppName = state.FocusedApp?.AppName,
                FocusedWindowTitle = state.FocusedApp?.WindowTitle,
                ActivityIntensity = state.ActivityIntensity,
                IsIdle = state.IsIdle,
                OpenContextsJson = state.OpenContexts.Count == 0 ? null : JsonSerializer.Serialize(state.OpenContexts)
            };

            _db.StudentContexts.Add(ctx);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist student context snapshot");
        }
    }

    private async Task<ActivityState> EnrichWithAssignmentsAsync(ActivityState state, CancellationToken ct)
    {
        try
        {
            var (domain, token) = await TryGetCanvasConfigAsync(ct);
            if (!string.IsNullOrWhiteSpace(domain) && !string.IsNullOrWhiteSpace(token))
            {
                var cached = _canvasCache?.GetAssignments();
                var assignments = (cached != null && cached.Count > 0)
                    ? cached
                    : await _canvasService.GetUpcomingAssignments(domain!, token!);
                var upcoming = assignments
                    .Where(a => a.DueAt.HasValue && a.DueAt.Value >= DateTime.UtcNow)
                    .OrderBy(a => a.DueAt)
                    .Take(5)
                    .ToList();

                foreach (var a in upcoming)
                {
                    state.OpenContexts.Add(new ContextItem
                    {
                        Type = "canvas_assignment",
                        Title = $"{a.Name} (due {a.DueAt:MMM d, h:mm tt})",
                        RelatedId = null
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to enrich activity with Canvas assignments");
        }

        return state;
    }

    private async Task<(string? domain, string? token)> TryGetCanvasConfigAsync(CancellationToken ct)
    {
        try
        {
            var cfg = await _db.ServiceConfigurations
                .AsNoTracking()
                .Where(c => c.ServiceName == "Canvas")
                .Select(c => c.AdditionalConfig)
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(cfg))
            {
                using var doc = JsonDocument.Parse(cfg);
                var root = doc.RootElement;
                var domain = root.TryGetProperty("domain", out var d) ? d.GetString() : null;
                var token = root.TryGetProperty("accessToken", out var t) ? t.GetString() : null;
                if (!string.IsNullOrWhiteSpace(domain) && !string.IsNullOrWhiteSpace(token))
                    return (domain, token);
            }
        }
        catch
        {
            // ignore
        }

        var envDomain = Environment.GetEnvironmentVariable("CANVAS_DOMAIN");
        var envToken = Environment.GetEnvironmentVariable("CANVAS_TOKEN");
        return (envDomain, envToken);
    }
}
