# 🔨 Phase 5a: Refactoring for Cross-Platform Architecture
**Timeline:** 2 weeks | **Status:** Ready to Start

---

## Overview
We're reorganizing the codebase to separate **platform-specific code** from **business logic**. This allows us to reuse all the study tools when we build mobile and web apps in Phase 6.

**Current Problem:**
```
✗ Win32 P/Invoke scattered throughout
✗ Services depend on WPF
✗ Models tied to Windows paths
✗ No clear abstraction layer
→ Hard to reuse code for mobile/web
```

**Phase 5a Solution:**
```
✓ FocusDeck.Shared (All models, no platform code)
✓ FocusDeck.Services (All business logic, interfaces)
✓ IPlatformService abstraction (Win32 on Desktop, iOS/Android on Mobile, Web API on Web)
✓ Dependency injection (Pick platform at startup)
→ Easy to add new platforms
```

---

## Implementation Steps

### Step 1: Create New Project Structure
**Goal:** Add 2 new .NET 8 class libraries

#### Create FocusDeck.Shared
```bash
# In PowerShell, from FocusDeck directory
dotnet new classlib -n FocusDeck.Shared -f net8.0
```

**Purpose:**
- Models (StudySession, WorkspaceInfo, WindowInfo, etc.)
- DTOs for API responses
- Constants and enums
- JSON serialization helpers
- **Zero dependencies** on WPF or Win32

**Files to move here:**
- `src/FocusDock.Data/Models/*`
- `src/FocusDock.Core/Models/*`

#### Create FocusDeck.Services
```bash
dotnet new classlib -n FocusDeck.Services -f net8.0
```

**Purpose:**
- Core business logic (services)
- Platform-agnostic interfaces
- Service implementations
- Only depends on FocusDeck.Shared

**Files to move here:**
- `src/FocusDock.Core/Services/*` (except UI-specific code)
- Refactored to not use WPF/Win32 directly

#### Update Project File
```xml
<!-- FocusDeck.sln -->
<Project>
  <ItemGroup>
    <ProjectReference Include="src/FocusDeck.Shared/FocusDeck.Shared.csproj" />
    <ProjectReference Include="src/FocusDeck.Services/FocusDeck.Services.csproj" />
    <ProjectReference Include="src/FocusDeck.Core/FocusDeck.Core.csproj" />
  </ItemGroup>
</Project>
```

---

### Step 2: Create Platform Abstraction Layer
**Goal:** Define interfaces for platform-specific operations

#### File: `FocusDeck.Services/Abstractions/IPlatformService.cs`
```csharp
namespace FocusDeck.Services.Abstractions;

public interface IPlatformService
{
    // File system
    Task<string> GetAppDataPath();
    Task<string> GetAudioStoragePath();
    Task<bool> DirectoryExists(string path);
    Task CreateDirectory(string path);
    
    // Notifications
    Task RequestNotificationPermission();
    Task SendNotification(string title, string message, int durationMs = 5000);
    
    // Screen/Window
    Task<(int Width, int Height)> GetScreenSize();
    Task<bool> IsAppForeground();
    
    // External
    Task LaunchUrl(string url);
    Task<string> GetClipboardText();
    Task SetClipboardText(string text);
}

public enum PlatformType
{
    Windows,
    MacOS,
    Linux,
    iOS,
    Android,
    Web
}
```

#### File: `FocusDeck.Services/Abstractions/IAudioService.cs`
```csharp
namespace FocusDeck.Services.Abstractions;

public interface IAudioRecordingService
{
    Task<string> StartRecording();
    Task<AudioRecording> StopRecording();
    Task<string> TranscribeAudio(string filePath);
    event EventHandler<double>? RecordingProgressChanged;
}

public interface IAudioPlaybackService
{
    Task PlayAudio(string filePath);
    Task PauseAudio();
    Task ResumeAudio();
    Task StopAudio();
    Task SetVolume(int percentage);
}

public class AudioRecording
{
    public string FilePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### Step 3: Create Dependency Injection Container
**Goal:** Make it easy to swap implementations per platform

#### File: `FocusDeck.Services/ServiceConfiguration.cs`
```csharp
namespace FocusDeck.Services;

