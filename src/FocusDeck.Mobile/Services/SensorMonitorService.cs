using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls;

namespace FocusDeck.Mobile.Services;

/// <summary>
/// Service for monitoring phone sensors during focus sessions
/// </summary>
public interface ISensorMonitorService
{
    /// <summary>
    /// Start monitoring sensors and sending signals to server
    /// </summary>
    Task StartMonitoringAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop monitoring sensors
    /// </summary>
    Task StopMonitoringAsync();

    /// <summary>
    /// Get current motion level (0.0 to 1.0)
    /// </summary>
    double GetMotionLevel();

    /// <summary>
    /// Get screen state (true if on, false if off)
    /// </summary>
    bool GetScreenState();

    /// <summary>
    /// Get ambient light level (0.0 to 1.0)
    /// </summary>
    double GetLightLevel();
}

/// <summary>
/// Full implementation of sensor monitor service using MAUI
/// </summary>
public class SensorMonitorService : ISensorMonitorService, IDisposable
{
    private readonly ILogger<SensorMonitorService> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;
    private Guid _currentSessionId;
    private double _motionLevel;
    private bool _screenState = true;
    private double _lightLevel = 0.5;
    private bool _disposed;

    public SensorMonitorService(
        ILogger<SensorMonitorService> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public Task StartMonitoringAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_monitoringTask != null)
        {
            _logger.LogWarning("Sensor monitoring already started");
            return Task.CompletedTask;
        }

        _currentSessionId = sessionId;
        _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // Start accelerometer monitoring
        StartAccelerometerMonitoring();
        
        // Start sensor monitoring loop
        _monitoringTask = MonitorSensorsLoop(_monitoringCts.Token);

        _logger.LogInformation("Sensor monitoring started for session {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    public async Task StopMonitoringAsync()
    {
        if (_monitoringCts != null)
        {
            _monitoringCts.Cancel();
            
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when canceling
                }
            }

            _monitoringCts.Dispose();
            _monitoringCts = null;
            _monitoringTask = null;
        }

        // Stop accelerometer monitoring
        StopAccelerometerMonitoring();

