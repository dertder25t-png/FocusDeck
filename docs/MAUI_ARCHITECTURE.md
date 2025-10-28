# 🏗️ MAUI Architecture Guide

**Version:** 2.0 | **Last Updated:** October 28, 2025

## 📐 Project Structure

```
src/FocusDeck.Mobile/
├── Platforms/
│   ├── iOS/
│   │   ├── Info.plist
│   │   ├── Entitlements.plist
│   │   └── AppDelegate.cs
│   ├── Android/
│   │   ├── AndroidManifest.xml
│   │   ├── MainActivity.cs
│   │   └── Resources/
│   ├── MacCatalyst/
│   │   └── AppDelegate.cs
│   └── Windows/
│       └── App.xaml
├── Pages/
│   ├── MainPage.xaml
│   ├── MainPage.xaml.cs
│   ├── StudyTimerPage.xaml
│   ├── StudyTimerPage.xaml.cs
│   ├── SessionHistoryPage.xaml
│   ├── SessionHistoryPage.xaml.cs
│   ├── AnalyticsPage.xaml
│   ├── AnalyticsPage.xaml.cs
│   ├── SettingsPage.xaml
│   └── SettingsPage.xaml.cs
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── StudyTimerViewModel.cs
│   ├── SessionHistoryViewModel.cs
│   ├── AnalyticsViewModel.cs
│   └── SettingsViewModel.cs
├── Services/
│   ├── IMobileAudioRecordingService.cs
│   ├── MobileAudioRecordingService.cs
│   ├── IMobileNotificationService.cs
│   ├── MobileNotificationService.cs
│   ├── IMobileStorageService.cs
│   └── MobileStorageService.cs
├── Models/
│   ├── MobileStudySession.cs
│   ├── MobileAnalytics.cs
│   └── MobileSettings.cs
├── Resources/
│   ├── Styles/
│   │   └── Colors.xaml
│   ├── Fonts/
│   └── Icons/
├── App.xaml
├── App.xaml.cs
├── AppShell.xaml
├── AppShell.xaml.cs
├── MauiProgram.cs
└── FocusDeck.Mobile.csproj
```

## 🎨 MVVM Pattern Implementation

### BaseViewModel

```csharp
public abstract class BaseViewModel : INotifyPropertyChanged
{
    // Properties auto-notify UI of changes
    // Simplifies ViewModel creation
    // Handles loading state and title
}
```

### Page-ViewModel Binding

```xaml
<!-- StudyTimerPage.xaml -->
<ContentPage x:Class="FocusDeck.Mobile.Pages.StudyTimerPage"
             BindingContext="{StaticResource StudyTimerViewModel}">
    
    <StackLayout>
        <!-- Timer display bound to ViewModel property -->
        <Label Text="{Binding TimerDisplay}" FontSize="48" />
        
        <!-- Commands bound to ViewModel methods -->
        <Button Text="Start" 
                Command="{Binding StartTimerCommand}" />
    </StackLayout>
    
</ContentPage>
```

### ViewModel Command Binding

```csharp
public class StudyTimerViewModel : BaseViewModel
{
    public ICommand StartTimerCommand => new Command(async () => 
    {
        await StartTimerAsync();
    });
}
```

## 🔄 Service Architecture

### Layer 1: Shared Services (from FocusDock.Core)
```
IStudySessionService      ← Session CRUD & persistence
IAnalyticsService         ← Productivity metrics
ICloudSyncService         ← Cloud synchronization
IEncryptionService        ← Data security
```

### Layer 2: Platform-Specific Services
```
IMobileAudioRecordingService  ← iOS/Android audio
IMobileNotificationService    ← Local & push notifications
IMobileStorageService         ← File & app data management
```

### Layer 3: Platform Implementation
```
iOS (Services/iOS/):
  ├─ iOSAudioRecordingService   ← AVAudioEngine
  ├─ iOSNotificationService     ← UserNotifications
  └─ iOSStorageService          ← FileManager

Android (Services/Android/):
  ├─ AndroidAudioRecordingService  ← MediaRecorder
  ├─ AndroidNotificationService    ← NotificationCompat
  └─ AndroidStorageService         ← Context.GetFilesDir()
```

## 📱 Page Architecture

### Main Application Flow

