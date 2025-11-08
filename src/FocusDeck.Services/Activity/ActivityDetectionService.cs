using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("FocusDeck.Services.Tests")]

namespace FocusDeck.Services.Activity
{
    /// <summary>
    /// Abstract base implementation of activity detection service.
    /// Provides platform-agnostic logic for activity monitoring.
    /// Platform-specific implementations (Windows, Linux, Mobile) inherit from this.
    /// </summary>
    public abstract class ActivityDetectionService : IActivityDetectionService
    {
        protected readonly ILogger<ActivityDetectionService> _logger;
        protected readonly Subject<ActivityState> _activityChanged = new();

        internal ActivityState LastState { get; set; } = new();
        internal DateTime LastActivity { get; set; } = DateTime.UtcNow;
        private const int IdleThresholdMs = 60000;

        public IObservable<ActivityState> ActivityChanged => _activityChanged.AsObservable();

        protected ActivityDetectionService(ILogger<ActivityDetectionService> logger)
        {
            _logger = logger;
        }

        protected abstract Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct);
        protected abstract Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct);

        public virtual async Task<ActivityState> GetCurrentActivityAsync(CancellationToken ct)
        {
            var focusedApp = await GetFocusedApplicationInternalAsync(ct);
            var intensity = await GetActivityIntensityInternalAsync(1, ct);

            var state = new ActivityState
            {
                FocusedApp = focusedApp,
                ActivityIntensity = intensity,
                IsIdle = DateTime.UtcNow - LastActivity > TimeSpan.FromMilliseconds(IdleThresholdMs),
                Timestamp = DateTime.UtcNow
            };

            if (state.FocusedApp?.AppName != LastState.FocusedApp?.AppName)
            {
                _activityChanged.OnNext(state);
                _logger.LogInformation("App focus changed: {App}", state.FocusedApp?.AppName ?? "(none)");
            }

            LastState = state;
            return state;
        }

        public virtual Task<bool> IsIdleAsync(int idleThresholdSeconds, CancellationToken ct)
        {
            var isIdle = DateTime.UtcNow - LastActivity > TimeSpan.FromSeconds(idleThresholdSeconds);
            return Task.FromResult(isIdle);
        }

        public virtual async Task<double> GetActivityIntensityAsync(int minutesWindow, CancellationToken ct)
        {
            return await GetActivityIntensityInternalAsync(minutesWindow, ct);
        }

        public virtual async Task<FocusedApplication?> GetFocusedApplicationAsync(CancellationToken ct)
        {
            return await GetFocusedApplicationInternalAsync(ct);
        }

        protected void RecordActivity()
        {
            LastActivity = DateTime.UtcNow;
        }
    }
}
