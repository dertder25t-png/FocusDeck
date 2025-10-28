# Phase 6b Week 2: Study Timer Page Implementation

## Overview
This week focuses on implementing the Study Timer page for the MAUI mobile app - the core feature that drives the entire FocusDeck experience.

---

## 📋 Objectives

**Primary Goal**: Create a fully functional Study Timer page with a 25-minute default timer, customizable controls, and session persistence.

**Key Features**:
- ✅ Large, easy-to-read timer display (MM:SS format)
- ✅ Play/Pause/Stop/Reset controls
- ✅ Automatic session save on timer completion
- ✅ Audio & haptic feedback
- ✅ MVVM data binding with real-time updates
- ✅ Session history integration

---

## 🎯 Tasks

### Task 1: Create StudyTimerViewModel
**Time**: 2 hours
**File**: `src/FocusDeck.Mobile/ViewModels/StudyTimerViewModel.cs`

**Requirements**:
```csharp
public class StudyTimerViewModel : BaseViewModel
{
    // Properties
    public TimeSpan TotalTime { get; set; } = TimeSpan.FromMinutes(25);
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan RemainingTime { get; set; }
    
    // State
    public TimerState CurrentState { get; set; } // Running, Paused, Stopped
    public bool IsRunning { get; }
    public bool IsPaused { get; }
    
    // Customization
    public int MinutesInput { get; set; } = 25;
    public int SecondsInput { get; set; } = 0;
    
    // Progress
    public double ProgressPercentage { get; } // 0-100
    
    // Commands
    public IRelayCommand StartCommand { get; }
    public IRelayCommand PauseCommand { get; }
    public IRelayCommand ResumeCommand { get; }
    public IRelayCommand StopCommand { get; }
    public IRelayCommand ResetCommand { get; }
    public IRelayCommand SetCustomTimeCommand { get; }
    
    // Events
    public event EventHandler? TimerCompleted;
    public event EventHandler<string>? MessageChanged;
    
    // Implementation
    private void TimerTick() { }
    private async Task SaveSessionAsync() { }
    private void PlayCompletionSound() { }
}
```

**Acceptance Criteria**:
- [ ] ViewModel inherits from `BaseViewModel`
- [ ] All properties properly notify changes
- [ ] Timer state machine implemented (Stopped → Running → Paused → Running → Stopped)
- [ ] Session saves automatically on completion
- [ ] Commands use MVVM Toolkit `IRelayCommand`
- [ ] No UI code in ViewModel

---

### Task 2: Design StudyTimerPage UI
**Time**: 2 hours
**File**: `src/FocusDeck.Mobile/Views/StudyTimerPage.xaml`

**Layout Structure**:
```
┌─────────────────────────────┐
│        FocusDeck            │ Header
├─────────────────────────────┤
│                             │
│     MM : SS                 │ Large Timer Display
│                             │ (Grid.Column 1, Row 1)
│                             │
├─────────────────────────────┤
│                             │
│    ▓▓▓▓░░░░░░░░ 45%        │ Progress Bar
│                             │
├─────────────────────────────┤
│  Elapsed: 11:15  │ Remaining│ Stats Row
├─────────────────────────────┤
│   [◼ Stop]  [▶ Play]       │ Main Controls
│   [⏸ Pause] [↻ Reset]     │
├─────────────────────────────┤
│  Custom Time:               │
│  [__25__] min [__0__] sec   │
│  [Set] [Load Presets]       │
├─────────────────────────────┤
│  Session Notes:             │
│  [________________]         │ Text input
│                             │
│  [ Session History ]        │ Navigation
│                             │
└─────────────────────────────┘
```

**XAML Elements**:
- `Grid` with 3 columns for layout
- `Label` with large font (48pt) for MM:SS display
- `ProgressBar` with percentage
- `Button` for controls (custom styles)
- `Entry` for custom time input
- `Picker` for preset durations
- `CollectionView` for session quick-links