```
AppShell (Navigation Container)
  ├── Tab 1: StudyTimerPage
  │   └── StudyTimerViewModel
  │       ├── Timer countdown (25 min default)
  │       ├── Session persistence
  │       ├── Audio alerts
  │       └── Voice notes
  │
  ├── Tab 2: SessionHistoryPage
  │   └── SessionHistoryViewModel
  │       ├── Load past sessions
  │       ├── Filter & sort
  │       └── Session details
  │
  ├── Tab 3: AnalyticsPage
  │   └── AnalyticsViewModel
  │       ├── Charts & graphs
  │       ├── Productivity trends
  │       └── Time analysis
  │
  └── Tab 4: SettingsPage
      └── SettingsViewModel
          ├── Cloud sync config
          ├── User preferences
          └── About & feedback
```

### StudyTimerPage Design

```
┌─────────────────────────────┐
│  FocusDeck                  │
├─────────────────────────────┤
│                             │
│       25:00                 │
│   (Large timer display)     │
│                             │
│  ┌──────────────────────┐   │
│  │ Focus Time: 7h 30m   │   │
│  │ Sessions: 24         │   │
│  │ Target: 8h           │   │
│  └──────────────────────┘   │
│                             │
│  [▶ Start] [⏸ Pause] [⏹ End]│
│                             │
│  [🎙️ Note] [☕ Break] [⚙️ ...]│
│                             │
├─────────────────────────────┤
│ Study  History  Analytics Settings
└─────────────────────────────┘
```

### SessionHistoryPage Design

```
┌─────────────────────────────┐
│  Session History            │
├─────────────────────────────┤
│ [Filter: All ▼] [Sort: Date ▼] │
│                             │
│ Today                       │
│  ├─ Focus Session: 25m  ✓   │
│  ├─ Study Math: 50m     ✓   │
│  └─ Break: 15m          ✓   │
│                             │
│ Yesterday                   │
│  ├─ Focus Session: 25m  ✓   │
│  └─ Deep Work: 90m      ✓   │
│                             │
│ [Tap session for details]   │
│                             │
├─────────────────────────────┤
│ Study  History  Analytics Settings
└─────────────────────────────┘
```

### AnalyticsPage Design

```
┌─────────────────────────────┐
│  Analytics                  │
├─────────────────────────────┤
│ This Week: 22.5 hours       │
│ Target: 40 hours            │
│ [████░░░░░░░░░░] 56%        │
│                             │
│ Sessions Breakdown           │
│  Focus: 15h (67%)           │
│  Deep Work: 7.5h (33%)      │
│ [Bar chart]                 │
│                             │
│ Daily Average: 3.2h         │
│ Best Day: 6.5h (Tuesday)    │
│ Streak: 5 days              │
│                             │
│ [Line chart - 7 day trend]  │
│                             │
├─────────────────────────────┤
│ Study  History  Analytics Settings
└─────────────────────────────┘
```

### SettingsPage Design

```
┌─────────────────────────────┐
│  Settings                   │
├─────────────────────────────┤
│ Timer Preferences           │
│  ├─ Default Duration: 25m   │
│  ├─ Auto-start Breaks: On   │
│  └─ Sound: On               │
│                             │
│ Cloud Sync                  │
│  ├─ Sync: OneDrive    ✓     │
│  ├─ Last Sync: 5m ago       │
│  ├─ Auto-sync: Every 5m     │
│  └─ [Sync Now]              │
│                             │
│ Data                        │
│  ├─ Local Storage: 45 MB    │
│  ├─ Cloud Storage: 2 GB     │
│  └─ [Export Data]           │
│                             │
│ About                       │
│  ├─ Version: 2.0.0          │
│  └─ [Feedback & Support]    │
│                             │
├─────────────────────────────┤
│ Study  History  Analytics Settings
└─────────────────────────────┘
```

## 🔐 Data Flow & Synchronization

### Offline-First Architecture

```
User Input (Study Session)
  ↓
Local Database (SQLite)
  ↓
ViewModel State
  ↓
Page UI Update
  ↓
Background Sync (when online)
  ↓
Cloud Storage (OneDrive/Google Drive)
```

### Conflict Resolution

```
Local Change (10:30 AM) ← Edit duration
  ↓
Cloud Change (10:25 AM) ← Sync pulled
  ↓
Conflict Detected
  ↓
Strategy: Last-Write-Wins
  ↓
Keep: 10:30 AM version
  ↓
Update: Cloud
```

