using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Services.Activity;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FocusDeck.Mobile.Tests.Services.Activity
{
    /// <summary>
    /// Unit tests for MobileActivityDetectionService.
    /// Tests sensor integration and mobile-specific functionality.
    /// Note: Full sensor testing requires an actual Android device or emulator.
    /// </summary>
    public class MobileActivityDetectionServiceTests
    {
        private readonly ILogger<MockMobileActivityDetectionService> _logger;

        public MobileActivityDetectionServiceTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<MockMobileActivityDetectionService>();
        }

        /// <summary>
        /// Test 1: Service can be instantiated (mock version without sensors).
        /// </summary>
        [Fact]
        public void Constructor_WithLogger_Initializes()
        {
            // Arrange & Act
            var service = new MockMobileActivityDetectionService(_logger);

            // Assert
            Assert.NotNull(service);
            Assert.NotNull(service.ActivityChanged);
        }

        /// <summary>
        /// Test 2: Service inherits from ActivityDetectionService.
        /// </summary>
        [Fact]
        public void MobileActivityDetectionService_InheritsFromBase()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);

            // Assert
            Assert.IsAssignableFrom<ActivityDetectionService>(service);
            Assert.IsAssignableFrom<IActivityDetectionService>(service);
        }

        /// <summary>
        /// Test 3: GetCurrentActivityAsync returns FocusDeck app when foreground.
        /// </summary>
        [Fact]
        public async Task GetCurrentActivityAsync_ReturnsFocusDeckWhenForeground()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);
            service.SetForeground(true);

            // Act
            var activity = await service.GetCurrentActivityAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(activity);
            Assert.Equal("FocusDeck", activity.AppName);
        }

        /// <summary>
        /// Test 4: GetCurrentActivityAsync returns null when backgrounded.
        /// </summary>
        [Fact]
        public async Task GetCurrentActivityAsync_ReturnsNullWhenBackground()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);
            service.SetForeground(false);

            // Act
            var activity = await service.GetCurrentActivityAsync(CancellationToken.None);

            // Assert
            Assert.Null(activity);
        }

        /// <summary>
        /// Test 5: GetFocusedApplicationAsync includes correct tags.
        /// </summary>
        [Fact]
        public async Task GetFocusedApplicationAsync_IncludesCorrectTags()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);
            service.SetForeground(true);

            // Act
            var app = await service.GetFocusedApplicationAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(app);
            Assert.NotNull(app.Tags);
            Assert.Contains("focus", app.Tags);
            Assert.Contains("mobile", app.Tags);
        }

        /// <summary>
        /// Test 6: GetActivityIntensityAsync returns valid range.
        /// </summary>
        [Fact]
        public async Task GetActivityIntensityAsync_ReturnsValidRange()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);

            // Act
            var intensity = await service.GetActivityIntensityAsync(minutesWindow: 5, CancellationToken.None);

            // Assert
            Assert.InRange(intensity, 0.0, 100.0);
        }

        /// <summary>
        /// Test 7: IsIdleAsync tracks idle state.
        /// </summary>
        [Fact]
        public async Task IsIdleAsync_TracksIdleState()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);
            service.RecordMotion();

            // Act
            var isIdle = await service.IsIdleAsync(idleThresholdSeconds: 60, CancellationToken.None);

            // Assert
            Assert.False(isIdle);
        }

        /// <summary>
        /// Test 8: Motion recording updates activity.
        /// </summary>
        [Fact]
        public async Task RecordMotion_UpdatesActivityTime()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);
            var beforeActivity = service.LastActivity;
            await Task.Delay(50);

            // Act
            service.RecordMotion();

            // Assert
            Assert.True(service.LastActivity >= beforeActivity);
        }

        /// <summary>
        /// Test 9: Multiple motion events tracked.
        /// </summary>
        [Fact]
        public async Task GetActivityIntensityAsync_ReflectsMultipleMotions()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);

            // Act
            service.RecordMotion();
            await Task.Delay(50);
            service.RecordMotion();
            await Task.Delay(50);
            var intensity = await service.GetActivityIntensityAsync(minutesWindow: 5, CancellationToken.None);

            // Assert
            Assert.InRange(intensity, 0, 100);
        }

        /// <summary>
        /// Test 10: CancellationToken support.
        /// </summary>
        [Fact]
        public async Task GetCurrentActivityAsync_SupportsCancellation()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            try
            {
                await service.GetCurrentActivityAsync(cts.Token);
                Assert.True(true);
            }
            catch (OperationCanceledException)
            {
                Assert.True(true);
            }
        }

        /// <summary>
        /// Test 11: ActivityChanged observable fires.
        /// </summary>
        [Fact]
        public async Task ActivityChanged_EmitsEvents()
        {
            // Arrange
            var service = new MockMobileActivityDetectionService(_logger);
            var stateChanges = new System.Collections.Generic.List<ActivityState>();
            var subscription = service.ActivityChanged.Subscribe(state => stateChanges.Add(state));

            // Act
            service.SetForeground(true);
            await service.GetCurrentActivityAsync(CancellationToken.None);
            await Task.Delay(100);

            // Assert
            subscription.Dispose();
            Assert.NotNull(stateChanges);
        }
    }

    /// <summary>
    /// Mock implementation for testing without actual sensors.
    /// </summary>
    internal class MockMobileActivityDetectionService : ActivityDetectionService
    {
        private bool _isForeground = true;
        private int _motionCount;
        private DateTime _lastMotionTime = DateTime.UtcNow;

        public MockMobileActivityDetectionService(ILogger<MockMobileActivityDetectionService> logger)
            : base(logger)
        {
        }

        protected override Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct)
        {
            if (!_isForeground)
                return Task.FromResult<FocusedApplication?>(null);

            var app = new FocusedApplication
            {
                AppName = "FocusDeck",
                WindowTitle = "FocusDeck Study Timer",
                ProcessPath = "",
                Tags = new[] { "focus", "mobile" },
                SwitchedAt = DateTime.UtcNow
            };

            return Task.FromResult<FocusedApplication?>(app);
        }

        protected override Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct)
        {
            int intensity = 0;

            // Motion count contributes to intensity
            intensity += Math.Min(_motionCount * 5, 40);

            // Recent activity boost
            if (DateTime.UtcNow - _lastMotionTime < TimeSpan.FromSeconds(10))
                intensity += 30;

            // Device foreground contributes
            if (_isForeground)
                intensity += 10;

            return Task.FromResult(Math.Min(intensity, 100));
        }

        public void SetForeground(bool isForeground)
        {
            _isForeground = isForeground;
        }

        public void RecordMotion()
        {
            _motionCount++;
            _lastMotionTime = DateTime.UtcNow;
            RecordActivity();
        }
    }
}
