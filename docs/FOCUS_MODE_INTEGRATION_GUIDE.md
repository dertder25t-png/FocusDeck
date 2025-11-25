# Focus Mode Integration Guide

This guide provides detailed instructions for integrating Focus Mode features into Desktop and Mobile clients.

## Table of Contents
- [Database Setup](#database-setup)
- [SignalR Integration](#signalr-integration)
- [Desktop Integration](#desktop-integration)
- [Mobile Integration](#mobile-integration)
- [API Reference](#api-reference)

## Database Setup

### Running Migrations

The `AddFocusSessions` migration creates the necessary database schema. To apply it:

#### SQLite (Development/Testing)
```bash
cd src/FocusDeck.Persistence
dotnet ef database update --context AutomationDbContext
```

#### PostgreSQL (Production)
```bash
# Update connection string in appsettings.json
# Then run:
cd src/FocusDeck.Server
dotnet ef database update --project ../FocusDeck.Persistence --context AutomationDbContext
```

The migration creates the `FocusSessions` table with the following schema:
- `Id` (GUID): Primary key
- `UserId` (VARCHAR): User identifier
- `StartTime` (DATETIME): Session start timestamp
- `EndTime` (DATETIME): Session end timestamp (nullable)
- `Status` (INT): Session status (Active=0, Paused=1, Completed=2, Canceled=3)
- `Policy` (TEXT): JSON policy configuration
- `Signals` (TEXT): JSON array of signals
- `DistractionsCount` (INT): Total distractions detected
- `LastRecoverySuggestionAt` (DATETIME): Last suggestion timestamp
- `CreatedAt`, `UpdatedAt` (DATETIME): Audit timestamps

Indexes are created for efficient querying on `UserId`, `Status`, `StartTime`, and composite `UserId+Status`.

## SignalR Integration

### Client Setup

Both Desktop and Mobile clients need to establish SignalR connections to receive real-time focus events.

#### .NET Client (Desktop WPF/MAUI)

```csharp
using Microsoft.AspNetCore.SignalR.Client;

public class FocusSignalRService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl;
    private readonly string _userId;

    public event EventHandler<DistractionEventArgs>? DistractionDetected;
    public event EventHandler<RecoverySuggestionEventArgs>? RecoverySuggested;

    public FocusSignalRService(string baseUrl, string userId)
    {
        _hubUrl = $"{baseUrl}/hubs/notifications";
        _userId = userId;
    }

    public async Task ConnectAsync()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Subscribe to focus events
        _connection.On<string, DateTime>("FocusDistraction", (reason, at) =>
        {
            DistractionDetected?.Invoke(this, new DistractionEventArgs
            {
                Reason = reason,
                DetectedAt = at
            });
        });

        _connection.On<string>("FocusRecoverySuggested", (suggestion) =>
        {
            RecoverySuggested?.Invoke(this, new RecoverySuggestionEventArgs
            {
                Suggestion = suggestion
            });
        });

        await _connection.StartAsync();

        // Join user-specific group to receive notifications
        await _connection.InvokeAsync("JoinUserGroup", _userId);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.InvokeAsync("LeaveUserGroup", _userId);
            await _connection.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}

public class DistractionEventArgs : EventArgs
{
    public string Reason { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}

public class RecoverySuggestionEventArgs : EventArgs
{
    public string Suggestion { get; set; } = string.Empty;
}
```

#### JavaScript Client (Web)

```javascript
// Import SignalR library
import * as signalR from "@microsoft/signalr";

class FocusSignalRService {
    constructor(baseUrl, userId) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/notifications`)
            .withAutomaticReconnect()
            .build();
        
        this.userId = userId;
        this.setupHandlers();
    }

    setupHandlers() {
        // Handle distraction events
        this.connection.on("FocusDistraction", (reason, at) => {
            console.log(`Distraction detected: ${reason} at ${at}`);
            this.onDistraction(reason, new Date(at));
        });

        // Handle recovery suggestions
        this.connection.on("FocusRecoverySuggested", (suggestion) => {
            console.log(`Recovery suggested: ${suggestion}`);
            this.onRecoverySuggestion(suggestion);
        });
    }

    async connect() {
        await this.connection.start();
        console.log("SignalR connected");

        // Join user group
        await this.connection.invoke("JoinUserGroup", this.userId);
    }

    async disconnect() {
        await this.connection.invoke("LeaveUserGroup", this.userId);
        await this.connection.stop();
    }

    // Override these in your implementation
    onDistraction(reason, detectedAt) {
        // Show distraction alert UI
    }

    onRecoverySuggestion(suggestion) {
        // Show recovery banner with action buttons
    }
}
```

## Desktop Integration

### Step 1: Implement ActivityMonitorService

Complete the stub implementation in `src/FocusDeck.Desktop/Services/ActivityMonitorService.cs`:

```csharp
// Add Windows API interop
[DllImport("user32.dll")]
static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

[StructLayout(LayoutKind.Sequential)]
struct LASTINPUTINFO
{
    public uint cbSize;
    public uint dwTime;
}

public int GetKeyboardIdleSeconds()
{
    LASTINPUTINFO lastInput = new LASTINPUTINFO();
    lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);
    
    if (GetLastInputInfo(ref lastInput))
    {
        uint idleTime = (uint)Environment.TickCount - lastInput.dwTime;
        return (int)(idleTime / 1000);
    }
    
    return 0;
}

// For ambient noise, add NAudio package:
// dotnet add package NAudio

using NAudio.Wave;

private WaveInEvent? _waveIn;
private double _currentNoiseLevel;

private void StartAmbientNoiseMonitoring()
{
    _waveIn = new WaveInEvent
    {
        WaveFormat = new WaveFormat(44100, 1)
    };

    _waveIn.DataAvailable += (sender, args) =>
    {
        // Calculate RMS level
        float max = 0;
        for (int i = 0; i < args.BytesRecorded; i += 2)
        {
            short sample = (short)((args.Buffer[i + 1] << 8) | args.Buffer[i]);
            float sample32 = sample / 32768f;
            if (sample32 > max) max = sample32;
        }
        _currentNoiseLevel = max;
    };

    _waveIn.StartRecording();
}

public double GetAmbientNoiseLevel()
{
    return _currentNoiseLevel;
}
```

### Step 2: Send Signals to Server

```csharp
private readonly HttpClient _httpClient;
private readonly string _deviceId;

private async Task SendSignalsToServer(CancellationToken cancellationToken)
{
    var signals = new[]
    {
        new
        {
            deviceId = _deviceId,
            kind = "Keyboard",
            value = GetKeyboardIdleSeconds() == 0 ? 1.0 : 0.0,
            timestamp = DateTime.UtcNow
        },
        new
        {
            deviceId = _deviceId,
            kind = "Mouse",
            value = GetMouseIdleSeconds() == 0 ? 1.0 : 0.0,
            timestamp = DateTime.UtcNow
        },
        new
        {
            deviceId = _deviceId,
            kind = "AmbientNoise",
            value = GetAmbientNoiseLevel(),
            timestamp = DateTime.UtcNow
        }
    };

    foreach (var signal in signals)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/v1/focus/signals",
                signal,
                cancellationToken
            );
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send signal {Kind}", signal.kind);
        }
    }
}
```

### Step 3: Implement FocusOverlayService

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public class FocusOverlayWindow : Window
{
    public FocusOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        
        // Fill the screen
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }
}

public void ShowDimOverlay()
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        _overlayWindow = new FocusOverlayWindow();
        _overlayWindow.Show();
    });
}

public void ShowRecoveryBanner(string suggestion)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        var banner = new RecoveryBannerWindow(suggestion);
        banner.ActionClicked += (s, action) =>
        {
            HandleRecoveryAction(action);
        };
        banner.Show();
    });
}

private async void HandleRecoveryAction(string action)
{
    switch (action)
    {
        case "Take 2-min break":
            await StartBreakTimer(TimeSpan.FromMinutes(2));
            break;
        case "Enable Lock Mode":
            await EnableStrictMode();
            break;
        case "Snooze":
            // Snooze for 10 minutes
            break;
    }
}
```

