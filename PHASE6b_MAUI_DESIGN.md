# Phase 6b: Mobile Companion App (MAUI) - Architecture & Design

## Overview
Phase 6b creates a native mobile app for iOS and Android using .NET MAUI, enabling study tracking, quick timers, and cloud sync from mobile devices.

## Why MAUI?

```
Option Comparison:
┌──────────────────┬──────────────┬─────────────────┬────────────┐
│ Technology       │ Code Sharing │ Time to Market  │ Quality    │
├──────────────────┼──────────────┼─────────────────┼────────────┤
│ MAUI (Chosen)    │ 95%          │ 4-6 weeks       │ ⭐⭐⭐⭐⭐ │
│ Flutter          │ 90%          │ 4-5 weeks       │ ⭐⭐⭐⭐   │
│ React Native     │ 85%          │ 5-7 weeks       │ ⭐⭐⭐     │
│ Native (iOS+Kts) │ 0%           │ 12-16 weeks     │ ⭐⭐⭐⭐⭐ │
└──────────────────┴──────────────┴─────────────────┴────────────┘

Benefits:
✅ 95% code sharing with desktop (same C#/.NET 8)
✅ Single codebase for iOS & Android
✅ Access to native APIs when needed
✅ Hot reload during development
✅ Shared cloud sync logic with desktop
```

## Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│                 MAUI App (Shared)                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │   Pages     │  │  Controls   │  │   Views     │  │
│  │  (UI Layer) │  │ (Reusable)  │  │  (XAML)     │  │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  │
│         │                 │                 │        │
│  ┌──────▼──────────────────▼─────────────────▼─────┐ │
│  │      ViewModel Layer (MVVM)                     │ │
│  │  - StudySessionViewModel                        │ │
│  │  - SessionHistoryViewModel                      │ │
│  │  - AnalyticsViewModel                           │ │
│  └─────────┬────────────────────────────────────────┘ │
│            │                                           │
│  ┌─────────▼─────────────────────────────────────────┐ │
│  │      Service Layer (Shared .NET 8)                │ │
│  │  - IStudySessionService (shared)                 │ │
│  │  - ICloudSyncService (shared)                    │ │
│  │  - IAnalyticsService (shared)                    │ │
│  │  - IAudioRecordingService (platform-specific)    │ │
│  └─────────┬─────────────────────────────────────────┘ │
│            │                                            │
└────────────┼────────────────────────────────────────────┘
             │
    ┌────────▼────────┐
    │  Platform-Specific Services
    │  
    │  iOS:              Android:
    │  ├─ Audio Rec.      ├─ Audio Rec.
    │  ├─ Notify.         ├─ Notify.
    │  ├─ Storage         ├─ Storage
    │  └─ Camera          └─ Camera
    │
    └────────┬────────────────┐
             │                 │
        ☁️ Cloud Sync (OneDrive/Google Drive)
             │                 │
        🔐 Local SQLite DB   📱 Push Notifications
