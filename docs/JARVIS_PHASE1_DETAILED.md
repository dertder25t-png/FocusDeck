#  JARVIS Phase 1: Activity Detection Foundation (Weeks 1-6)

**Phase Status:** Ready to Start  
**Difficulty:** Medium  
**Time Estimate:** 6 weeks (full-time)  

---

## Overview

Phase 1 is the **critical foundation** for the entire JARVIS system. Everything else depends on the ability to detect what the student is doing across all three platforms.

### What We're Building

1. **IActivityDetectionService** - Cross-platform interface for activity monitoring
2. **Windows Implementation** - WinEventHook + process tracking
3. **Linux Implementation** - wmctrl/xdotool integration
4. **Mobile Implementation** - MAUI device motion sensors
5. **IContextAggregationService** - Merge all activity data into unified state

---

## Success Criteria

 **Accuracy:** >95% correct app detection  
 **Performance:** <5% CPU overhead  
 **Latency:** Context updates <100ms  
 **Cross-Platform:** Consistent behavior on Windows, Linux, Android  

---

## Detailed Breakdown

### Week 1: Interface Design & Architecture

#### Task 1.1: Design IActivityDetectionService

**Goal:** Create the interface that all platforms will implement

**Deliverables:**
`csharp
// src/FocusDeck.Services/Activity/IActivityDetectionService.cs

public interface IActivityDetectionService
{
    /// Get current activity state across all platforms
    Task<ActivityState> GetCurrentActivityAsync(CancellationToken ct);
    
    /// Subscribe to activity changes (real-time)
    IObservable<ActivityState> ActivityChanged { get; }
    
    /// Focused window/app details
    Task<FocusedApplication> GetFocusedApplicationAsync(CancellationToken ct);
    
    /// Idle detection
    Task<bool> IsIdleAsync(int idleThresholdSeconds, CancellationToken ct);
    
    /// Keyboard/mouse activity in last N minutes
    Task<double> GetActivityIntensityAsync(int minutesWindow, CancellationToken ct);
}

public class ActivityState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // What app/window is currently focused?
    public FocusedApplication? FocusedApp { get; set; }
    
    // How intense is the current activity? (0-100)
    public int ActivityIntensity { get; set; }
    
    // Is the user idle?
    public bool IsIdle { get; set; }
    
    // What assignments/notes are currently open?
    public List<ContextItem> OpenContexts { get; set; } = [];
    
    // Timestamp
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class FocusedApplication
{
    public string AppName { get; set; } = string.Empty;  // Chrome, Word, etc.
    public string WindowTitle { get; set; } = string.Empty;  // Full window title
    public string ProcessPath { get; set; } = string.Empty;  // Full path to executable
    public string[] Tags { get; set; } = [];  // "productivity", "distraction", "study", etc.
    public DateTime SwitchedAt { get; set; }
}

public class ContextItem
{
    public string Type { get; set; } = string.Empty;  // "note", "canvas_assignment", "file"
    public string Title { get; set; } = string.Empty;
    public Guid? RelatedId { get; set; }  // Link to DB entity
}
`

**Implementation Notes:**
- Use IObservable<T> for real-time activity changes
- Activity intensity based on keyboard/mouse frequency
- Platform-specific implementations in separate projects

**Tests:**
- [ ] Interface compiles without errors
- [ ] Tests can mock the service
- [ ] All platform implementations can be dependency-injected

---

#### Task 1.2: Create ActivityDetectionService (Base/Shared)

**Goal:** Create base class and platform-agnostic code

**File:** src/FocusDeck.Services/Activity/ActivityDetectionService.cs