## Mobile Integration

### Step 1: Implement SensorMonitorService

For MAUI, use built-in sensor APIs:

```csharp
using Microsoft.Maui.Devices.Sensors;

public class SensorMonitorService : ISensorMonitorService
{
    private readonly IAccelerometer _accelerometer;
    private double _motionLevel;

    public SensorMonitorService()
    {
        _accelerometer = Accelerometer.Default;
    }

    public async Task StartMonitoringAsync(Guid sessionId, CancellationToken ct)
    {
        // Start accelerometer
        if (_accelerometer.IsSupported)
        {
            _accelerometer.ReadingChanged += OnAccelerometerReadingChanged;
            _accelerometer.Start(SensorSpeed.UI);
        }

        // Monitor screen state (platform-specific)
        #if ANDROID
        StartAndroidScreenMonitoring();
        #elif IOS
        StartIOSScreenMonitoring();
        #endif

        // Start signal sending loop
        _monitoringTask = SendSignalsLoop(sessionId, ct);
    }

    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var reading = e.Reading;
        // Calculate motion magnitude
        _motionLevel = Math.Sqrt(
            reading.Acceleration.X * reading.Acceleration.X +
            reading.Acceleration.Y * reading.Acceleration.Y +
            reading.Acceleration.Z * reading.Acceleration.Z
        );
    }

    public double GetMotionLevel()
    {
        // Normalize to 0-1 range (threshold at ~1.2g)
        return Math.Min(_motionLevel / 1.2, 1.0);
    }

    #if ANDROID
    private void StartAndroidScreenMonitoring()
    {
        var powerManager = (Android.OS.PowerManager?)
            Android.App.Application.Context.GetSystemService(
                Android.Content.Context.PowerService
            );
        
        // Check screen state periodically
        _screenState = powerManager?.IsInteractive ?? true;
    }
    #endif

    #if IOS
    private void StartIOSScreenMonitoring()
    {
        // iOS screen state monitoring
        _screenState = UIKit.UIScreen.MainScreen.Brightness > 0;
    }
    #endif
}
```