```

## Project Structure

```
FocusDeck/
├── src/
│   ├── FocusDeck.Shared/        (Phase 5a - existing)
│   ├── FocusDeck.Services/      (Phase 5a/6a - existing)
│   ├── FocusDock.Core/          (Phase 1-4 - existing)
│   ├── FocusDock.System/        (Phase 1-4 - existing)
│   ├── FocusDock.Data/          (Phase 1-4 - existing)
│   └── FocusDeck.Mobile/        (NEW - Phase 6b)
│       ├── FocusDeck.Mobile.csproj
│       ├── Platforms/
│       │   ├── iOS/
│       │   │   ├── Info.plist
│       │   │   ├── Entitlements.plist
│       │   │   └── AppDelegate.cs
│       │   ├── Android/
│       │   │   ├── AndroidManifest.xml
│       │   │   ├── MainActivity.cs
│       │   │   └── Resources/
│       │   ├── MacCatalyst/
│       │   └── Windows/
│       ├── Pages/              (XAML UI)
│       │   ├── MainPage.xaml
│       │   ├── StudyTimerPage.xaml
│       │   ├── SessionHistoryPage.xaml
│       │   ├── AnalyticsPage.xaml
│       │   └── SettingsPage.xaml
│       ├── ViewModels/         (MVVM)
│       │   ├── BaseViewModel.cs
│       │   ├── StudyTimerViewModel.cs
│       │   ├── SessionHistoryViewModel.cs
│       │   ├── AnalyticsViewModel.cs
│       │   └── SettingsViewModel.cs
│       ├── Views/              (Reusable controls)
│       │   ├── TimerControl.xaml
│       │   ├── SessionCard.xaml
│       │   └── StatsCard.xaml
│       ├── Services/
│       │   ├── MobileAudioRecordingService.cs
│       │   ├── MobileNotificationService.cs
│       │   ├── MobileStorageService.cs
│       │   └── MobileCloudSyncService.cs
│       ├── Models/
│       │   └── SessionDisplayModel.cs
│       ├── Resources/
│       │   ├── Styles/
│       │   ├── Images/
│       │   └── Colors.xaml
│       ├── App.xaml
│       ├── AppShell.xaml
│       └── MauiProgram.cs
├── FocusDeck.sln
└── README.md
```

## Core Pages & Features

### 1. Main Page / Dashboard
```xaml
┌─────────────────────────────┐
│  FocusDeck Study Companion  │
├─────────────────────────────┤
│                             │
│    Current Session          │
│    ┌───────────────────┐    │
│    │    00:15:32       │    │
│    │  Study: Math      │    │
│    ├───────────────────┤    │
│    │  [⏸ Pause] [Stop] │    │
│    └───────────────────┘    │
│                             │
│    Session Stats            │
│    ├─ Today: 2h 45m         │
│    ├─ Week Avg: 3h 20m      │
│    └─ Streak: 7 days        │
│                             │
│    Quick Actions            │
│    [ New Session ] [ View All ]
│                             │
└─────────────────────────────┘
```

### 2. Study Timer Page (Primary)
```xaml
┌─────────────────────────────┐
│  Quick Study Session        │
├─────────────────────────────┤
│                             │
│    Subject:  [Select ▼]     │
│    Duration: [25 ◄ 25 ►]    │
│                             │
│    ┌─────────────────────┐  │
│    │                     │  │
│    │       00:25:00      │  │
│    │                     │  │
│    └─────────────────────┘  │
│                             │
│    [🎙️ Note] [⏸ Pause]      │
│    [ 🎵 Music] [⏹ Stop]      │
│    [ 🎯 Plan ]              │
│                             │
└─────────────────────────────┘
```

### 3. Session History Page
```xaml
┌─────────────────────────────┐
│  Session History            │
├─────────────────────────────┤
│                             │
│  Today                      │
│  ┌───────────────────────┐  │
│  │ Math - 45 min         │  │
│  │ 2:00 PM - Completed   │  │
│  └───────────────────────┘  │
│  ┌───────────────────────┐  │
│  │ Physics - 60 min      │  │
│  │ 10:30 AM - Completed  │  │
│  └───────────────────────┘  │
│                             │
│  Yesterday                  │
│  ┌───────────────────────┐  │
│  │ Biology - 30 min      │  │
│  │ 8:15 PM - Completed   │  │
│  └───────────────────────┘  │
│                             │
│  [← Older]          [Newer] │
│                             │
└─────────────────────────────┘
```

### 4. Analytics Page
```xaml
┌─────────────────────────────┐
│  Your Analytics             │
├─────────────────────────────┤
│                             │
│  This Week                  │
│  ████████░░░░░░░ 18h 45m   │
│                             │
│  By Subject                 │
│  📘 Math:    5h 30m (29%)   │
│  📗 Biology: 4h 15m (23%)   │
│  📙 Physics: 3h 20m (18%)   │
│  ...                        │
│                             │
│  Best Time:  2-4 PM         │
│  Avg Session: 42 min        │
│  Streaks: 🔥 7 days         │
│                             │
│  [Weekly] [Monthly] [All]   │
│                             │
└─────────────────────────────┘
```

## MVVM Pattern Implementation

### Base ViewModel
```csharp
public abstract class BaseViewModel : INotifyPropertyChanged
{
    protected IStudySessionService SessionService { get; }
    protected ICloudSyncService CloudSync { get; }
    protected IAnalyticsService Analytics { get; }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new(propertyName));
    
    protected void Set<T>(ref T field, T value, 
        [CallerMemberName] string? name = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(name!);
        }
    }
}
```

### StudyTimerViewModel
```csharp
public class StudyTimerViewModel : BaseViewModel
{
    private TimeSpan _currentTime;
    private TimeSpan _totalDuration = TimeSpan.FromMinutes(25);
    private bool _isRunning;
    private string _currentSubject = "Study";
    