`csharp
public abstract class ActivityDetectionService : IActivityDetectionService
{
    protected readonly ILogger<ActivityDetectionService> _logger;
    protected readonly Subject<ActivityState> _activityChanged = new();
    
    private ActivityState _lastState = new();
    private DateTime _lastActivity = DateTime.UtcNow;
    private const int IdleThresholdMs = 60000;  // 60 seconds
    
    public IObservable<ActivityState> ActivityChanged => _activityChanged.AsObservable();
    
    protected ActivityDetectionService(ILogger<ActivityDetectionService> logger)
    {
        _logger = logger;
    }
    
    // Platform-specific: Get focused application
    protected abstract Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct);
    
    // Platform-specific: Detect keyboard/mouse activity
    protected abstract Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct);
    
    public virtual async Task<ActivityState> GetCurrentActivityAsync(CancellationToken ct)
    {
        var focusedApp = await GetFocusedApplicationInternalAsync(ct);
        var intensity = await GetActivityIntensityInternalAsync(1, ct);
        
        var state = new ActivityState
        {
            FocusedApp = focusedApp,
            ActivityIntensity = intensity,
            IsIdle = DateTime.UtcNow - _lastActivity > TimeSpan.FromMilliseconds(IdleThresholdMs),
            Timestamp = DateTime.UtcNow
        };
        
        // Publish change if state differs
        if (state.FocusedApp?.AppName != _lastState.FocusedApp?.AppName)
        {
            _activityChanged.OnNext(state);
            _logger.LogInformation("App focus changed: {App}", state.FocusedApp?.AppName);
        }
        
        _lastState = state;
        return state;
    }
    
    public virtual Task<bool> IsIdleAsync(int idleThresholdSeconds, CancellationToken ct)
    {
        var isIdle = DateTime.UtcNow - _lastActivity > TimeSpan.FromSeconds(idleThresholdSeconds);
        return Task.FromResult(isIdle);
    }
    
    // Record activity (called by platform implementations)
    protected void RecordActivity()
    {
        _lastActivity = DateTime.UtcNow;
    }
}
`

**Tests:**
- [ ] Abstract class can be instantiated via mock
- [ ] Activity change detection works
- [ ] Idle detection threshold is respected

---

### Week 2: Windows Implementation

#### Task 2.1: Windows Activity Detection (WinEventHook)

**File:** src/FocusDeck.Desktop/Services/WindowsActivityDetectionService.cs

**Goal:** Track focused window and activity on Windows

`csharp
#if NET8_0_WINDOWS || WINDOWS

public class WindowsActivityDetectionService : ActivityDetectionService
{
    private readonly ILogger<WindowsActivityDetectionService> _logger;
    private IntPtr _m_hook;
    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    private HookProc _hookProc;
    
    // P/Invoke declarations
    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventHook, HookProc lpfnWinEventHook, uint idProcess, uint idThread, uint dwFlags);
    
    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    
    private const uint EVENT_SYSTEM_FOREGROUND = 3;
    private const uint EVENT_SYSTEM_FOCUS = 4;
    
    public WindowsActivityDetectionService(ILogger<WindowsActivityDetectionService> logger) 
        : base(logger)
    {
        _logger = logger;
        _hookProc = HookHandler;
        
        // Set up the hook
        _m_hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _hookProc, 0, 0, 0);
    }
    
    private IntPtr HookHandler(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            RecordActivity();
        }
        return IntPtr.Zero;
    }
    
    protected override async Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct)
    {
        try
        {
            var foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return null;
            
            var sb = new StringBuilder(256);
            GetWindowText(foregroundWindow, sb, 256);
            var windowTitle = sb.ToString();
            
            // Get process name from window handle
            _ = GetWindowThreadProcessId(foregroundWindow, out var pid);
            var process = Process.GetProcessById((int)pid);
            
            return new FocusedApplication
            {
                WindowTitle = windowTitle,
                AppName = process.ProcessName,
                ProcessPath = process.MainModule?.FileName ?? string.Empty,
                Tags = ClassifyApplication(process.ProcessName),
                SwitchedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get focused application");
            return null;
        }
    }
    
    protected override Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct)
    {
        // TODO: Hook keyboard/mouse to measure intensity
        // For now, return 50 (placeholder)
        return Task.FromResult(50);
    }
    
    private string[] ClassifyApplication(string appName)
    {
        return appName.ToLowerInvariant() switch
        {
            "winword" or "excel" or "powerpnt" => ["productivity"],
            "chrome" or "firefox" or "msedge" => ["browser"],
            "discord" or "slack" => ["distraction"],
            "spotify" => ["focus_music"],
            "notepad" or "code" => ["coding"],
            _ => ["other"]
        };
    }
    
    ~WindowsActivityDetectionService()
    {
        if (_m_hook != IntPtr.Zero)
            UnhookWinEvent(_m_hook);
    }
}

#endif
`

**Tests:**
- [ ] Hook initialized without crashing
- [ ] Focused window detected correctly
- [ ] App classification works
- [ ] Performance impact <5% CPU

---

### Week 3: Linux & Mobile Implementation

#### Task 3.1: Linux Activity Detection