### Step 2: Send Signals to Server

```csharp
private async Task SendSignalsLoop(Guid sessionId, CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct);

        var signals = new[]
        {
            new SignalDto
            {
                DeviceId = DeviceInfo.Current.Name,
                Kind = "PhoneMotion",
                Value = GetMotionLevel(),
                Timestamp = DateTime.UtcNow
            },
            new SignalDto
            {
                DeviceId = DeviceInfo.Current.Name,
                Kind = "PhoneScreen",
                Value = GetScreenState() ? 1.0 : 0.0,
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var signal in signals)
        {
            try
            {
                await _httpClient.PostAsJsonAsync("/v1/focus/signals", signal, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send signal");
            }
        }
    }
}
```

### Step 3: Implement FocusNotificationService

```csharp
public async Task ShowSilenceNotificationPromptAsync()
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
        
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            if (!notificationManager!.IsNotificationPolicyAccessGranted)
            {
                var intent = new Android.Content.Intent(
                    Android.Provider.Settings.ActionNotificationPolicyAccessSettings
                );
                Platform.CurrentActivity?.StartActivity(intent);
            }
            else
            {
                notificationManager.SetInterruptionFilter(
                    Android.App.InterruptionFilter.None
                );
            }
        }
        #elif IOS
        // On iOS, guide user to Control Center
        await Application.Current!.MainPage!.DisplayAlert(
            "Enable Do Not Disturb",
            "Swipe down from top-right and tap the moon icon to enable Do Not Disturb.",
            "OK"
        );
        #endif
    }
}

public async Task ShowRecoverySuggestionAsync(string suggestion)
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
        await HandleRecoveryAction(action);
    }
}
```

## API Reference

### Create Focus Session