        _logger.LogInformation("Sensor monitoring stopped");
    }

    public double GetMotionLevel()
    {
        return _motionLevel;
    }

    public bool GetScreenState()
    {
        // Get current screen state from platform
        try
        {
#if ANDROID
            var powerManager = (Android.OS.PowerManager?)Android.App.Application.Context.GetSystemService(Android.Content.Context.PowerService);
            _screenState = powerManager?.IsInteractive ?? true;
#elif IOS || MACCATALYST
            _screenState = UIKit.UIScreen.MainScreen.Brightness > 0;
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting screen state");
        }

        return _screenState;
    }

    public double GetLightLevel()
    {
        // Light sensor would require platform-specific implementation
        // For now, return estimated value
        return _lightLevel;
    }

    private void StartAccelerometerMonitoring()
    {
        try
        {
            if (Accelerometer.Default.IsSupported)
            {
                Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.UI);
                _logger.LogInformation("Accelerometer monitoring started");
            }
            else
            {
                _logger.LogWarning("Accelerometer not supported on this device");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start accelerometer monitoring");
        }
    }

    private void StopAccelerometerMonitoring()
    {
        try
        {
            if (Accelerometer.Default.IsSupported && Accelerometer.Default.IsMonitoring)
            {
                Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
                Accelerometer.Default.Stop();
                _logger.LogInformation("Accelerometer monitoring stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping accelerometer monitoring");
        }
    }

    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var reading = e.Reading;
        
        // Calculate motion magnitude
        var magnitude = Math.Sqrt(
            reading.Acceleration.X * reading.Acceleration.X +
            reading.Acceleration.Y * reading.Acceleration.Y +
            reading.Acceleration.Z * reading.Acceleration.Z
        );

        // Normalize to 0-1 range (threshold at ~1.2g, as 1g is gravity)
        _motionLevel = Math.Min(Math.Max(magnitude - 1.0, 0.0) / 0.2, 1.0);
    }

    private async Task MonitorSensorsLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait 10 seconds between signal submissions
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                // Send signals to server
                await SendSignalsToServer(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sensor monitoring cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sensor monitoring loop");
        }
    }

    private async Task SendSignalsToServer(CancellationToken cancellationToken)
    {
        if (_httpClientFactory == null)
        {
            _logger.LogDebug("No HTTP client factory configured, skipping signal submission");
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("FocusDeckApi");
            var deviceId = DeviceInfo.Current.Name;

            var signals = new[]
            {
                new
                {
                    deviceId,
                    kind = "PhoneMotion",
                    value = GetMotionLevel(),
                    timestamp = DateTime.UtcNow
                },
                new
                {
                    deviceId,
                    kind = "PhoneScreen",
                    value = GetScreenState() ? 1.0 : 0.0,
                    timestamp = DateTime.UtcNow
                }
            };

            foreach (var signal in signals)
            {
                try
                {
                    var response = await httpClient.PostAsJsonAsync(
                        "/v1/focus/signals",
                        signal,
                        cancellationToken
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug("Signal {Kind} sent successfully", signal.kind);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send signal {Kind}: {StatusCode}",
                            signal.kind, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send signal {Kind}", signal.kind);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signals to server");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopMonitoringAsync().GetAwaiter().GetResult();
        _monitoringCts?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Service for handling focus notifications on mobile device
/// </summary>
public interface IFocusNotificationService
{
    /// <summary>
    /// Show notification prompting user to silence notifications
    /// </summary>
    Task ShowSilenceNotificationPromptAsync();

    /// <summary>
    /// Show distraction alert
    /// </summary>
    Task ShowDistractionAlertAsync(string reason);

    /// <summary>
    /// Show recovery suggestion
    /// </summary>
    Task ShowRecoverySuggestionAsync(string suggestion);
}

/// <summary>
/// Full implementation of focus notification service using MAUI
/// </summary>
public class FocusNotificationService : IFocusNotificationService
{
    private readonly ILogger<FocusNotificationService> _logger;

    public FocusNotificationService(ILogger<FocusNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task ShowSilenceNotificationPromptAsync()
    {
        try
        {
            var result = await Application.Current!.MainPage!.DisplayAlert(
                "Focus Mode",
                "Silence notifications now?",
                "Yes",
                "No"
            );

            if (result)
            {
#if ANDROID
                // Request Do Not Disturb permission
                var notificationManager = (Android.App.NotificationManager?)
                    Android.App.Application.Context.GetSystemService(
                        Android.Content.Context.NotificationService
                    );

                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                {
                    if (notificationManager != null && !notificationManager.IsNotificationPolicyAccessGranted)
                    {
                        var intent = new Android.Content.Intent(
                            Android.Provider.Settings.ActionNotificationPolicyAccessSettings
                        );
                        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.StartActivity(intent);
                    }
                    else if (notificationManager != null)
                    {
                        notificationManager.SetInterruptionFilter(
                            Android.App.InterruptionFilter.None
                        );
                    }
                }
#elif IOS || MACCATALYST
                // On iOS, guide user to Control Center
                await Application.Current!.MainPage!.DisplayAlert(
                    "Enable Do Not Disturb",
                    "Swipe down from top-right and tap the moon icon to enable Do Not Disturb.",
                    "OK"
                );
#endif
            }

            _logger.LogInformation("Silence notification prompt shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing silence notification prompt");
        }
    }

    public async Task ShowDistractionAlertAsync(string reason)
    {
        try
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "⚠️ Distraction Detected",
                reason,
                "OK"
            );

            _logger.LogInformation("Distraction alert shown: {Reason}", reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing distraction alert");
        }
    }

    public async Task ShowRecoverySuggestionAsync(string suggestion)
    {
        try
        {
            var action = await Application.Current!.MainPage!.DisplayActionSheet(
                "Focus Recovery",
                "Cancel",
                null,
                "Take 2-min break",
                "Enable Lock Mode",
                "Snooze for 10 min"
            );

            if (action != null && action != "Cancel")
            {
                _logger.LogInformation("Recovery action selected: {Action}", action);
                // Handle the action - this would typically call back to a service
                // that manages the focus session
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing recovery suggestion");
        }
    }
}
}