## 🎯 Dependency Injection Setup

### Registration (MauiProgram.cs)

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    builder
        .UseMauiApp<App>()
        // ... configuration ...
        .AddMobileServices();  // ← Register all services
    
    return builder.Build();
}
```

### Usage in ViewModel

```csharp
public class StudyTimerViewModel : BaseViewModel
{
    private readonly IStudySessionService _sessionService;
    private readonly IMobileAudioRecordingService _audioService;
    private readonly ICloudSyncService _syncService;
    
    // Injected via constructor
    public StudyTimerViewModel(
        IStudySessionService sessionService,
        IMobileAudioRecordingService audioService,
        ICloudSyncService syncService)
    {
        _sessionService = sessionService;
        _audioService = audioService;
        _syncService = syncService;
    }
}
```

## 🛠️ Platform-Specific Code

### iOS Audio (Objective-C#)

```csharp
#if IOS
using AVFoundation;

public partial class iOSAudioRecordingService : IMobileAudioRecordingService
{
    private AVAudioRecorder? _recorder;
    
    public async Task<bool> StartRecordingAsync()
    {
        var recordingPath = GetRecordingPath();
        var settings = new AudioSettings
        {
            SampleRate = 44100,
            NumberChannels = 1,
            LinearPcmBitDepth = 16
        };
        
        _recorder = new AVAudioRecorder(
            NSUrl.FromFilename(recordingPath), 
            settings, 
            out var error);
        
        return _recorder?.Record() ?? false;
    }
}
#endif
```

### Android Audio (Kotlin Interop)

```csharp
#if ANDROID
using Android.Media;

public partial class AndroidAudioRecordingService : IMobileAudioRecordingService
{
    private MediaRecorder? _mediaRecorder;
    
    public async Task<bool> StartRecordingAsync()
    {
        _mediaRecorder = new MediaRecorder();
        _mediaRecorder.SetAudioSource(AudioSource.Mic);
        _mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
        _mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
        _mediaRecorder.SetOutputFile(GetRecordingPath());
        
        _mediaRecorder.Prepare();
        _mediaRecorder.Start();
        
        return await Task.FromResult(true);
    }
}
#endif
```

## 🔄 State Management

### Session State Flow

```
Idle State
  ↓
User Taps "Start" → Timer ViewModel: OnTimerStarted()
  ↓
Timer Runs (25 min)
  ↓
Time Expires → Timer ViewModel: OnTimerComplete()
  ↓
Save Session
  → IStudySessionService.CreateSessionAsync()
  → Local database
  → Cloud sync (if online)
  ↓
Show Completion Screen
  ↓
Idle State
```

## 📊 Performance Targets

- **Memory:** < 100 MB at idle, < 200 MB during recording
- **UI Response:** < 100ms for all interactions
- **Battery:** < 5% per hour in background
- **Sync:** Complete within 10 seconds on WiFi
- **Load Time:** App launches in < 2 seconds

## 🧪 Testing Strategy

### Unit Tests
```
✓ ViewModel business logic
✓ Service method calls
✓ Data persistence
✓ Sync algorithms
```

### UI Tests
```
✓ Navigation between pages
✓ Timer functionality
✓ Data binding
✓ Button interactions
```

### Integration Tests
```
✓ End-to-end study session
✓ Cloud sync with local data
✓ Cross-platform consistency
```

## 🚀 Build Configuration

### Project File Settings

```xml
<PropertyGroup>
    <TargetFrameworks>net8.0-android;net8.0-ios;net8.0-maccatalyst;net8.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
</PropertyGroup>

<ItemGroup>
    <ProjectReference Include="../FocusDock.Core/FocusDock.Core.csproj" />
    <ProjectReference Include="../FocusDock.System/FocusDock.System.csproj" />
</ItemGroup>
```

## 📚 References

- MAUI Documentation: https://learn.microsoft.com/maui
- MVVM Toolkit: https://learn.microsoft.com/windows/communitytoolkit/mvvm/
- Shared Services: See `FocusDock.Core/Services/`
- Cloud Sync: See `CLOUD_SYNC_ARCHITECTURE.md`

---

**Next:** Phase 6b Week 1 - Project Setup