```http
POST /v1/focus/sessions
Content-Type: application/json

{
  "policy": {
    "strict": true,
    "autoBreak": true,
    "autoDim": false,
    "notifyPhone": true
  }
}

Response: 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "test-user",
  "startTime": "2025-11-03T03:00:00Z",
  "endTime": null,
  "status": "Active",
  "policy": {
    "strict": true,
    "autoBreak": true,
    "autoDim": false,
    "notifyPhone": true
  },
  "distractionsCount": 0,
  "lastRecoverySuggestionAt": null,
  "createdAt": "2025-11-03T03:00:00Z",
  "updatedAt": "2025-11-03T03:00:00Z"
}
```

### Get Active Session

```http
GET /v1/focus/sessions/active

Response: 200 OK
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "test-user",
  "startTime": "2025-11-03T03:00:00Z",
  "status": "Active",
  ...
}

Response: 404 Not Found (if no active session)
{
  "error": "No active focus session found"
}
```

### Submit Signal

```http
POST /v1/focus/signals
Content-Type: application/json

{
  "deviceId": "desktop-001",
  "kind": "Keyboard",
  "value": 1.0,
  "timestamp": "2025-11-03T03:00:00Z"
}

Response: 200 OK
{
  "success": true,
  "distractionsCount": 0
}
```

**Signal Kinds:**
- `PhoneMotion`: Accelerometer motion (0.0 = still, 1.0 = high movement)
- `PhoneScreen`: Screen state (0.0 = off, 1.0 = on)
- `Keyboard`: Keyboard activity (0.0 = idle, 1.0 = active)
- `Mouse`: Mouse activity (0.0 = idle, 1.0 = active)
- `AmbientNoise`: Noise level (0.0 = silent, 1.0 = loud)

### End Session

```http
POST /v1/focus/sessions/{id}/end

Response: 200 OK
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Completed",
  "endTime": "2025-11-03T04:00:00Z",
  ...
}
```

### SignalR Events

**FocusDistraction**
```javascript
connection.on("FocusDistraction", (reason, at) => {
  // reason: "Phone motion detected" or "Phone screen activated"
  // at: ISO 8601 timestamp
});
```

**FocusRecoverySuggested**
```javascript
connection.on("FocusRecoverySuggested", (suggestion) => {
  // suggestion: "Take 2-min break" or "Enable Lock Mode"
});
```

## Testing Your Integration

### Unit Testing

```csharp
[Fact]
public async Task SignalSubmission_InActiveSession_RecordsSignal()
{
    // Arrange
    var client = _factory.CreateClient();
    var sessionResponse = await client.PostAsJsonAsync("/v1/focus/sessions", 
        new { policy = new { strict = true } });
    var session = await sessionResponse.Content.ReadFromJsonAsync<FocusSessionDto>();

    // Act
    var signalResponse = await client.PostAsJsonAsync("/v1/focus/signals", new
    {
        deviceId = "test-device",
        kind = "PhoneMotion",
        value = 0.8,
        timestamp = DateTime.UtcNow
    });

    // Assert
    Assert.Equal(HttpStatusCode.OK, signalResponse.StatusCode);
}
```

### Integration Testing

1. Start the server: `dotnet run --project src/FocusDeck.Server`
2. Connect a SignalR test client
3. Create a focus session
4. Submit signals with strict mode enabled
5. Verify distraction events are received
6. Submit 3+ signals to trigger recovery suggestion
7. Verify recovery suggestion event is received

## Troubleshooting

### SignalR Connection Issues

**Problem:** Client can't connect to SignalR hub

**Solution:** Ensure CORS is configured in `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### Notifications Not Received

**Problem:** SignalR events not received by client

**Solution:** Verify the client has joined the user group:
```csharp
await connection.InvokeAsync("JoinUserGroup", userId);
```

### Signal Submission Fails

**Problem:** HTTP 404 when submitting signals

**Solution:** Ensure an active focus session exists:
```http
GET /v1/focus/sessions/active
```

## Next Steps

1. Complete the platform-specific implementations for Desktop and Mobile
2. Add UI components for distraction alerts and recovery banners
3. Implement user preferences for notification settings
4. Add analytics dashboard for focus session insights
5. Integrate with calendar for automatic session scheduling

For questions or issues, please refer to the main [FOCUS_MODE_IMPLEMENTATION.md](./FOCUS_MODE_IMPLEMENTATION.md) documentation.