using Microsoft.Extensions.DependencyInjection;
using FocusDeck.Services.Abstractions;

public static class ServiceConfiguration
{
    public static IServiceCollection AddFocusDeckCoreServices(
        this IServiceCollection services)
    {
        // Register all cross-platform services
        services.AddSingleton<IStudySessionService, StudySessionService>();
        services.AddSingleton<IStudyPlanService, StudyPlanService>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<IWorkspaceService, WorkspaceService>();
        services.AddSingleton<IPinService, PinService>();
        services.AddSingleton<IReminderService, ReminderService>();
        
        return services;
    }
    
    public static IServiceCollection AddPlatformServices(
        this IServiceCollection services,
        PlatformType platformType)
    {
        return platformType switch
        {
            PlatformType.Windows => services
                .AddSingleton<IPlatformService, WindowsPlatformService>()
                .AddSingleton<IAudioRecordingService, WindowsAudioRecordingService>()
                .AddSingleton<IAudioPlaybackService, WindowsAudioPlaybackService>(),
            
            PlatformType.iOS => services
                .AddSingleton<IPlatformService, iOSPlatformService>()
                .AddSingleton<IAudioRecordingService, iOSAudioRecordingService>()
                .AddSingleton<IAudioPlaybackService, iOSAudioPlaybackService>(),
            
            // ... other platforms
            
            _ => throw new NotSupportedException($"Platform {platformType} not supported")
        };
    }
}
```

---

### Step 4: Implement Windows Platform Service
**Goal:** Concrete implementation for our current desktop app

#### File: `FocusDock.Core/Platforms/WindowsPlatformService.cs`
```csharp
namespace FocusDock.Core.Platforms;

using FocusDeck.Services.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;

public class WindowsPlatformService : IPlatformService
{
    public Task<string> GetAppDataPath()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocusDeck");
        return Task.FromResult(path);
    }
    
    public Task<string> GetAudioStoragePath()
    {
        return Task.FromResult(Path.Combine(GetAppDataPath().Result, "audio"));
    }
    
    public Task<bool> DirectoryExists(string path)
    {
        return Task.FromResult(Directory.Exists(path));
    }
    
    public Task CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }
    
    public Task RequestNotificationPermission()
    {
        // Windows 10+ always allows notifications
        return Task.CompletedTask;
    }
    
    public async Task SendNotification(string title, string message, int durationMs = 5000)
    {
        // Use Windows Toast Notifications (UWP)
        // Implementation: System.AppModel.Activation
        await Task.Delay(durationMs);
    }
    
    public Task<(int Width, int Height)> GetScreenSize()
    {
        var width = System.Windows.SystemParameters.PrimaryScreenWidth;
        var height = System.Windows.SystemParameters.PrimaryScreenHeight;
        return Task.FromResult(((int)width, (int)height));
    }
    
    public Task<bool> IsAppForeground()
    {
        // Use Win32 GetForegroundWindow
        // This requires P/Invoke
        return Task.FromResult(true);
    }
    
    public async Task LaunchUrl(string url)
    {
        System.Diagnostics.ProcessStartInfo psi = new()
        {
            FileName = url,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
        await Task.CompletedTask;
    }
    
    public Task<string> GetClipboardText()
    {
        string? text = null;
        var staThread = new System.Threading.Thread(() =>
        {
            text = System.Windows.Forms.Clipboard.GetText();
        });
        staThread.SetApartmentState(System.Threading.ApartmentState.STA);
        staThread.Start();
        staThread.Join();
        
        return Task.FromResult(text ?? string.Empty);
    }
    
    public Task SetClipboardText(string text)
    {
        var staThread = new System.Threading.Thread(() =>
        {
            System.Windows.Forms.Clipboard.SetText(text);
        });
        staThread.SetApartmentState(System.Threading.ApartmentState.STA);
        staThread.Start();
        staThread.Join();
        
        return Task.CompletedTask;
    }
}
```

---

### Step 5: Update App.xaml.cs to Use DI
**Goal:** Initialize services with platform awareness

#### File: `FocusDock.App/App.xaml.cs`
```csharp
namespace FocusDock.App;

