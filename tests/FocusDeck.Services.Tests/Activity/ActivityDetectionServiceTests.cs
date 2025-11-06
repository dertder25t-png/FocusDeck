using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Services.Activity;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FocusDeck.Services.Tests.Activity
{
    /// <summary>
    /// Unit tests for activity detection service implementations.
    /// Tests interface compilation, domain models, and base class functionality.
    /// </summary>
    public class ActivityDetectionServiceTests
    {
        private readonly ILogger<TestActivityDetectionService> _logger;
        private readonly TestActivityDetectionService _service;

        public ActivityDetectionServiceTests()
        {
            // Use the console logger provider for testing
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TestActivityDetectionService>();
            _service = new TestActivityDetectionService(_logger);
        }

        /// <summary>
        /// Test 1: Interface IActivityDetectionService compiles and has required methods.
        /// </summary>
        [Fact]
        public void IActivityDetectionService_HasRequiredMethods()
        {
            // Assert - Verify interface exists and has expected members
            var interfaceType = typeof(IActivityDetectionService);
            Assert.NotNull(interfaceType);
            Assert.True(interfaceType.IsInterface);
            
            var methods = interfaceType.GetMethods();
            Assert.Contains(methods, m => m.Name == "GetCurrentActivityAsync");
            Assert.Contains(methods, m => m.Name == "GetFocusedApplicationAsync");
            Assert.Contains(methods, m => m.Name == "IsIdleAsync");
            Assert.Contains(methods, m => m.Name == "GetActivityIntensityAsync");
        }

        /// <summary>
        /// Test 2: Service can be instantiated with logger.
        /// </summary>
        [Fact]
        public void Constructor_WithLogger_InitializesSuccessfully()
        {
            // Arrange & Act
            var service = new TestActivityDetectionService(_logger);

            // Assert
            Assert.NotNull(service);
            Assert.NotNull(service.ActivityChanged);
        }

        /// <summary>
        /// Test 3: ActivityState domain model has required properties.
        /// </summary>
        [Fact]
        public void ActivityState_HasRequiredProperties()
        {
            // Arrange
            var state = new ActivityState();

            // Assert
            Assert.True(typeof(ActivityState).GetProperty(nameof(ActivityState.Id)) != null);
            Assert.True(typeof(ActivityState).GetProperty(nameof(ActivityState.ActivityIntensity)) != null);
            Assert.True(typeof(ActivityState).GetProperty(nameof(ActivityState.IsIdle)) != null);
            Assert.True(typeof(ActivityState).GetProperty(nameof(ActivityState.FocusedApp)) != null);
            Assert.True(typeof(ActivityState).GetProperty(nameof(ActivityState.OpenContexts)) != null);
            Assert.True(typeof(ActivityState).GetProperty(nameof(ActivityState.Timestamp)) != null);
        }

        /// <summary>
        /// Test 4: FocusedApplication domain model has required properties.
        /// </summary>
        [Fact]
        public void FocusedApplication_HasRequiredProperties()
        {
            // Arrange
            var app = new FocusedApplication();

            // Assert
            Assert.True(typeof(FocusedApplication).GetProperty(nameof(FocusedApplication.AppName)) != null);
            Assert.True(typeof(FocusedApplication).GetProperty(nameof(FocusedApplication.WindowTitle)) != null);
            Assert.True(typeof(FocusedApplication).GetProperty(nameof(FocusedApplication.ProcessPath)) != null);
            Assert.True(typeof(FocusedApplication).GetProperty(nameof(FocusedApplication.Tags)) != null);
            Assert.True(typeof(FocusedApplication).GetProperty(nameof(FocusedApplication.SwitchedAt)) != null);
        }

        /// <summary>
        /// Test 5: ContextItem domain model has required properties.
        /// </summary>
        [Fact]
        public void ContextItem_HasRequiredProperties()
        {
            // Arrange
            var context = new ContextItem();

            // Assert
            Assert.True(typeof(ContextItem).GetProperty(nameof(ContextItem.Type)) != null);
            Assert.True(typeof(ContextItem).GetProperty(nameof(ContextItem.Title)) != null);
            Assert.True(typeof(ContextItem).GetProperty(nameof(ContextItem.RelatedId)) != null);
        }

        /// <summary>
        /// Test 6: GetCurrentActivityAsync returns ActivityState with valid data.
        /// </summary>
        [Fact]
        public async Task GetCurrentActivityAsync_ReturnsValidActivityState()
        {
            // Act
            var activity = await _service.GetCurrentActivityAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(activity);
            Assert.NotEqual(Guid.Empty, activity.Id);
            Assert.InRange(activity.ActivityIntensity, 0, 100);
            Assert.NotNull(activity.OpenContexts);
        }

        /// <summary>
        /// Test 7: IsIdleAsync correctly identifies idle state when time threshold exceeded.
        /// </summary>
        [Fact]
        public async Task IsIdleAsync_DetectsIdleWhenThresholdExceeded()
        {
            // Arrange - Directly set last activity to 65 seconds ago via property
            _service.LastActivity = DateTime.UtcNow.AddSeconds(-65);

            // Act
            var isIdle = await _service.IsIdleAsync(idleThresholdSeconds: 60, CancellationToken.None);

            // Assert
            Assert.True(isIdle);
        }

        /// <summary>
        /// Test 8: IsIdleAsync returns false for recent activity.
        /// </summary>
        [Fact]
        public async Task IsIdleAsync_ReturnsActiveDuringRecentActivity()
        {
            // Arrange - Record activity now
            _service.RecordActivityPublic();

            // Act
            var isIdle = await _service.IsIdleAsync(idleThresholdSeconds: 60, CancellationToken.None);

            // Assert
            Assert.False(isIdle);
        }

        /// <summary>
        /// Test 9: GetActivityIntensityAsync returns valid range (0-100).
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
        /// Test 10: GetFocusedApplicationAsync returns current focused app.
        /// </summary>
        [Fact]
        public async Task GetFocusedApplicationAsync_ReturnsFocusedApplication()
        {
            // Arrange
            _service.SimulateAppFocus("Chrome", "Google Chrome");

            // Act
            var app = await _service.GetFocusedApplicationAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(app);
            Assert.Equal("Chrome", app.AppName);
            Assert.Equal("Google Chrome", app.WindowTitle);
        }

        /// <summary>
        /// Test 11: ActivityChanged observable emits when app changes.
        /// </summary>
        [Fact]
        public async Task ActivityChanged_EmitsWhenAppFocusChanges()
        {
            // Arrange
            var stateChanges = new List<ActivityState>();
            var subscription = _service.ActivityChanged.Subscribe(state => stateChanges.Add(state));

            // Act
            _service.SimulateAppFocus("VSCode", "MyProject");
            await _service.GetCurrentActivityAsync(CancellationToken.None);
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(stateChanges);
            subscription.Dispose();
        }

        /// <summary>
        /// Test 12: Multiple app focus changes tracked.
        /// </summary>
        [Fact]
        public async Task ActivityChanged_TracksMultipleFocusChanges()
        {
            // Arrange
            var stateChanges = new List<ActivityState>();
            var subscription = _service.ActivityChanged.Subscribe(state => stateChanges.Add(state));

            // Act
            _service.SimulateAppFocus("App1", "Title1");
            await _service.GetCurrentActivityAsync(CancellationToken.None);
            await Task.Delay(50);
            
            _service.SimulateAppFocus("App2", "Title2");
            await _service.GetCurrentActivityAsync(CancellationToken.None);
            await Task.Delay(50);
            
            _service.SimulateAppFocus("App3", "Title3");
            await _service.GetCurrentActivityAsync(CancellationToken.None);
            await Task.Delay(50);

            // Assert
            Assert.True(stateChanges.Count >= 2);
            subscription.Dispose();
        }
    }

    /// <summary>
    /// Concrete test implementation of ActivityDetectionService.
    /// Allows simulation of platform-specific behavior for testing.
    /// </summary>
    internal class TestActivityDetectionService : ActivityDetectionService
    {
        private FocusedApplication? _simulatedApp;
        private int _simulatedIntensity = 50;

        public TestActivityDetectionService(ILogger<TestActivityDetectionService> logger)
            : base(logger)
        {
        }

        protected override Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct)
        {
            return Task.FromResult(_simulatedApp);
        }

        protected override Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct)
        {
            return Task.FromResult(_simulatedIntensity);
        }

        public void SimulateAppFocus(string appName, string windowTitle)
        {
            _simulatedApp = new FocusedApplication
            {
                AppName = appName,
                WindowTitle = windowTitle,
                ProcessPath = $"C:\\Program Files\\{appName}\\{appName}.exe",
                Tags = new[] { "productivity" },
                SwitchedAt = DateTime.UtcNow
            };
        }

        public void RecordActivityPublic()
        {
            RecordActivity();
        }
    }
}