**File:** src/FocusDeck.Server/Services/Activity/LinuxActivityDetectionService.cs

**Goal:** Track focused window on Linux using wmctrl/xdotool

`csharp
public class LinuxActivityDetectionService : ActivityDetectionService
{
    private readonly IProcessService _processService;
    private DateTime _lastCheck = DateTime.UtcNow;
    private FocusedApplication? _lastApp;
    
    public LinuxActivityDetectionService(ILogger<LinuxActivityDetectionService> logger, IProcessService processService) 
        : base(logger)
    {
        _processService = processService;
    }
    
    protected override async Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct)
    {
        try
        {
            // Use wmctrl to get focused window
            var output = await _processService.ExecuteAsync("wmctrl", "-l", ct);
            
            // Parse wmctrl output (format: id desk x y w h host client)
            var lines = output.Split('\n');
            var focusedLine = lines.FirstOrDefault(l => l.Contains('*'));  // * indicates focused
            
            if (focusedLine == null)
                return null;
            
            var parts = focusedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var appName = parts.Last();
            
            // Use xdotool to get window ID and details
            var windowIdOutput = await _processService.ExecuteAsync("xdotool", "getactivewindow", ct);
            var windowId = windowIdOutput.Trim();
            
            return new FocusedApplication
            {
                WindowTitle = appName,
                AppName = ExtractAppName(appName),
                ProcessPath = windowId,  // Store window ID
                Tags = ClassifyApplication(appName),
                SwitchedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get focused application on Linux");
            return null;
        }
    }
    
    protected override Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct)
    {
        // TODO: Monitor /proc/interrupts for keyboard input
        return Task.FromResult(50);  // Placeholder
    }
}
`

#### Task 3.2: Mobile Activity Detection (MAUI)

**File:** src/FocusDeck.Mobile/Services/MobileActivityDetectionService.cs

**Goal:** Detect user motion and app foreground/background state

`csharp
#if NET8_0_ANDROID

public class MobileActivityDetectionService : ActivityDetectionService
{
    private readonly Accelerometer _accelerometer;
    private readonly Gyroscope _gyroscope;
    private int _motionCount;
    
    public MobileActivityDetectionService(ILogger<MobileActivityDetectionService> logger) 
        : base(logger)
    {
        _accelerometer = Accelerometer.Default;
        _gyroscope = Gyroscope.Default;
        
        // Start monitoring sensors
        if (_accelerometer.IsSupported)
            _accelerometer.ReadingChanged += OnAccelerometerReadingChanged;
        
        if (_gyroscope.IsSupported)
            _gyroscope.ReadingChanged += OnGyroscopeReadingChanged;
    }
    
    protected override Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct)
    {
        // On mobile, "current app" is always FocusDeck when app is foreground
        return Task.FromResult(new FocusedApplication
        {
            AppName = "FocusDeck",
            WindowTitle = "FocusDeck Study Timer",
            ProcessPath = "",
            Tags = ["focus"],
            SwitchedAt = DateTime.UtcNow
        });
    }
    
    protected override Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct)
    {
        // Motion count over N minutes = intensity
        var intensity = Math.Min(_motionCount, 100);
        _motionCount = 0;  // Reset
        return Task.FromResult(intensity);
    }
    
    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        // Motion detected
        _motionCount++;
        RecordActivity();
    }
    
    private void OnGyroscopeReadingChanged(object? sender, GyroscopeChangedEventArgs e)
    {
        // Rotation detected
        _motionCount++;
        RecordActivity();
    }
}

#endif
`

---

### Week 4: Context Aggregation & Integration

#### Task 4.1: IContextAggregationService

**File:** src/FocusDeck.Services/Activity/IContextAggregationService.cs

