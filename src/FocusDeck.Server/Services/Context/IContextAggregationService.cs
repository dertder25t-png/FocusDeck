using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Services.Activity;

namespace FocusDeck.Server.Services.Context;

public interface IContextAggregationService
{
    IObservable<ActivityState> AggregatedActivity { get; }

    Task<ActivityState> GetAggregatedActivityAsync(CancellationToken ct);

    Task PersistSnapshotAsync(ActivityState state, string userId, CancellationToken ct);
}