using Microsoft.Extensions.DependencyInjection;
using FocusDeck.Services;
using FocusDeck.Services.Abstractions;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var services = new ServiceCollection();
        
        // Add core services (cross-platform)
        services.AddFocusDeckCoreServices();
        
        // Add Windows-specific services
        services.AddPlatformServices(PlatformType.Windows);
        
        ServiceProvider = services.BuildServiceProvider();
        
        // Now you can get services like:
        // var studyService = ServiceProvider.GetRequiredService<IStudySessionService>();
    }
}
```

---

### Step 6: Update Existing Services (Remove Platform Dependencies)

#### Before (Bad):
```csharp
public class StudySessionService
{
    private readonly string _storagePath = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                     "FocusDeck");
    
    public void SaveSession(StudySession session)
    {
        // Tightly coupled to Windows paths
    }
}
```

#### After (Good):
```csharp
public class StudySessionService : IStudySessionService
{
    private readonly IPlatformService _platformService;
    
    public StudySessionService(IPlatformService platformService)
    {
        _platformService = platformService;
    }
    
    public async Task SaveSessionAsync(StudySession session)
    {
        string storagePath = await _platformService.GetAppDataPath();
        string filePath = Path.Combine(storagePath, "sessions", $"{session.Id}.json");
        
        // Abstracted away from platform
        string json = JsonSerializer.Serialize(session);
        await File.WriteAllTextAsync(filePath, json);
    }
}
```

---

## Files to Create/Modify

### New Files:
```
✓ FocusDeck.Shared/
├─ FocusDeck.Shared.csproj
├─ Models/
│  ├─ StudySession.cs (moved from Core)
│  ├─ WorkspaceInfo.cs
│  ├─ WindowInfo.cs
│  └─ ... (all models)
└─ Constants/
   └─ AppConstants.cs

✓ FocusDeck.Services/
├─ FocusDeck.Services.csproj
├─ Abstractions/
│  ├─ IPlatformService.cs
│  ├─ IAudioService.cs
│  ├─ IStudySessionService.cs
│  └─ ... (all service interfaces)
├─ Implementations/
│  ├─ StudySessionService.cs (refactored)
│  ├─ AnalyticsService.cs (refactored)
│  └─ ... (all services)
├─ ServiceConfiguration.cs
└─ ServiceExtensions.cs

✓ FocusDock.Core/
├─ Platforms/
│  └─ WindowsPlatformService.cs (Windows impl)
└─ ... (existing services, refactored)
```

### Modified Files:
```
✓ FocusDock.App/App.xaml.cs (Add DI container)
✓ MainWindow.xaml.cs (Use ServiceProvider)
✓ StudySessionWindow.xaml.cs (Inject services)
✓ FocusDock.sln (Add 2 new projects)
```

---

## Build & Test Checklist

- [ ] Create FocusDeck.Shared project
- [ ] Create FocusDeck.Services project
- [ ] Move all models to Shared
- [ ] Create platform service interfaces
- [ ] Implement WindowsPlatformService
- [ ] Update ServiceConfiguration
- [ ] Update App.xaml.cs to use DI
- [ ] Update all services to use IPlatformService
- [ ] Update App to inject services
- [ ] Build: `dotnet build` (0 errors)
- [ ] Run app and verify still works
- [ ] Test study session creation
- [ ] Test analytics loading
- [ ] Commit to git: "Phase 5a: Cross-platform refactoring"

---

## Expected Outcome

After Phase 5a:
- ✅ All platform-agnostic code separated
- ✅ Services receive dependencies via constructor
- ✅ Easy to add iOS, Android, Web implementations
- ✅ Desktop app still fully functional
- ✅ Build time: ~2-3 seconds
- ✅ 0 warnings, 0 errors
- ✅ Ready for mobile in Phase 6

---

## Next Phase (After 5a Complete)
- Phase 5b: Voice Notes & Transcription (using IAudioRecordingService)
- Phase 5c: AI Recommendations (clean service interface)
- Phase 5d: Music & Breaks (pluggable implementations)

All future phases build on this foundation, making cross-platform support trivial.
