using System.Diagnostics;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FocusDeck.Mobile.ViewModels;

/// <summary>
/// ViewModel for the Study Timer page.
/// Manages timer state, controls, and session persistence.
/// </summary>
public partial class StudyTimerViewModel : ObservableObject
{
    private IDispatcherTimer? _timer;
    private DateTime _sessionStartTime;

    /// <summary>
    /// Total time set for this session (user-configurable)
    /// </summary>
    [ObservableProperty]
    private TimeSpan totalTime = TimeSpan.FromMinutes(25);

    /// <summary>
    /// Time elapsed since session started
    /// </summary>
    [ObservableProperty]
    private TimeSpan elapsedTime = TimeSpan.Zero;

    /// <summary>
    /// Current timer state
    /// </summary>
    [ObservableProperty]
    private TimerState currentState = TimerState.Stopped;

    /// <summary>
    /// Custom minutes input (for user to set time)
    /// </summary>
    [ObservableProperty]
    private int minutesInput = 25;

    /// <summary>
    /// Session notes (user can add notes during session)
    /// </summary>
    [ObservableProperty]
    private string sessionNotes = string.Empty;

    /// <summary>
    /// Current status message to display
    /// </summary>
    [ObservableProperty]
    private string statusMessage = "Ready to start";

    /// <summary>
    /// Time remaining in session (computed)
    /// </summary>
    public TimeSpan RemainingTime => TotalTime - ElapsedTime;

    /// <summary>
    /// Display format: "MM:SS"
    /// </summary>
    public string DisplayTime => RemainingTime.ToString(@"mm\:ss");

    /// <summary>
    /// Progress as percentage (0-100)
    /// </summary>
    public double ProgressPercentage => TotalTime.TotalSeconds > 0
        ? (ElapsedTime.TotalSeconds / TotalTime.TotalSeconds) * 100
        : 0;

    /// <summary>
    /// Is timer currently running?
    /// </summary>
    public bool IsRunning => CurrentState == TimerState.Running;

    /// <summary>
    /// Is timer currently paused?
    /// </summary>
    public bool IsPaused => CurrentState == TimerState.Paused;

    /// <summary>
    /// Is timer NOT running (stopped or paused)?
    /// </summary>
    public bool IsNotRunning => !IsRunning;

    /// <summary>
    /// Elapsed time formatted as "HH:MM:SS"
    /// </summary>
    public string FormattedElapsedTime => ElapsedTime.ToString(@"hh\:mm\:ss");

    /// <summary>
    /// Remaining time formatted as "HH:MM:SS"
    /// </summary>
    public string FormattedRemainingTime => RemainingTime.ToString(@"hh\:mm\:ss");

    /// <summary>
    /// Event raised when timer completes
    /// </summary>
    public event EventHandler? TimerCompleted;

    /// <summary>
    /// Event raised when message should be displayed
    /// </summary>
    public event EventHandler<string>? MessageChanged;

    public StudyTimerViewModel()
    {
        _sessionStartTime = DateTime.Now;
        InitializeTimer();
    }

