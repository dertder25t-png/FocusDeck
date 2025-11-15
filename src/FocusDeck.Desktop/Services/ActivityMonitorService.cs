using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Desktop.Services.Privacy;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace FocusDeck.Desktop.Services;

/// <summary>
/// Activity monitor for tracking keyboard, mouse, and ambient noise
/// </summary>
public interface IActivityMonitorService
{
    /// <summary>
    /// Start monitoring activity
    /// </summary>
    Task StartMonitoringAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop monitoring activity
    /// </summary>
    Task StopMonitoringAsync();

    /// <summary>
    /// Get keyboard idle time in seconds
    /// </summary>
    int GetKeyboardIdleSeconds();

    /// <summary>
    /// Get mouse idle time in seconds
    /// </summary>
    int GetMouseIdleSeconds();

    /// <summary>
    /// Get ambient noise level (0.0 to 1.0)
    /// </summary>
    double GetAmbientNoiseLevel();

    /// <summary>
    /// Event fired when activity is detected
    /// </summary>
    event EventHandler<ActivityDetectedEventArgs>? ActivityDetected;
}

/// <summary>
/// Event args for activity detection
/// </summary>
public class ActivityDetectedEventArgs : EventArgs
{
    public string ActivityType { get; set; } = string.Empty; // "Keyboard", "Mouse", "AmbientNoise"
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Full implementation of activity monitor using Windows APIs and NAudio
/// </summary>
public class ActivityMonitorService : IActivityMonitorService, IDisposable
{
    private readonly ILogger<ActivityMonitorService> _logger;
    private readonly IApiClient _apiClient;
    private readonly ISensorPrivacyGate _privacyGate;
    private readonly string _deviceId;
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;
    private WaveInEvent? _waveIn;
    private double _currentNoiseLevel;
    private Guid _currentSessionId;
    private bool _disposed;

    // Windows API interop
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    public event EventHandler<ActivityDetectedEventArgs>? ActivityDetected;

    public ActivityMonitorService(
        ILogger<ActivityMonitorService> logger,
        IApiClient apiClient,
        ISensorPrivacyGate privacyGate)
    {
        _logger = logger;
        _apiClient = apiClient;
        _privacyGate = privacyGate;
        _deviceId = Environment.MachineName;
    }

    public Task StartMonitoringAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_monitoringTask != null)
        {
            _logger.LogWarning("Activity monitoring already started");
            return Task.CompletedTask;
        }

        _currentSessionId = sessionId;
        _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // Start ambient noise monitoring
        StartAmbientNoiseMonitoring();
        
        // Start activity monitoring loop
        _monitoringTask = MonitorActivityLoop(_monitoringCts.Token);

        _logger.LogInformation("Activity monitoring started for session {SessionId}", sessionId);
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

        // Stop ambient noise monitoring
        StopAmbientNoiseMonitoring();

