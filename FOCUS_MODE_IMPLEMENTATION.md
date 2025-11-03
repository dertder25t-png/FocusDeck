# Focus Mode Enhancement Implementation

This document describes the implementation of enhanced Focus Mode with smart signals and Focus Recovery features.

## Overview

The Focus Mode enhancement adds intelligent distraction detection and recovery suggestions to help users maintain focus during study sessions. The system monitors multiple signals from desktop and mobile devices and provides real-time feedback when distractions are detected.

## Architecture

### Server Components

#### 1. FocusSession Entity (`FocusDeck.Domain.Entities.FocusSession`)

Core entity representing a focus session with:
- **Policy Configuration**: Controls session behavior
  - `Strict`: Enable distraction detection for phone signals
  - `AutoBreak`: Automatically suggest breaks when threshold exceeded
  - `AutoDim`: Dim desktop background when paused
  - `NotifyPhone`: Send notifications to mobile device
  
- **Signal Tracking**: Stores signals from devices
  - PhoneMotion: Accelerometer movement
  - PhoneScreen: Screen on/off state
  - Keyboard: Desktop keyboard activity
  - Mouse: Desktop mouse activity
  - AmbientNoise: Microphone noise level

- **Distraction Tracking**: Records distractions and recovery suggestions
  - DistractionsCount: Total distractions in session
  - LastRecoverySuggestionAt: Timestamp of last suggestion

#### 2. FocusController (`FocusDeck.Server.Controllers.v1.FocusController`)

REST API endpoints:
- `POST /v1/focus/sessions` - Create new focus session
- `GET /v1/focus/sessions/{id}` - Get session details
- `GET /v1/focus/sessions/active` - Get active session
- `POST /v1/focus/signals` - Submit device signal
- `POST /v1/focus/sessions/{id}/end` - End focus session

#### 3. Distraction Detection Logic

When in strict mode, the controller monitors signals for distractions:
- **Phone Motion**: Value > 0.5 with recent phone activity in 15-second window
- **Phone Screen**: Screen turned on (value > 0)

Detection window: 15 seconds (configurable via `DistractionWindowSeconds` constant)

#### 4. Recovery Suggestion Logic

When AutoBreak is enabled:
- Threshold: 3+ distractions in 10-minute window
- Cooldown: Won't send another suggestion within same window
- Suggestions:
  - Strict mode: "Enable Lock Mode"
  - Normal mode: "Take 2-min break"

#### 5. SignalR Events (`FocusDeck.Server.Hubs.NotificationsHub`)

Real-time events broadcast to clients:
- `Focus:Distraction { reason, at }` - Distraction detected
- `Focus:RecoverySuggested { suggestion }` - Recovery action suggested

### Desktop Components (Stub Implementation)

#### ActivityMonitorService (`FocusDeck.Desktop.Services.ActivityMonitorService`)

Monitors desktop activity (stub - requires full Windows implementation):
- **Keyboard idle time**: Track time since last keystroke
- **Mouse idle time**: Track time since last mouse movement  
- **Ambient noise**: Capture microphone RMS level while muted

Implementation notes:
- Full implementation requires Windows API hooks (GetLastInputInfo)
- NAudio library for microphone RMS detection
- Signals sent to server every 10 seconds

#### FocusOverlayService (`FocusDeck.Desktop.Services.ActivityMonitorService`)

Manages focus UI overlays (stub - requires WPF implementation):
- **Dim overlay**: Semi-transparent black background when paused
- **Recovery banner**: Action buttons for recovery suggestions
  - "Take 2-min break" button
  - "Enable Lock Mode" button
  - "Snooze" button (dismisses for 10 minutes)

Implementation notes:
- Requires WPF overlay window with topmost z-order
- Banner positioned at top or bottom of screen

### Mobile Components (Stub Implementation)

#### SensorMonitorService (`FocusDeck.Mobile.Services.SensorMonitorService`)

Monitors phone sensors (stub - requires MAUI implementation):
- **Accelerometer**: Detects phone movement
- **Screen state**: Monitors screen on/off
- **Light sensor**: Ambient light level

Implementation notes:
- Use Microsoft.Maui.Devices.Sensors for accelerometer
- Platform-specific APIs for screen state
- Signals sent to server every 10 seconds via HTTP POST

#### FocusNotificationService (`FocusDeck.Mobile.Services.SensorMonitorService`)

Handles focus-related notifications (stub - requires MAUI implementation):
- **Silence prompt**: "Silence notifications now?" action
- **Distraction alerts**: Toast notifications for distractions
- **Recovery suggestions**: Interactive notifications with action buttons