`csharp
public interface IContextAggregationService
{
    /// Get current aggregated context (merged from all sources)
    Task<StudentContext> GetAggregatedContextAsync(Guid studentId, CancellationToken ct);
    
    /// Subscribe to context changes in real-time
    IObservable<StudentContext> ContextChanged { get; }
}

public class ContextAggregationService : IContextAggregationService
{
    private readonly IActivityDetectionService _activityDetection;
    private readonly ICanvasService _canvas;
    private readonly INoteService _notes;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;
    private readonly Subject<StudentContext> _contextChanged = new();
    
    public IObservable<StudentContext> ContextChanged => _contextChanged.AsObservable();
    
    public async Task<StudentContext> GetAggregatedContextAsync(Guid studentId, CancellationToken ct)
    {
        // Gather from all sources in parallel
        var activityTask = _activityDetection.GetCurrentActivityAsync(ct);
        var assignmentsTask = _canvas.GetUpcomingAssignmentsAsync(studentId, ct);
        var openNotesTask = _notes.GetRecentNotesAsync(studentId, limit: 5, ct);
        
        await Task.WhenAll(activityTask, assignmentsTask, openNotesTask);
        
        var context = new StudentContext
        {
            StudentId = studentId,
            CurrentActivity = activityTask.Result,
            UpcomingAssignments = assignmentsTask.Result,
            RecentNotes = openNotesTask.Result,
            Timestamp = DateTime.UtcNow
        };
        
        // Broadcast via SignalR
        await _hubContext.Clients.User(studentId.ToString())
            .SendAsync("ContextUpdated", context, ct);
        
        _contextChanged.OnNext(context);
        return context;
    }
}
`

#### Task 4.2: Database Schema Updates

**File:** src/FocusDeck.Persistence/Configurations/StudentContextConfiguration.cs

`csharp
public class StudentContextConfiguration : IEntityTypeConfiguration<StudentContext>
{
    public void Configure(EntityTypeBuilder<StudentContext> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.StudentId).IsRequired();
        builder.Property(e => e.CurrentApp).IsRequired(false).HasMaxLength(256);
        builder.Property(e => e.FocusLevel).HasDefaultValue(0);
        builder.Property(e => e.IsIdle).HasDefaultValue(false);
        builder.Property(e => e.ActivityIntensity).HasDefaultValue(0);
        builder.Property(e => e.Timestamp).IsRequired();
        
        // Indexes for queries
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.StudentId, e.Timestamp });
    }
}
`

#### Task 4.3: DI Registration

**File:** src/FocusDeck.Server/Program.cs (Add to existing service registration)

`csharp
// Activity Detection
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddScoped<IActivityDetectionService, WindowsActivityDetectionService>();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddScoped<IActivityDetectionService, LinuxActivityDetectionService>();
}

// Context Aggregation
builder.Services.AddScoped<IContextAggregationService, ContextAggregationService>();

// Background job for continuous context polling
RecurringJob.AddOrUpdate<IContextAggregationService>(
    "aggregate-student-context",
    x => x.GetAggregatedContextAsync(Guid.Empty, CancellationToken.None),
    Cron.EveryMinute);
`

---

##  Task Checklist

### Week 1: Interface Design
- [ ] IActivityDetectionService interface created and reviewed
- [ ] ActivityState class defined
- [ ] Base ActivityDetectionService abstract class implemented
- [ ] Unit tests for base class
- [ ] Code reviewed and merged to develop

### Week 2: Windows
- [ ] WindowsActivityDetectionService implemented
- [ ] WinEventHook working for window focus changes
- [ ] Process name/path extraction working
- [ ] App classification working
- [ ] Integration tests pass
- [ ] <5% CPU overhead verified

### Week 3: Linux & Mobile
- [ ] LinuxActivityDetectionService implemented
- [ ] wmctrl/xdotool integration working
- [ ] MobileActivityDetectionService implemented
- [ ] MAUI sensor integration working
- [ ] All platform implementations tested

### Week 4: Integration
- [ ] IContextAggregationService implemented
- [ ] Database entities and configurations created
- [ ] DI registration completed
- [ ] End-to-end integration tests pass
- [ ] SignalR real-time context broadcasts working
- [ ] Performance benchmarked (<100ms aggregation latency)
- [ ] Ready for Phase 2

---

## Testing Strategy

### Unit Tests
- Mock IActivityDetectionService in services
- Test ActivityState creation and mutation
- Test activity intensity calculations
- Test app classification logic

### Integration Tests
- Test Windows hook with actual window focus changes
- Test Linux wmctrl output parsing
- Test context aggregation with mock Canvas/Notes services
- Test SignalR broadcasts

### Performance Tests
- CPU usage monitoring (target: <5% overhead)
- Context aggregation latency (target: <100ms)
- Memory usage (monitor for leaks)

---

## Resources & Documentation

- **Windows API:** https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook
- **wmctrl:** man wmctrl
- **MAUI Sensors:** https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/sensors
- **RxDotNet:** https://github.com/dotnet/reactive

---

**Next Phase:** Once Phase 1 is complete, move to Phase 2 (Burnout Detection & Prevention)