        _logger.LogInformation("Activity monitoring stopped");
    }

    public int GetKeyboardIdleSeconds()
    {
        return GetIdleTimeSeconds();
    }

    public int GetMouseIdleSeconds()
    {
        return GetIdleTimeSeconds();
    }

    private int GetIdleTimeSeconds()
    {
        try
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);

            if (GetLastInputInfo(ref lastInput))
            {
                uint idleTime = (uint)Environment.TickCount - lastInput.dwTime;
                return (int)(idleTime / 1000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting idle time");
        }

        return 0;
    }

    public double GetAmbientNoiseLevel()
    {
        return _currentNoiseLevel;
    }

    private void StartAmbientNoiseMonitoring()
    {
        try
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(44100, 1), // 44.1kHz, mono
                BufferMilliseconds = 50
            };

            _waveIn.DataAvailable += OnAudioDataAvailable;
            _waveIn.StartRecording();

            _logger.LogInformation("Ambient noise monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start ambient noise monitoring");
        }
    }

    private void StopAmbientNoiseMonitoring()
    {
        if (_waveIn != null)
        {
            try
            {
                _waveIn.StopRecording();
                _waveIn.DataAvailable -= OnAudioDataAvailable;
                _waveIn.Dispose();
                _waveIn = null;

                _logger.LogInformation("Ambient noise monitoring stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping ambient noise monitoring");
            }
        }
    }

    private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
    {
        // Calculate RMS (Root Mean Square) level
        float max = 0;
        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i + 0]);
            float sample32 = sample / 32768f;
            float absSample = Math.Abs(sample32);
            if (absSample > max) max = absSample;
        }

        // Normalize to 0.0-1.0 range
        _currentNoiseLevel = Math.Min(max, 1.0);
    }

    private async Task MonitorActivityLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait 10 seconds between signal submissions
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                // Send signals to server
                await SendSignalsToServer(cancellationToken);

                // Fire events for local handling
                FireActivityEvents();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Activity monitoring cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in activity monitoring loop");
        }
    }

    private async Task SendSignalsToServer(CancellationToken cancellationToken)
    {
        var typingValue = GetKeyboardIdleSeconds() == 0 ? "active" : "idle";
        var mouseValue = GetMouseIdleSeconds() == 0 ? "active" : "idle";
        var noiseValue = GetAmbientNoiseLevel().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        await SendSignalAsync("TypingVelocity", typingValue, new { Sensor = "keyboard", DeviceId = _deviceId }, cancellationToken);
        await SendSignalAsync("MouseEntropy", mouseValue, new { Sensor = "mouse", DeviceId = _deviceId }, cancellationToken);
        await SendSignalAsync("AmbientNoise", noiseValue, new { Sensor = "microphone", DeviceId = _deviceId }, cancellationToken);
    }

    private async Task SendSignalAsync(string signalType, string signalValue, object metadata, CancellationToken cancellationToken)
    {
        if (!await _privacyGate.IsEnabledAsync(signalType, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null;
        var signal = new ActivitySignalDto(signalType, signalValue, "FocusDeck.Desktop", DateTime.UtcNow, metadataJson);

        try
        {
            await _apiClient.PostAsync<object>("/v1/activity/signals", signal, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Activity signal {SignalType} sent", signalType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send activity signal {SignalType}", signalType);
        }
    }

    private void FireActivityEvents()
    {
        var keyboardIdle = GetKeyboardIdleSeconds();
        var mouseIdle = GetMouseIdleSeconds();
        var noiseLevel = GetAmbientNoiseLevel();

        if (keyboardIdle == 0)
        {
            ActivityDetected?.Invoke(this, new ActivityDetectedEventArgs
            {
                ActivityType = "Keyboard",
                Value = 1.0,
                Timestamp = DateTime.UtcNow
            });
        }

        if (mouseIdle == 0)
        {
            ActivityDetected?.Invoke(this, new ActivityDetectedEventArgs
            {
                ActivityType = "Mouse",
                Value = 1.0,
                Timestamp = DateTime.UtcNow
            });
        }

        if (noiseLevel > 0.1)
        {
            ActivityDetected?.Invoke(this, new ActivityDetectedEventArgs
            {
                ActivityType = "AmbientNoise",
                Value = noiseLevel,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopMonitoringAsync().GetAwaiter().GetResult();
        _monitoringCts?.Dispose();
        _waveIn?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Service for managing focus overlay (dim effect) using WPF
/// </summary>
public interface IFocusOverlayService
{
    /// <summary>
    /// Show dim overlay when focus session is paused
    /// </summary>
    void ShowDimOverlay();

    /// <summary>
    /// Hide dim overlay when focus session resumes
    /// </summary>
    void HideDimOverlay();

    /// <summary>
    /// Show focus recovery banner with action buttons
    /// </summary>
    void ShowRecoveryBanner(string suggestion);

    /// <summary>
    /// Hide recovery banner
    /// </summary>
    void HideRecoveryBanner();

    /// <summary>
    /// Event fired when user takes a recovery action
    /// </summary>
    event EventHandler<RecoveryActionEventArgs>? RecoveryActionTaken;
}

/// <summary>
/// Event args for recovery actions
/// </summary>
public class RecoveryActionEventArgs : EventArgs
{
    public string Action { get; set; } = string.Empty; // "Take 2-min break", "Enable Lock Mode", "Snooze"
}

/// <summary>
/// Full implementation of focus overlay service using WPF
/// </summary>
public class FocusOverlayService : IFocusOverlayService
{
    private readonly ILogger<FocusOverlayService> _logger;
    private System.Windows.Window? _overlayWindow;
    private System.Windows.Window? _bannerWindow;

    public event EventHandler<RecoveryActionEventArgs>? RecoveryActionTaken;

    public FocusOverlayService(ILogger<FocusOverlayService> logger)
    {
        _logger = logger;
    }

    public void ShowDimOverlay()
    {
        try
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_overlayWindow != null)
                {
                    _overlayWindow.Show();
                    return;
                }

                _overlayWindow = new System.Windows.Window
                {
                    WindowStyle = System.Windows.WindowStyle.None,
                    ResizeMode = System.Windows.ResizeMode.NoResize,
                    Topmost = true,
                    ShowInTaskbar = false,
                    Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(128, 0, 0, 0)),
                    AllowsTransparency = true,
                    WindowState = System.Windows.WindowState.Maximized
                };

                _overlayWindow.Show();
                _logger.LogInformation("Dim overlay shown");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing dim overlay");
        }
    }

    public void HideDimOverlay()
    {
        try
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_overlayWindow != null)
                {
                    _overlayWindow.Hide();
                    _logger.LogInformation("Dim overlay hidden");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding dim overlay");
        }
    }

    public void ShowRecoveryBanner(string suggestion)
    {
        try
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_bannerWindow != null)
                {
                    _bannerWindow.Close();
                }

                _bannerWindow = CreateRecoveryBannerWindow(suggestion);
                _bannerWindow.Show();
                _logger.LogInformation("Recovery banner shown: {Suggestion}", suggestion);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing recovery banner");
        }
    }

    public void HideRecoveryBanner()
    {
        try
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_bannerWindow != null)
                {
                    _bannerWindow.Close();
                    _bannerWindow = null;
                    _logger.LogInformation("Recovery banner hidden");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding recovery banner");
        }
    }

    private System.Windows.Window CreateRecoveryBannerWindow(string suggestion)
    {
        var window = new System.Windows.Window
        {
            WindowStyle = System.Windows.WindowStyle.None,
            ResizeMode = System.Windows.ResizeMode.NoResize,
            Topmost = true,
            ShowInTaskbar = false,
            SizeToContent = System.Windows.SizeToContent.WidthAndHeight,
            WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(240, 30, 30, 30))
        };

        // Position at top center of screen
        var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
        window.Left = (screenWidth - 600) / 2;
        window.Top = 20;

        // Create content
        var stackPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Vertical,
            Margin = new System.Windows.Thickness(20)
        };

        // Title
        var titleLabel = new System.Windows.Controls.Label
        {
            Content = "🎯 Focus Recovery",
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            Margin = new System.Windows.Thickness(0, 0, 0, 10)
        };
        stackPanel.Children.Add(titleLabel);

        // Suggestion text
        var suggestionLabel = new System.Windows.Controls.Label
        {
            Content = $"Suggestion: {suggestion}",
            Foreground = System.Windows.Media.Brushes.LightGray,
            FontSize = 14,
            Margin = new System.Windows.Thickness(0, 0, 0, 15)
        };
        stackPanel.Children.Add(suggestionLabel);

        // Buttons
        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };

        var breakButton = CreateActionButton("Take 2-min break", window);
        var lockButton = CreateActionButton("Enable Lock Mode", window);
        var snoozeButton = CreateActionButton("Snooze", window);

        buttonPanel.Children.Add(breakButton);
        buttonPanel.Children.Add(lockButton);
        buttonPanel.Children.Add(snoozeButton);

        stackPanel.Children.Add(buttonPanel);

        window.Content = stackPanel;

        return window;
    }

    private System.Windows.Controls.Button CreateActionButton(string action, System.Windows.Window window)
    {
        var button = new System.Windows.Controls.Button
        {
            Content = action,
            Padding = new System.Windows.Thickness(15, 8, 15, 8),
            Margin = new System.Windows.Thickness(5),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 212)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new System.Windows.Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 12
        };

        button.Click += (s, e) =>
        {
            RecoveryActionTaken?.Invoke(this, new RecoveryActionEventArgs { Action = action });
            window.Close();
        };

        return button;
    }
}