Implementation notes:
- Use MAUI DisplayAlert and CommunityToolkit toasts
- Platform-specific Do Not Disturb APIs
- Interactive notification actions

## API Usage Examples

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
```

### Submit Signal

```http
POST /v1/focus/signals
Content-Type: application/json

{
  "deviceId": "mobile-device-123",
  "kind": "PhoneScreen",
  "value": 1.0,
  "timestamp": "2025-11-03T02:00:00Z"
}
```

### SignalR Connection

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications")
  .build();

connection.on("FocusDistraction", (reason, at) => {
  console.log(`Distraction detected: ${reason} at ${at}`);
});

connection.on("FocusRecoverySuggested", (suggestion) => {
  console.log(`Recovery suggested: ${suggestion}`);
});
```

## Testing

### Unit Tests (`FocusDeck.Server.Tests.FocusSessionTests`)

Comprehensive test coverage (12 tests, all passing):

1. **Session Management**
   - CreateSession_WithValidRequest_CreatesSession
   - GetActiveSession_WithActiveSession_ReturnsSession
   - GetActiveSession_NoActiveSession_ReturnsNotFound
   - EndSession_ActiveSession_CompletesSession
   - EndSession_NonExistentSession_ReturnsNotFound

2. **Distraction Detection**
   - SubmitSignal_PhoneMotion_InStrictMode_DetectsDistraction
   - SubmitSignal_PhoneScreen_InStrictMode_DetectsDistraction
   - SubmitSignal_NonStrictMode_DoesNotDetectDistraction

3. **Recovery Suggestions**
   - SubmitSignal_MultipleDistractions_SuggestsRecovery
   - SubmitSignal_WithAutoBreakDisabled_DoesNotSuggestRecovery

4. **Validation**
   - SubmitSignal_InvalidSignalKind_ReturnsBadRequest
   - SubmitSignal_NoActiveSession_ReturnsNotFound

All tests use in-memory database and test SignalR hub context.

## Database Schema

### FocusSessions Table

```sql
CREATE TABLE FocusSessions (
    Id UUID PRIMARY KEY,
    UserId VARCHAR(200) NOT NULL,
    StartTime TIMESTAMP NOT NULL,
    EndTime TIMESTAMP NULL,
    Status INTEGER NOT NULL,
    Policy TEXT NOT NULL,  -- JSON: { strict, autoBreak, autoDim, notifyPhone }
    Signals TEXT NOT NULL,  -- JSON array: [{ deviceId, kind, value, timestamp }]
    DistractionsCount INTEGER NOT NULL,
    LastRecoverySuggestionAt TIMESTAMP NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);

CREATE INDEX IX_FocusSessions_UserId ON FocusSessions(UserId);
CREATE INDEX IX_FocusSessions_Status ON FocusSessions(Status);
CREATE INDEX IX_FocusSessions_UserId_Status ON FocusSessions(UserId, Status);
CREATE INDEX IX_FocusSessions_StartTime ON FocusSessions(StartTime);
```

## Configuration

### Constants (Tunable Parameters)

Defined in `FocusController.cs`:

```csharp
// Time window for detecting related distractions
private const int DistractionWindowSeconds = 15;

// Time window for counting distractions for recovery
private const int RecoverySuggestionWindowMinutes = 10;

// Number of distractions to trigger recovery suggestion
private const int RecoverySuggestionThreshold = 3;
```

## Future Enhancements

1. **Desktop Implementation**
   - Complete ActivityMonitorService with Windows API hooks
   - Implement FocusOverlayService with WPF UI
   - Add configuration UI for monitoring settings

2. **Mobile Implementation**
   - Complete SensorMonitorService with MAUI sensors
   - Implement FocusNotificationService with platform notifications
   - Add Do Not Disturb integration

3. **Advanced Features**
   - Machine learning for personalized distraction patterns
   - Integration with calendar for automatic focus sessions
   - Focus session analytics and reports
   - Customizable recovery actions
   - Team focus mode for group study sessions

## Known Limitations

1. **Platform Support**
   - Desktop monitoring requires Windows (WPF + Windows APIs)
   - Mobile requires MAUI workload installation
   - Linux/Mac desktop support limited without native API access

2. **Privacy Considerations**
   - Ambient noise monitoring should be opt-in
   - Signals stored in database (consider retention policy)
   - Consider GDPR/privacy implications for signal data

3. **Performance**
   - Signal frequency (10 seconds) may need tuning
   - JSON serialization overhead for large signal arrays
   - Consider signal aggregation for long sessions

## License

Part of FocusDeck project - see main repository LICENSE file.
