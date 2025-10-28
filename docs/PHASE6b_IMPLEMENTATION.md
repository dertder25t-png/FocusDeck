# 🚀 Phase 6b: MAUI Mobile App - Implementation Guide

**Status:** Starting | **Timeline:** 5 weeks | **Target:** November 28, 2025

## 📋 Week 1: Project Setup & Foundation

### Tasks (40 hours)

#### ✅ Task 1: Create MAUI Project
```bash
# Create new MAUI project
dotnet new maui -n FocusDeck.Mobile -o src/FocusDeck.Mobile

# Add to solution
cd src/FocusDeck.Mobile
dotnet sln ../../FocusDeck.sln add FocusDeck.Mobile.csproj
```

**Expected Result:**
- ✅ Project structure created with iOS, Android, MacCatalyst, Windows folders
- ✅ Basic MauiProgram.cs with app shell
- ✅ Default page and shell layouts
- ✅ Builds successfully for all platforms

**Files Created:**
```
src/FocusDeck.Mobile/
├── Platforms/
│   ├── iOS/
│   ├── Android/
│   └── MacCatalyst/
├── Pages/
│   └── MainPage.xaml (.xaml.cs)
├── AppShell.xaml (.xaml.cs)
├── App.xaml (.xaml.cs)
├── MauiProgram.cs
└── FocusDeck.Mobile.csproj
```

#### ✅ Task 2: Update Project File & Dependencies
```xml
<!-- FocusDeck.Mobile.csproj -->
<PropertyGroup>
    <TargetFrameworks>net8.0-android;net8.0-ios;net8.0-maccatalyst;net8.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
</PropertyGroup>

<!-- Add package references -->
<ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
</ItemGroup>

<!-- Add project references -->
<ItemGroup>
    <ProjectReference Include="../FocusDock.Core/FocusDock.Core.csproj" />
    <ProjectReference Include="../FocusDock.System/FocusDock.System.csproj" />
</ItemGroup>
```

**Expected Result:**
- ✅ FocusDeck.Core and FocusDock.System referenced
- ✅ MVVM Toolkit available
- ✅ No build errors

#### ✅ Task 3: Set Up Dependency Injection

Create `MobileServiceConfiguration.cs`:
```csharp
using FocusDock.Core.Services;
using FocusDeck.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Mobile;

public static class MobileServiceConfiguration
{
    public static IServiceCollection AddMobileServices(this IServiceCollection services)
    {
        // Shared services from Core (already in App.xaml.cs for desktop)
        // Re-register for mobile context
        services.AddSingleton<IStudySessionService, StudySessionService>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<ICloudSyncService, CloudSyncService>();
        
        // Platform-specific mobile services
        services.AddSingleton<IMobileAudioRecordingService, MobileAudioRecordingService>();
        services.AddSingleton<IMobileNotificationService, MobileNotificationService>();
        services.AddSingleton<IMobileStorageService, MobileStorageService>();
        
        return services;
    }
}
```

Update `MauiProgram.cs`:
```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
            .AddMobileServices();  // ← Add this line
        
        return builder.Build();
    }
}
```

**Expected Result:**
- ✅ DI container properly initialized
- ✅ Both shared and mobile services available
- ✅ Clean service registration

#### ✅ Task 4: Create Mobile Service Interfaces

Create `Services/IMobileAudioRecordingService.cs`:
```csharp
namespace FocusDeck.Mobile.Services;

public interface IMobileAudioRecordingService
{
    event EventHandler<AudioRecordingEventArgs>? RecordingStarted;
    event EventHandler<AudioRecordingEventArgs>? RecordingStopped;
    event EventHandler<RecordingErrorEventArgs>? RecordingError;

    bool IsRecording { get; }
    TimeSpan RecordingDuration { get; }
    
    Task<bool> StartRecordingAsync();
    Task<bool> StopRecordingAsync();
    Task<bool> PauseRecordingAsync();
    Task<bool> ResumeRecordingAsync();
    Task<string> GetLastRecordingPathAsync();
}

public class AudioRecordingEventArgs : EventArgs
{
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
}

public class RecordingErrorEventArgs : EventArgs
{
    public string ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}
```

Create `Services/IMobileNotificationService.cs`:
```csharp
namespace FocusDeck.Mobile.Services;

public interface IMobileNotificationService
{
    Task<bool> RequestPermissionAsync();
    Task SendLocalNotificationAsync(string title, string message, int delaySeconds = 10);
    Task SendStudySessionReminderAsync(string sessionName, DateTime startTime);
    Task CancelNotificationAsync(int notificationId);
}
```