    public TimeSpan CurrentTime { 
        get => _currentTime; 
        set => Set(ref _currentTime, value);
    }
    
    public IAsyncRelayCommand StartSessionCommand { get; }
    public IAsyncRelayCommand PauseSessionCommand { get; }
    public IAsyncRelayCommand StopSessionCommand { get; }
    public IAsyncRelayCommand RecordNoteCommand { get; }
    
    public StudyTimerViewModel()
    {
        StartSessionCommand = new AsyncRelayCommand(StartSessionAsync);
        // ... initialize other commands
    }
    
    private async Task StartSessionAsync()
    {
        // Create study session
        // Start timer
        // Update UI
        // Sync to cloud
    }
}
```

## Platform-Specific Services

### iOS Audio Recording
```csharp
public class iOSAudioRecordingService : IAudioRecordingService
{
    public async Task<string> StartRecording()
    {
        // Use AVAudioRecorder
        // Request microphone permission
        // Set up audio session
        return recordingPath;
    }
    
    public async Task<string> TranscribeAudio(string filePath)
    {
        // Use SFSpeechRecognizer
        // Handle speech recognition lifecycle
    }
}
```

### Android Audio Recording
```csharp
public class AndroidAudioRecordingService : IAudioRecordingService
{
    public async Task<string> StartRecording()
    {
        // Use MediaRecorder
        // Request recording permission
        // Setup audio focus
        return recordingPath;
    }
    
    public async Task<string> TranscribeAudio(string filePath)
    {
        // Use Android Speech Recognition
        // Handle lifecycle
    }
}
```

## Cloud Sync Integration

### Offline-First Approach
```
┌─────────────────────────────┐
│  Local SQLite Database      │
│  (Fast, offline support)    │
├─────────────────────────────┤
│  ├─ StudySessions           │
│  ├─ TodoItems               │
│  ├─ Analytics               │
│  └─ SyncQueue               │
└────────┬────────────────────┘
         │
    ┌────▼─────┐
    │ Internet? │
    └─┬────┬────┘
   Yes│    │No
      │    └─→ Queue changes locally
      │
   ┌──▼──────────────────────┐
   │ CloudSyncService        │
   │ ├─ Upload changes       │
   │ ├─ Download updates     │
   │ ├─ Resolve conflicts    │
   │ └─ Update local DB      │
   └──┬──────────────────────┘
      │
   ☁️ Cloud Storage
