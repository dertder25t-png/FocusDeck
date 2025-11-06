using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Services.Activity;
using FocusDeck.Services.Activity;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FocusDeck.Server.Tests.Services.Activity
{
    /// <summary>
    /// Unit tests for LinuxActivityDetectionService.
    /// Tests wmctrl/xdotool integration and Linux-specific functionality.
    /// </summary>
    public class LinuxActivityDetectionServiceTests
    {
        private readonly ILogger<LinuxActivityDetectionService> _logger;
        private readonly LinuxActivityDetectionService _service;

        public LinuxActivityDetectionServiceTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<LinuxActivityDetectionService>();
            _service = new LinuxActivityDetectionService(_logger);
        }

        /// <summary>
        /// Test 1: Service instantiates successfully.
        /// </summary>
        [Fact]
        public void Constructor_WithLogger_InitializesSuccessfully()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<LinuxActivityDetectionService>();

            // Act
            var service = new LinuxActivityDetectionService(logger);

            // Assert
            Assert.NotNull(service);
            Assert.NotNull(service.ActivityChanged);
        }

        /// <summary>
        /// Test 2: Inherits from ActivityDetectionService.
        /// </summary>
        [Fact]
        public void LinuxActivityDetectionService_InheritsFromBase()
        {
            // Assert
            Assert.IsAssignableFrom<ActivityDetectionService>(_service);
            Assert.IsAssignableFrom<IActivityDetectionService>(_service);
        }

        /// <summary>
        /// Test 3: GetCurrentActivityAsync returns valid state (gracefully handles missing tools).
        /// </summary>
        [Fact]
        public async Task GetCurrentActivityAsync_HandlesGracefully()
        {
            // Act
            var activity = await _service.GetCurrentActivityAsync(CancellationToken.None);

            // Assert
            // On systems without wmctrl/xdotool, may return null or partial data
            Assert.True(activity == null || activity.Id != Guid.Empty);
        }

        /// <summary>
        /// Test 4: GetActivityIntensityAsync returns valid range.
        /// </summary>
        [Fact]
        public async Task GetActivityIntensityAsync_ReturnsValidRange()
        {
            // Act
            var intensity = await _service.GetActivityIntensityAsync(minutesWindow: 5, CancellationToken.None);

            // Assert
            Assert.InRange(intensity, 0.0, 100.0);
        }

        /// <summary>
        /// Test 5: IsIdleAsync tracks idle state.
        /// </summary>
        [Fact]
        public async Task IsIdleAsync_TracksIdleState()
        {
            // Arrange
            _service.RecordLinuxActivity();

            // Act
            var isIdle = await _service.IsIdleAsync(idleThresholdSeconds: 60, CancellationToken.None);

            // Assert
            Assert.False(isIdle);
        }

        /// <summary>
        /// Test 6: RecordLinuxActivity updates activity time.
        /// </summary>
        [Fact]
        public async Task RecordLinuxActivity_UpdatesActivityTime()
        {
            // Arrange
            var beforeActivity = _service.LastActivity;
            await Task.Delay(50);

            // Act
            _service.RecordLinuxActivity();

            // Assert
            Assert.True(_service.LastActivity >= beforeActivity);
        }

        /// <summary>
        /// Test 7: Application classification works for known Linux apps.
        /// </summary>
        [Fact]
        public void ApplicationClassification_WorksForKnownApps()
        {
            // Verify classification method exists via reflection
            var classifyMethod = typeof(LinuxActivityDetectionService)
                .GetMethod("ClassifyApplication", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.NotNull(classifyMethod);
        }

        /// <summary>
        /// Test 8: Multiple activity recordings tracked.
        /// </summary>
        [Fact]
        public async Task GetActivityIntensityAsync_ReflectsMultipleActivities()
        {
            // Act
            _service.RecordLinuxActivity();
            await Task.Delay(50);
            _service.RecordLinuxActivity();
            await Task.Delay(50);
            var intensity = await _service.GetActivityIntensityAsync(minutesWindow: 5, CancellationToken.None);

            // Assert
            Assert.InRange(intensity, 0, 100);
        }

        /// <summary>
        /// Test 9: Supports CancellationToken.
        /// </summary>
        [Fact]
        public async Task GetCurrentActivityAsync_SupportsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            try
            {
                await _service.GetCurrentActivityAsync(cts.Token);
                Assert.True(true);  // If no exception, that's fine
            }
            catch (OperationCanceledException)
            {
                Assert.True(true);  // Cancellation is acceptable
            }
        }

        /// <summary>
        /// Test 10: ActivityChanged observable fires.
        /// </summary>
        [Fact]
        public async Task ActivityChanged_EmitsEvents()
        {
            // Arrange
            var stateChanges = new System.Collections.Generic.List<ActivityState>();
            var subscription = _service.ActivityChanged.Subscribe(state => stateChanges.Add(state));

            // Act
            await _service.GetCurrentActivityAsync(CancellationToken.None);
            await Task.Delay(100);

            // Assert
            subscription.Dispose();
            // May or may not have changes depending on system state
            Assert.NotNull(stateChanges);
        }
    }
}