Create `Services/IMobileStorageService.cs`:
```csharp
namespace FocusDeck.Mobile.Services;

public interface IMobileStorageService
{
    Task<string> GetAppDataPathAsync();
    Task<string> GetCachePath();
    Task<bool> FileExistsAsync(string filePath);
    Task<byte[]> ReadFileAsync(string filePath);
    Task WriteFileAsync(string filePath, byte[] data);
    Task DeleteFileAsync(string filePath);
    Task<long> GetStorageUsageAsync();
}
```

**Expected Result:**
- ✅ Three service interfaces defined
- ✅ Clear contracts for platform services
- ✅ Event handling patterns established

#### ✅ Task 5: Create Stub Implementations

Create `Services/MobileAudioRecordingService.cs`:
```csharp
using System.Diagnostics;

namespace FocusDeck.Mobile.Services;

public class MobileAudioRecordingService : IMobileAudioRecordingService
{
    private bool _isRecording = false;
    private Stopwatch _recordingTimer = new();
    private string _lastRecordingPath = string.Empty;

    public event EventHandler<AudioRecordingEventArgs>? RecordingStarted;
    public event EventHandler<AudioRecordingEventArgs>? RecordingStopped;
    public event EventHandler<RecordingErrorEventArgs>? RecordingError;

    public bool IsRecording => _isRecording;
    public TimeSpan RecordingDuration => _recordingTimer.Elapsed;

    public Task<bool> StartRecordingAsync()
    {
        try
        {
            _isRecording = true;
            _recordingTimer.Restart();
            RecordingStarted?.Invoke(this, new AudioRecordingEventArgs 
            { 
                Timestamp = DateTime.Now 
            });
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            RecordingError?.Invoke(this, new RecordingErrorEventArgs 
            { 
                ErrorMessage = ex.Message, 
                Exception = ex 
            });
            return Task.FromResult(false);
        }
    }

    public async Task<bool> StopRecordingAsync()
    {
        try
        {
            _isRecording = false;
            _recordingTimer.Stop();
            
            RecordingStopped?.Invoke(this, new AudioRecordingEventArgs 
            { 
                Timestamp = DateTime.Now,
                Duration = _recordingTimer.Elapsed
            });
            
            // TODO: Implement actual platform audio recording stop
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            RecordingError?.Invoke(this, new RecordingErrorEventArgs 
            { 
                ErrorMessage = ex.Message, 
                Exception = ex 
            });
            return false;
        }
    }

    public Task<bool> PauseRecordingAsync() => Task.FromResult(true);
    public Task<bool> ResumeRecordingAsync() => Task.FromResult(true);
    public Task<string> GetLastRecordingPathAsync() => Task.FromResult(_lastRecordingPath);
}
```

**Expected Result:**
- ✅ Stub implementations created
- ✅ Basic event flow working
- ✅ TODO markers for platform-specific code

#### ✅ Task 6: Create MVVM Base Classes

Create `ViewModels/BaseViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FocusDeck.Mobile.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T backingStore, T value,
        [CallerMemberName] string propertyName = "",
        Action? onChanged = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

**Expected Result:**
- ✅ MVVM base class with INotifyPropertyChanged
- ✅ Property change notification system
- ✅ Loading/title state management

#### ✅ Task 7: Set Up App Shell & Navigation

Update `AppShell.xaml`:
```xaml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="FocusDeck.Mobile.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    FlyoutBehavior="Locked">

    <TabBar>
        <ShellContent 
            Title="Study" 
            Icon="timer.png"
            ContentTemplate="{DataTemplate local:StudyTimerPage}"
            Route="StudyTimerPage" />
        
        <ShellContent 
            Title="History" 
            Icon="history.png"
            ContentTemplate="{DataTemplate local:SessionHistoryPage}"
            Route="SessionHistoryPage" />
        
        <ShellContent 
            Title="Analytics" 
            Icon="chart.png"
            ContentTemplate="{DataTemplate local:AnalyticsPage}"
            Route="AnalyticsPage" />
        
        <ShellContent 
            Title="Settings" 
            Icon="settings.png"
            ContentTemplate="{DataTemplate local:SettingsPage}"
            Route="SettingsPage" />
    </TabBar>