```

### Sync Strategy
1. **First Load**: Download all data from cloud
2. **On Change**: Queue local changes
3. **On Network**: Sync queued changes
4. **Periodic**: Auto-sync every 5 minutes
5. **Conflict**: User chooses or merge

## Notifications

### Local Notifications
```csharp
public async Task SendStudyReminderAsync(StudySession session)
{
    var request = new NotificationRequest
    {
        Title = "Study Time!",
        Description = $"Start {session.Subject} session",
        Subtitle = $"Scheduled for {session.PlannedTime:h:mm}",
        NotificationId = session.Id.GetHashCode(),
        Schedule = new NotificationRequestBuilder()
            .AtTime(session.PlannedTime)
            .Build()
    };
    
    await NotificationCenter.Current.SendAsync(request);
}
```

### Push Notifications (Future)
```csharp
// Firebase Cloud Messaging integration
// Handle notifications from server
public void OnPushNotification(RemoteMessage message)
{
    // "Your study plan for today needs review"
    // "You've studied for 2 hours today!"
    // "Time for your scheduled break"
}
```

## Dependencies

### NuGet Packages
```xml
<ItemGroup>
    <!-- MAUI Framework -->
    <PackageReference Include="Microsoft.Maui" Version="8.0.0" />
    <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.0" />
    
    <!-- MVVM -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
    
    <!-- Local Database -->
    <PackageReference Include="sqlite-net-pcl" Version="1.8.391" />
    
    <!-- Firebase (for push notifications) -->
    <PackageReference Include="FirebaseAuthentication.net" Version="3.5.0" />
    
    <!-- Cloud Storage -->
    <PackageReference Include="Microsoft.Graph" Version="5.0.0" />
    <PackageReference Include="Google.Apis.Drive.v3" Version="1.63.0" />
    
    <!-- Platform-specific audio/speech -->
    <!-- AVFoundation, Android Media APIs via Maui platform channels -->
</ItemGroup>
```

## Implementation Timeline

### Week 1: Project Setup & Basic UI
- [ ] Create MAUI project structure
- [ ] Set up iOS/Android project configurations
- [ ] Implement basic page layouts (XAML)
- [ ] Set up MVVM base classes
- [ ] Create main app shell and navigation

### Week 2: Timer & Session Management
- [ ] Implement StudyTimerViewModel
- [ ] Build timer UI with countdown
- [ ] Integrate with IStudySessionService
- [ ] Add session start/pause/stop logic
- [ ] Implement session persistence

### Week 3: Cloud Sync & Analytics
- [ ] Set up SQLite local database
- [ ] Implement offline-first sync
- [ ] Create analytics page UI
- [ ] Build analytics ViewModel
- [ ] Add sync status indicators

### Week 4: Audio & Notifications
- [ ] Implement platform-specific audio recording
- [ ] Add voice note recording UI
- [ ] Implement local notifications
- [ ] Set up push notifications framework
- [ ] Testing and bug fixes

### Week 5: Polish & Testing
- [ ] UI/UX refinement
- [ ] Performance optimization
- [ ] Comprehensive testing
- [ ] Documentation
- [ ] App store preparation

## Security Considerations

- Biometric authentication for sensitive data
- Encrypted local database
- SSL certificate pinning for cloud sync
- Secure credential storage
- App-level encryption for notes

## Testing Strategy

```
Unit Tests:
├─ ViewModels (MVVM logic)
├─ Services (cloud sync, audio)
└─ Utilities (encryption, conversion)

UI Tests (XCUITest/Espresso):
├─ Navigation flows
├─ Timer functionality
├─ Sync workflows
└─ Error handling

Integration Tests:
├─ Cloud sync with mock providers
├─ Local/remote data consistency
└─ Multi-device scenarios
```

## Success Metrics

✅ **Performance**
- App startup: < 2 seconds
- Timer accuracy: ± 1 second
- Sync latency: < 5 seconds
- Memory usage: < 100MB

✅ **Functionality**
- 95%+ timer accuracy
- Cloud sync without data loss
- Offline operation support
- Multi-device consistency

✅ **Quality**
- 0 crashes on basic flows
- 98%+ test coverage for services
- No data corruption scenarios
- Graceful offline handling

---

## Next Steps
1. Create MAUI project with proper structure
2. Set up iOS/Android specific configurations
3. Implement basic timer page
4. Integrate with existing services
5. Add cloud sync connectivity
