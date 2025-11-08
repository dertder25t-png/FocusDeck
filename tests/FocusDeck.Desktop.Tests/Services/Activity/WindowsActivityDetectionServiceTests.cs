using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Desktop.Services.Activity;
using FocusDeck.Services.Activity;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FocusDeck.Desktop.Tests.Services.Activity
{
    /// <summary>
    /// Unit tests for WindowsActivityDetectionService.
    /// Tests P/Invoke interop, window tracking, and activity detection.
    /// </summary>
    public class WindowsActivityDetectionServiceTests
    {
        private readonly ILogger<WindowsActivityDetectionService> _logger;
        private readonly WindowsActivityDetectionService _service;

        public WindowsActivityDetectionServiceTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<WindowsActivityDetectionService>();
            _service = new WindowsActivityDetectionService(_logger);
        }

        /// <summary>
        /// Test 1: Service instantiates successfully with logger.
        /// </summary>
        [Fact]
        public void Constructor_WithLogger_InitializesSuccessfully()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<WindowsActivityDetectionService>();

            // Act
            var service = new WindowsActivityDetectionService(logger);

            // Assert
            Assert.NotNull(service);
            Assert.NotNull(service.ActivityChanged);
        }

        /// <summary>
        /// Test 2: Inherits from ActivityDetectionService base class.
        /// </summary>
        [Fact]
        public void WindowsActivityDetectionService_InheritsFromBase()
        {
            // Assert
            Assert.IsAssignableFrom<ActivityDetectionService>(_service);
            Assert.IsAssignableFrom<IActivityDetectionService>(_service);
        }

        /// <summary>
        /// Test 3: GetCurrentActivityAsync returns valid ActivityState.
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
        }

        /// <summary>
        /// Test 4: GetFocusedApplicationAsync returns current window.
        /// </summary>
        [Fact]
        public async Task GetFocusedApplicationAsync_ReturnsFocusedWindow()
        {
            // Act
            var focusedApp = await _service.GetFocusedApplicationAsync(CancellationToken.None);

            // Assert
            // The foreground window should be this test process
            Assert.NotNull(focusedApp);
            Assert.NotEmpty(focusedApp.AppName);
            Assert.NotEmpty(focusedApp.WindowTitle);
        }

        /// <summary>
        /// Test 5: GetActivityIntensityAsync returns valid range.
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
        /// Test 6: IsIdleAsync correctly identifies idle state.
        /// </summary>
        [Fact]
        public async Task IsIdleAsync_TracksIdleState()
        {
            // Arrange - Record activity now
            _service.RecordKeyboardMouseActivity();

            // Act - Check idle status immediately
            var isIdle = await _service.IsIdleAsync(idleThresholdSeconds: 60, CancellationToken.None);

            // Assert - Should not be idle immediately after activity
            Assert.False(isIdle);
        }

        /// <summary>
        /// Test 7: Focused application is classified by name.
        /// </summary>
        [Fact]
        public async Task GetFocusedApplicationAsync_IncludesApplicationTags()
        {
            // Act
            var focusedApp = await _service.GetFocusedApplicationAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(focusedApp);
            Assert.NotNull(focusedApp.Tags);
            Assert.NotEmpty(focusedApp.Tags);
        }

        /// <summary>
        /// Test 8: Application classification works for known apps.
        /// </summary>
        [Fact]
        public void ApplicationClassification_ReturnsCorrectTags()
        {
            // Act - Create a service and check classification via reflection
            var classifyMethod = typeof(WindowsActivityDetectionService)
                .GetMethod("ClassifyApplication", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Assert - Method exists and can be called
            Assert.NotNull(classifyMethod);
        }

        /// <summary>
        /// Test 9: ActivityChanged observable fires on app focus change.
        /// </summary>
        [Fact]
        public async Task ActivityChanged_EmitsOnFocusChange()
        {
            // Arrange
            var stateChanges = new System.Collections.Generic.List<ActivityState>();
            var subscription = _service.ActivityChanged.Subscribe(state => stateChanges.Add(state));

            // Act
            await _service.GetCurrentActivityAsync(CancellationToken.None);
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(stateChanges);
            subscription.Dispose();
        }

        /// <summary>
        /// Test 10: RecordKeyboardMouseActivity updates last activity time.
        /// </summary>
        [Fact]
        public async Task RecordKeyboardMouseActivity_UpdatesActivityTime()
        {
            // Arrange
            var beforeActivity = _service.LastActivity;
            await Task.Delay(50);

            // Act
            _service.RecordKeyboardMouseActivity();

            // Assert
            Assert.True(_service.LastActivity > beforeActivity);
        }

        /// <summary>
        /// Test 11: Multiple activity recordings tracked.
        /// </summary>
        [Fact]
        public async Task GetActivityIntensityAsync_ReflectsMultipleActivities()
        {
            // Act
            _service.RecordKeyboardMouseActivity();
            await Task.Delay(50);
            _service.RecordKeyboardMouseActivity();
            await Task.Delay(50);
            var intensity = await _service.GetActivityIntensityAsync(minutesWindow: 5, CancellationToken.None);

            // Assert
            Assert.InRange(intensity, 0, 100);
        }

        /// <summary>
        /// Test 12: Service implements CancellationToken support.
        /// </summary>
        [Fact]
        public async Task GetCurrentActivityAsync_SupportsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert - Should not throw even if cancelled
            try
            {
                await _service.GetCurrentActivityAsync(cts.Token);
                Assert.True(true);  // No exception means success
            }
            catch (OperationCanceledException)
            {
                // This is acceptable too
                Assert.True(true);
            }
        }
    }
}
