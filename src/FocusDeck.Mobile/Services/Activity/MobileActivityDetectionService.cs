using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Services.Activity;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;

#if NET8_0_ANDROID

namespace FocusDeck.Mobile.Services.Activity
{
    /// <summary>
    /// Mobile (MAUI) activity detection using accelerometer and gyroscope.
    /// Tracks device motion and app state for focus detection.
    /// </summary>
    public class MobileActivityDetectionService : ActivityDetectionService
    {
        private int _motionCount;
        private DateTime _lastMotionTime = DateTime.UtcNow;
        private Queue<DateTime> _motionHistory = new();
        private const int MOTION_THRESHOLD = 10;
        private const int MOTION_HISTORY_CAPACITY = 50;
        private bool _isForeground = true;

        public MobileActivityDetectionService(ILogger<MobileActivityDetectionService> logger)
            : base(logger)
        {
            // Reset motion count periodically
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(60000);  // Reset every minute
                    _motionCount = 0;
                }
            });

            // Monitor app foreground/background
            Application.Current!.Paused += OnAppPaused;
            Application.Current.Resumed += OnAppResumed;

            // Start sensor monitoring
            InitializeSensors();
        }

        /// <summary>
        /// Initialize accelerometer and gyroscope sensors.
        /// </summary>
        private void InitializeSensors()
        {
            try
            {
                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.Default);
                    _logger.LogInformation("Accelerometer initialized");
                }

                if (Gyroscope.Default.IsSupported)
                {
                    Gyroscope.Default.ReadingChanged += OnGyroscopeReadingChanged;
                    Gyroscope.Default.Start(SensorSpeed.Default);
                    _logger.LogInformation("Gyroscope initialized");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize sensors");
            }
        }

        /// <summary>
        /// Handle accelerometer readings (device motion).
        /// </summary>
        private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            try
            {
                var acceleration = e.Reading;
                var magnitude = Math.Sqrt(
                    acceleration.Acceleration.X * acceleration.Acceleration.X +
                    acceleration.Acceleration.Y * acceleration.Acceleration.Y +
                    acceleration.Acceleration.Z * acceleration.Acceleration.Z
                );

                // Detect significant motion (threshold to filter noise)
                if (magnitude > MOTION_THRESHOLD)
                {
                    _motionCount++;
                    _lastMotionTime = DateTime.UtcNow;
                    _motionHistory.Enqueue(DateTime.UtcNow);

                    if (_motionHistory.Count > MOTION_HISTORY_CAPACITY)
                        _motionHistory.Dequeue();

                    RecordActivity();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing accelerometer reading");
            }
        }

        /// <summary>
        /// Handle gyroscope readings (device rotation).
        /// </summary>
        private void OnGyroscopeReadingChanged(object? sender, GyroscopeChangedEventArgs e)
        {
            try
            {
                var rotation = e.Reading;
                var magnitude = Math.Sqrt(
                    rotation.AngularVelocity.X * rotation.AngularVelocity.X +
                    rotation.AngularVelocity.Y * rotation.AngularVelocity.Y +
                    rotation.AngularVelocity.Z * rotation.AngularVelocity.Z
                );

                // Detect significant rotation
                if (magnitude > MOTION_THRESHOLD)
                {
                    _motionCount++;
                    _lastMotionTime = DateTime.UtcNow;
                    _motionHistory.Enqueue(DateTime.UtcNow);

                    if (_motionHistory.Count > MOTION_HISTORY_CAPACITY)
                        _motionHistory.Dequeue();

                    RecordActivity();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing gyroscope reading");
            }
        }

        /// <summary>
        /// App went to background.
        /// </summary>
        private void OnAppPaused()
        {
            _isForeground = false;
            _logger.LogInformation("App paused - backgrounded");

            try
            {
                Accelerometer.Default.Stop();
                Gyroscope.Default.Stop();
            }
            catch { /* Ignore errors */ }
        }

        /// <summary>
        /// App resumed to foreground.
        /// </summary>
        private void OnAppResumed()
        {
            _isForeground = true;
            _logger.LogInformation("App resumed - foregrounded");

            try
            {
                Accelerometer.Default.Start(SensorSpeed.Default);
                Gyroscope.Default.Start(SensorSpeed.Default);
            }
            catch { /* Ignore errors */ }
        }

        /// <summary>
        /// On mobile, current focused app is always FocusDeck when in foreground.
        /// </summary>
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

        /// <summary>
        /// Activity intensity based on motion history and recency.
        /// </summary>
        protected override Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct)
        {
            try
            {
                int intensity = 0;

                // Clean up old motion history
                var cutoffTime = DateTime.UtcNow.AddMinutes(-minutesWindow);
                var activeMotions = new Queue<DateTime>();
                foreach (var motionTime in _motionHistory)
                {
                    if (motionTime >= cutoffTime)
                        activeMotions.Enqueue(motionTime);
                }

                // Motion history contributes to intensity
                intensity = Math.Min(activeMotions.Count * 3, 50);

                // Recent activity boost
                if (DateTime.UtcNow - _lastMotionTime < TimeSpan.FromSeconds(10))
                    intensity += 30;

                // Motion count (resets every minute)
                intensity += Math.Min(_motionCount * 5, 40);

                // Device orientation changes contribute
                if (_isForeground)
                    intensity += 10;

                return Task.FromResult(Math.Min(intensity, 100));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate activity intensity");
                return Task.FromResult(0);
            }
        }

        /// <summary>
        /// Cleanup: stop sensors and unsubscribe from events.
        /// </summary>
        ~MobileActivityDetectionService()
        {
            try
            {
                if (Accelerometer.Default.IsSupported)
                    Accelerometer.Default.Stop();

                if (Gyroscope.Default.IsSupported)
                    Gyroscope.Default.Stop();

                if (Application.Current != null)
                {
                    Application.Current.Paused -= OnAppPaused;
                    Application.Current.Resumed -= OnAppResumed;
                }
            }
            catch { /* Ignore cleanup errors */ }
        }
    }
}

#endif