</Shell>
```

**Expected Result:**
- ✅ 4-tab bottom navigation
- ✅ Tab routing configured
- ✅ Icons ready for pages

#### ✅ Task 8: Verify Build

```bash
cd src/FocusDeck.Mobile
dotnet build
```

**Expected Result:**
- ✅ 0 build errors
- ✅ All pages compile
- ✅ Services registered
- ✅ Ready for Week 2

### Acceptance Criteria ✅
- [ ] MAUI project created in `src/FocusDeck.Mobile/`
- [ ] All 4 platforms configured (iOS, Android, MacCatalyst, Windows)
- [ ] Shared services accessible from FocusDock.Core
- [ ] Mobile services interfaces defined
- [ ] MVVM base classes created
- [ ] App shell with 4-tab navigation ready
- [ ] 0 build errors
- [ ] Project builds successfully

### Time Estimate: 8 hours
- Project creation & setup: 1 hour
- Dependencies & references: 1.5 hours
- Service configuration: 2 hours
- Interfaces & stubs: 2 hours
- MVVM base classes: 0.5 hours
- App shell & navigation: 1 hour

---

## 📋 Week 2: Study Timer Page & Session Management

### Tasks (40 hours)

#### ✅ Task 1: Create StudyTimerViewModel

See `ViewModels/StudyTimerViewModel.md` for complete implementation.

**Key Features:**
- 25-minute default timer
- Play/pause/stop controls
- Session persistence
- Event publishing
- Break time tracking

#### ✅ Task 2: Create StudyTimerPage UI

See `Pages/StudyTimerPage.xaml` design spec

**Layout:**
- Large timer display (MM:SS format)
- Play/pause/stop buttons
- Session info (current / total)
- Quick actions (notes, break)

#### ✅ Task 3: Integrate with IStudySessionService

Connect UI to shared service for persistence

#### ✅ Task 4: Add Sound & Haptics

Timer end alerts and vibration

### Acceptance Criteria ✅
- [ ] StudyTimerViewModel fully implemented
- [ ] Timer UI with large readable display
- [ ] All controls functional
- [ ] Session data persisted
- [ ] Audio/haptic feedback working
- [ ] Data binding working correctly

### Time Estimate: 8 hours

---

## 📋 Week 3: Database & Offline Sync

### Tasks (40 hours)

#### ✅ Task 1: Set Up SQLite

Create local database schema for offline data

#### ✅ Task 2: Implement Data Sync Layer

Sync with cloud on network connectivity change

#### ✅ Task 3: Create Sync UI

Show sync status, conflicts, queue

### Acceptance Criteria ✅
- [ ] SQLite database created and working
- [ ] Data persists locally
- [ ] Cloud sync functional
- [ ] Offline mode working
- [ ] Sync conflicts resolved

### Time Estimate: 8 hours

---

## 📋 Week 4: Analytics & History Pages

### Tasks (40 hours)

#### ✅ Task 1: SessionHistoryPage

List past sessions with filters

#### ✅ Task 2: AnalyticsPage

Charts, statistics, trends

#### ✅ Task 3: SettingsPage

User preferences, cloud config

### Acceptance Criteria ✅
- [ ] All 3 pages functional
- [ ] Data visualization working
- [ ] Settings persist

### Time Estimate: 8 hours

---

## 📋 Week 5: Platform Services & Release

### Tasks (40 hours)

#### ✅ Task 1: iOS Implementation

Audio, notifications, storage permissions

#### ✅ Task 2: Android Implementation

Audio, notifications, storage permissions

#### ✅ Task 3: Testing & Optimization

Unit tests, UI tests, performance

#### ✅ Task 4: Build & Submission

Prepare for app store release

### Acceptance Criteria ✅
- [ ] iOS app builds and runs
- [ ] Android app builds and runs
- [ ] All features working on both platforms
- [ ] Performance acceptable
- [ ] Ready for app store

### Time Estimate: 8 hours

---

## 🎯 Success Criteria for Phase 6b

```
✅ MAUI project fully configured
✅ 4 pages with navigation working
✅ Study timer functional
✅ Data persisting locally and syncing to cloud
✅ iOS and Android builds successful
✅ All platform services implemented
✅ 0 critical bugs
✅ Performance: < 100ms response time, < 50MB RAM
✅ Battery usage acceptable
✅ Ready for TestFlight & Play Store
```

---

## 🚀 Quick Commands

```bash
# Build all platforms
dotnet build src/FocusDeck.Mobile

# Build Android
dotnet build src/FocusDeck.Mobile -f net8.0-android

# Build iOS
dotnet build src/FocusDeck.Mobile -f net8.0-ios

# Run on connected device/emulator
dotnet run -f net8.0-android --no-build
```

---

## 📚 References

- MAUI Architecture: `./MAUI_ARCHITECTURE.md`
- Cloud Sync: `./CLOUD_SYNC_ARCHITECTURE.md`
- API Integration: `./API_INTEGRATION_CHECKLIST.md`

---

**Phase 6b Timeline:** Nov 1 - Nov 28, 2025  
**Next Phase:** Phase 7 (Community Features)