**Styling**:
- Purple theme (#512BD4)
- Large, tap-friendly buttons (60x60pt minimum)
- Light background, dark text
- Responsive layout (adapts to portrait/landscape)

**Acceptance Criteria**:
- [ ] UI renders on Android device/emulator
- [ ] All buttons properly styled and themed
- [ ] Timer display uses monospace font
- [ ] Progress bar shows real-time updates
- [ ] Layout works in portrait and landscape
- [ ] Buttons are accessibility-compliant (large tap targets)

---

### Task 3: Implement StudyTimerPage Code-Behind
**Time**: 1.5 hours
**File**: `src/FocusDeck.Mobile/Views/StudyTimerPage.xaml.cs`

**Requirements**:
```csharp
public partial class StudyTimerPage : ContentPage
{
    private StudyTimerViewModel _viewModel;
    
    public StudyTimerPage()
    {
        InitializeComponent();
        _viewModel = new StudyTimerViewModel();
        BindingContext = _viewModel;
        
        // Subscribe to events
        _viewModel.TimerCompleted += OnTimerCompleted;
        _viewModel.MessageChanged += OnMessageChanged;
    }
    
    private void OnTimerCompleted(object? sender, EventArgs e) { }
    private void OnMessageChanged(object? sender, string message) { }
    private void OnPresetSelected(object? sender, EventArgs e) { }
}
```

**Acceptance Criteria**:
- [ ] ViewModel properly bound
- [ ] Event handlers implemented
- [ ] No code-behind logic (only event forwarding)
- [ ] Page lifecycle handled correctly

---

### Task 4: Integrate Audio & Haptic Feedback
**Time**: 2 hours
**Files**: 
- Update `IMobileAudioRecordingService.cs` for playback
- Update `StudyTimerViewModel.cs` with feedback calls

**Requirements**:
- Completion sound (0.5-second tone or sound file)
- 3 haptic pulses on completion
- Optional: Subtle sound every 5 minutes (notification tone)
- Mutable: User can disable sounds in settings

**Implementation**:
```csharp
// In StudyTimerViewModel
private async Task PlayCompletionSound()
{
    try
    {
        await _audioService.PlayAsync("completion_sound.wav", volume: 0.8);
        await Vibration.Default.Vibrate(milliseconds: 100);
        // 3 pulses
        await Task.Delay(100);
        await Vibration.Default.Vibrate(milliseconds: 100);
        await Task.Delay(100);
        await Vibration.Default.Vibrate(milliseconds: 100);
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Audio feedback error: {ex.Message}");
    }
}
```

**Acceptance Criteria**:
- [ ] Completion sound plays reliably
- [ ] Haptic feedback triggers on all devices
- [ ] Audio mutes with system volume
- [ ] Haptic mutable via settings

---

### Task 5: Implement Session Persistence
**Time**: 2 hours
**File**: Update `StudyTimerViewModel.cs`

**Requirements**:
- Save session when timer completes
- Include: Start time, end time, duration, notes
- Store in local database (SQLite via Entity Framework)
- Make sessionhistory queryable

**Schema**:
```csharp
public class StudySession
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }  // CSV
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool SyncedToCloud { get; set; } = false;
}
```

**Implementation**:
```csharp
private async Task SaveSessionAsync()
{
    try
    {
        var session = new StudySession
        {
            StartTime = _sessionStartTime,
            EndTime = DateTime.Now,
            Duration = TotalTime,
            Notes = SessionNotes,
            Tags = "timer,mobile",
            CreatedAt = DateTime.UtcNow,
            SyncedToCloud = false
        };
        
        await _sessionService.AddSessionAsync(session);
        MessageChanged?.Invoke(this, "Session saved!");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Save error: {ex.Message}");
    }
}
```

**Acceptance Criteria**:
- [ ] Sessions save to local database on completion
- [ ] Session data includes all required fields
- [ ] Sessions can be queried by date range
- [ ] Sessions sync flag ready for Phase 6b Week 3

---

### Task 6: MVVM Data Binding & Commands
**Time**: 1.5 hours
**Update**: `StudyTimerPage.xaml` with bindings

**XAML Bindings**:
```xml
<!-- Timer Display -->
<Label Text="{Binding DisplayTime, StringFormat='{0:mm\\:ss}'}" 
        FontSize="64"
        HorizontalTextAlignment="Center" />

<!-- Progress Bar -->
<ProgressBar Progress="{Binding ProgressPercentage, Converter={StaticResource PercentageConverter}}"
             HorizontalOptions="FillAndExpand" />

<!-- Controls -->
<Button Text="Start" 
        Command="{Binding StartCommand}"
        IsEnabled="{Binding IsNotRunning}" />

<Button Text="Pause" 
        Command="{Binding PauseCommand}"
        IsEnabled="{Binding IsRunning}" />

<!-- Custom Time Input -->
<Entry Text="{Binding MinutesInput, Mode=TwoWay}"
       Placeholder="0"
       Keyboard="Numeric" />

<Button Text="Set" 
        Command="{Binding SetCustomTimeCommand}" />
```

**Converters** (if needed):
```csharp
public class PercentageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
            return percentage / 100.0;
        return 0.0;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

**Acceptance Criteria**:
- [ ] All UI updates through bindings
- [ ] No code-behind property manipulation
- [ ] Commands execute from XAML
- [ ] Display formats are correct (MM:SS)
- [ ] Button enabled/disabled states reactive

---

### Task 7: Create Timer Page Style Resources
**Time**: 1 hour
**File**: Create `src/FocusDeck.Mobile/Resources/Styles/StudyTimerStyles.xaml`

**Styles**:
```xml
<Style TargetType="Button" x:Key="TimerButtonStyle">
    <Setter Property="BackgroundColor" Value="#512BD4" />
    <Setter Property="TextColor" Value="White" />
    <Setter Property="CornerRadius" Value="25" />
    <Setter Property="Padding" Value="20,10" />
    <Setter Property="FontSize" Value="16" />
    <Setter Property="FontAttributes" Value="Bold" />
</Style>

<Style TargetType="Label" x:Key="TimerDisplayStyle">
    <Setter Property="FontSize" Value="64" />
    <Setter Property="FontAttributes" Value="Bold" />
    <Setter Property="FontFamily" Value="monospace" />
    <Setter Property="TextColor" Value="#1F1F1F" />
    <Setter Property="HorizontalTextAlignment" Value="Center" />
</Style>

<Color x:Key="PrimaryColor">#512BD4</Color>
<Color x:Key="TextColor">#1F1F1F</Color>
<Color x:Key="LightBackground">#F5F5F5</Color>
```

**Acceptance Criteria**:
- [ ] Styles properly defined
- [ ] Colors consistent with app theme
- [ ] Styles applied to all controls
- [ ] No hardcoded colors in XAML

---

### Task 8: Testing & Verification
**Time**: 2 hours

**Manual Tests**:
- [ ] Timer starts and counts down correctly
- [ ] Pause/resume works properly
- [ ] Stop ends session and clears timer
- [ ] Reset returns to set time
- [ ] Completion sound plays
- [ ] Haptic feedback triggers
- [ ] Sessions save to database
- [ ] Custom time input works
- [ ] UI responsive to rapid button taps
- [ ] No crashes on rotation

**Automated Tests** (if time allows):
```csharp
[TestClass]
public class StudyTimerViewModelTests
{
    [TestMethod]
    public void StartCommand_StartsTimer() { }
    
    [TestMethod]
    public void PauseCommand_PausesTimer() { }
    
    [TestMethod]
    public void TimerComplete_SavesSession() { }
    
    [TestMethod]
    public void CustomTime_UpdatesDisplay() { }
}
```

**Acceptance Criteria**:
- [ ] All manual tests pass
- [ ] No console errors
- [ ] Timer accurate within 1 second
- [ ] Database queries work
- [ ] UI threads not blocked

---

## 📊 Success Criteria

**Build**: ✅ Compiles with 0 errors

**Functionality**:
- ✅ Timer displays correctly and counts down
- ✅ Controls (Start/Pause/Stop/Reset) all work
- ✅ Audio & haptic feedback on completion
- ✅ Custom time input functional
- ✅ Sessions persist to database
- ✅ UI responsive and accessible

**Code Quality**:
- ✅ MVVM pattern properly implemented
- ✅ No code-behind logic
- ✅ Proper error handling
- ✅ Debug output in place

**Performance**:
- ✅ Timer accurate (±1 second)
- ✅ No UI lag during countdown
- ✅ Memory usage stable
- ✅ Database queries optimized

---

## 📦 Dependencies

### NuGet Packages
- `CommunityToolkit.Mvvm` - Already included
- `Microsoft.Maui.Controls` - Already included
- `Essentials` (for Vibration) - Already included
- `Microsoft.EntityFrameworkCore` - Already included

### Files Modified
- `src/FocusDeck.Mobile/ViewModels/StudyTimerViewModel.cs` (NEW)
- `src/FocusDeck.Mobile/Views/StudyTimerPage.xaml` (UPDATE)
- `src/FocusDeck.Mobile/Views/StudyTimerPage.xaml.cs` (NEW)
- `src/FocusDeck.Mobile/Resources/Styles/StudyTimerStyles.xaml` (NEW)
- `src/FocusDeck.Mobile/AppShell.xaml` (UPDATE - route binding)
- `src/FocusDeck.Mobile/MauiProgram.cs` (UPDATE - service registration)

---

## 📅 Timeline

| Task | Estimated | Priority |
|------|-----------|----------|
| Task 1: ViewModel | 2h | Critical |
| Task 2: UI Design | 2h | Critical |
| Task 3: Code-Behind | 1.5h | Critical |
| Task 4: Audio/Haptic | 2h | High |
| Task 5: Persistence | 2h | High |
| Task 6: Data Binding | 1.5h | High |
| Task 7: Styles | 1h | Medium |
| Task 8: Testing | 2h | High |
| **TOTAL** | **14 hours** | |

---

## 🎯 Deliverables

**End of Week 2**:
- ✅ Fully functional study timer page
- ✅ MVVM implementation complete
- ✅ Sessions persisting to database
- ✅ Build: 0 Errors
- ✅ Git commit: "Phase 6b Week 2: Study Timer Page Implementation"

**Ready for Week 3**:
- Database schema finalized
- Session queries operational
- Foundation for sync mechanism in place

---

## 🔗 References

- Phase 6a Documentation: `docs/PHASE6a_READY.md`
- MAUI Documentation: https://learn.microsoft.com/maui/
- MVVM Toolkit: https://github.com/CommunityToolkit/dotnet
- Entity Framework Core: https://learn.microsoft.com/ef/core/
