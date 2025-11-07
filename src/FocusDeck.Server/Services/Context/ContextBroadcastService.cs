using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Hubs;
using FocusDeck.Server.Services.Context;
using FocusDeck.Services.Activity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Context;

public class ContextBroadcastService : BackgroundService
{
    private readonly ILogger<ContextBroadcastService> _logger;
    private readonly IContextAggregationService _aggregator;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hub;
    private readonly FocusDeck.Server.Services.Auth.IUserConnectionTracker _tracker;

    public ContextBroadcastService(
        ILogger<ContextBroadcastService> logger,
        IContextAggregationService aggregator,
        IHubContext<NotificationsHub, INotificationClient> hub,
        FocusDeck.Server.Services.Auth.IUserConnectionTracker tracker)
    {
        _logger = logger;
        _aggregator = aggregator;
        _hub = hub;
        _tracker = tracker;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _aggregator.AggregatedActivity.Subscribe(async state =>
        {
            try
            {
                foreach (var userId in _tracker.GetUserIds())
                {
                    await _hub.Clients.Group($"user:{userId}").ContextUpdated(state);
                    await _aggregator.PersistSnapshotAsync(state, userId, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed broadcasting context update");
            }
        });

        // Periodic broadcast to ensure UI updates even without event-driven detectors
        _ = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var state = await _aggregator.GetAggregatedActivityAsync(stoppingToken);
                    foreach (var userId in _tracker.GetUserIds())
                    {
                        await _hub.Clients.Group($"user:{userId}").ContextUpdated(state);
                        await _aggregator.PersistSnapshotAsync(state, userId, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Periodic context update failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }, stoppingToken);

        return Task.CompletedTask;
    }
}