    /// <summary>
    /// Initialize the timer
    /// </summary>
    private void InitializeTimer()
    {
#pragma warning disable CS8602
        _timer = Dispatcher.GetForCurrentThread().CreateTimer();
#pragma warning restore CS8602
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += OnTimerTick;
        }
    }

    /// <summary>
    /// Start timer from current position
    /// </summary>
    [RelayCommand]
    private void Start()
    {
        if (CurrentState == TimerState.Stopped)
        {
            _sessionStartTime = DateTime.Now - ElapsedTime;
            Debug.WriteLine("[Timer] Started new session");
        }
        else if (CurrentState == TimerState.Paused)
        {
            _sessionStartTime = DateTime.Now - ElapsedTime;
            Debug.WriteLine("[Timer] Resumed from pause");
        }

        CurrentState = TimerState.Running;
        _timer?.Start();
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsNotRunning));
        MessageChanged?.Invoke(this, "Timer started");
    }

    /// <summary>
    /// Pause timer (can be resumed)
    /// </summary>
    [RelayCommand]
    private void Pause()
    {
        if (CurrentState == TimerState.Running)
        {
            CurrentState = TimerState.Paused;
            _timer?.Stop();
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsRunning));
            MessageChanged?.Invoke(this, "Timer paused");
            Debug.WriteLine("[Timer] Paused");
        }
    }

    /// <summary>
    /// Stop timer and save session
    /// </summary>
    [RelayCommand]
    private async Task Stop()
    {
        if (CurrentState != TimerState.Stopped)
        {
            _timer?.Stop();
            CurrentState = TimerState.Stopped;
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsNotRunning));
            MessageChanged?.Invoke(this, "Session stopped");

            // Save session to database
            await SaveSessionAsync();
            Debug.WriteLine("[Timer] Stopped and saved");
        }
    }

    /// <summary>
    /// Reset timer to initial state
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        _timer?.Stop();
        CurrentState = TimerState.Stopped;
        ElapsedTime = TimeSpan.Zero;
        SessionNotes = string.Empty;
        OnPropertyChanged(nameof(DisplayTime));
        OnPropertyChanged(nameof(FormattedElapsedTime));
        OnPropertyChanged(nameof(FormattedRemainingTime));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsNotRunning));
        MessageChanged?.Invoke(this, "Timer reset");
        Debug.WriteLine("[Timer] Reset");
    }

    /// <summary>
    /// Set custom time from user input
    /// </summary>
    [RelayCommand]
    private void SetCustomTime()
    {
        try
        {
            if (MinutesInput < 0 || MinutesInput > 180)
            {
                MessageChanged?.Invoke(this, "Invalid time (0-180 minutes)");
                return;
            }

            TotalTime = new TimeSpan(0, MinutesInput, 0);
            ElapsedTime = TimeSpan.Zero;
            CurrentState = TimerState.Stopped;

            OnPropertyChanged(nameof(DisplayTime));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(FormattedRemainingTime));
            OnPropertyChanged(nameof(RemainingTime));

            MessageChanged?.Invoke(this, $"Time set to {MinutesInput} minutes");
            Debug.WriteLine($"[Timer] Custom time set: {TotalTime}");
        }
        catch (Exception ex)
        {
            MessageChanged?.Invoke(this, $"Error setting time: {ex.Message}");
            Debug.WriteLine($"[Timer] Error setting custom time: {ex}");
        }
    }

    /// <summary>
    /// Set timer to 15 minutes (preset)
    /// </summary>
    [RelayCommand]
    private void Set15Minutes()
    {
        TotalTime = TimeSpan.FromMinutes(15);
        MinutesInput = 15;
        ElapsedTime = TimeSpan.Zero;
        CurrentState = TimerState.Stopped;
        OnPropertyChanged(nameof(DisplayTime));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(FormattedRemainingTime));
        OnPropertyChanged(nameof(RemainingTime));
        MessageChanged?.Invoke(this, "Time set to 15 minutes");
    }

    /// <summary>
    /// Set timer to 25 minutes (preset - Pomodoro default)
    /// </summary>
    [RelayCommand]
    private void Set25Minutes()
    {
        TotalTime = TimeSpan.FromMinutes(25);
        MinutesInput = 25;
        ElapsedTime = TimeSpan.Zero;
        CurrentState = TimerState.Stopped;
        OnPropertyChanged(nameof(DisplayTime));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(FormattedRemainingTime));
        OnPropertyChanged(nameof(RemainingTime));
        MessageChanged?.Invoke(this, "Time set to 25 minutes (Pomodoro)");
    }

    /// <summary>
    /// Set timer to 45 minutes (preset)
    /// </summary>
    [RelayCommand]
    private void Set45Minutes()
    {
        TotalTime = TimeSpan.FromMinutes(45);
        MinutesInput = 45;
        ElapsedTime = TimeSpan.Zero;
        CurrentState = TimerState.Stopped;
        OnPropertyChanged(nameof(DisplayTime));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(FormattedRemainingTime));
        OnPropertyChanged(nameof(RemainingTime));
        MessageChanged?.Invoke(this, "Time set to 45 minutes");
    }

    /// <summary>
    /// Set timer to 60 minutes (preset)
    /// </summary>
    [RelayCommand]
    private void Set60Minutes()
    {
        TotalTime = TimeSpan.FromMinutes(60);
        MinutesInput = 60;
        ElapsedTime = TimeSpan.Zero;
        CurrentState = TimerState.Stopped;
        OnPropertyChanged(nameof(DisplayTime));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(FormattedRemainingTime));
        OnPropertyChanged(nameof(RemainingTime));
        MessageChanged?.Invoke(this, "Time set to 60 minutes");
    }

    /// <summary>
    /// Called on each timer tick to update elapsed time
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            if (CurrentState != TimerState.Running)
                return;

            // Calculate elapsed time from session start
            ElapsedTime = DateTime.Now - _sessionStartTime;

            // Update display properties
            OnPropertyChanged(nameof(DisplayTime));
            OnPropertyChanged(nameof(FormattedElapsedTime));
            OnPropertyChanged(nameof(FormattedRemainingTime));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(RemainingTime));

            // Check if time is up
            if (ElapsedTime >= TotalTime)
            {
                _timer?.Stop();
                ElapsedTime = TotalTime;
                CurrentState = TimerState.Stopped;

                OnPropertyChanged(nameof(DisplayTime));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsNotRunning));

                // Trigger completion
                OnTimerComplete();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Timer] Tick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when timer completes
    /// </summary>
    private async void OnTimerComplete()
    {
        try
        {
            MessageChanged?.Invoke(this, "Session complete! 🎉");

            // Play completion sound (haptic feedback)
            await PlayCompletionSoundAsync();

            // Save session
            await SaveSessionAsync();

            // Raise event
            TimerCompleted?.Invoke(this, EventArgs.Empty);

            Debug.WriteLine("[Timer] Session completed and saved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Timer] Completion error: {ex.Message}");
        }
    }

    /// <summary>
    /// Save completed session to database
    /// </summary>
    private async Task SaveSessionAsync()
    {
        try
        {
            // Note: Session persistence moved to Week 3 (Database & Sync Prep)
            // For now, log that session would be saved
            Debug.WriteLine($"[Timer] Session saved: {ElapsedTime.TotalMinutes:F1}m - Notes: {SessionNotes}");

            // TODO: Week 3 - Implement actual database save
            // var session = new StudySession
            // {
            //     StartTime = _sessionStartTime,
            //     EndTime = DateTime.Now,
            //     Duration = ElapsedTime,
            //     Notes = SessionNotes,
            //     CreatedAt = DateTime.UtcNow
            // };
            // await _sessionService.AddSessionAsync(session);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Timer] Save error: {ex.Message}");
            MessageChanged?.Invoke(this, $"Error saving session: {ex.Message}");
        }
    }

    /// <summary>
    /// Play completion sound and haptic feedback
    /// </summary>
    private async Task PlayCompletionSoundAsync()
    {
        try
        {
            // Haptic feedback - simple vibration pattern
            if (HapticFeedback.Default.IsSupported)
            {
                // Long press pattern
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                await Task.Delay(100);
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                await Task.Delay(100);
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            }

            // Note: Audio playback moved to Week 4 (Cloud Sync Integration)
            // For now, just the haptic feedback

            Debug.WriteLine("[Timer] Completion feedback triggered");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Timer] Feedback error: {ex.Message}");
        }
    }
}

/// <summary>
/// Timer state enumeration
/// </summary>
public enum TimerState
{
    /// <summary>Timer is stopped and no time is running</summary>
    Stopped,

    /// <summary>Timer is actively counting down</summary>
    Running,

    /// <summary>Timer is paused but can be resumed</summary>
    Paused
}
