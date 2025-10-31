using System.Diagnostics;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using FocusDeck.Shared.Models;
using FocusDeck.Mobile.Data.Repositories;
using FocusDeck.Mobile.Services;

namespace FocusDeck.Mobile.ViewModels;

/// <summary>
/// ViewModel for the Study Timer page.
/// Manages timer state, controls, and session persistence to local database and cloud sync.
/// </summary>
public partial class StudyTimerViewModel : ObservableObject
{
    private IDispatcherTimer? _timer;
    private DateTime _sessionStartTime;
    private readonly ISessionRepository _sessionRepository;
    private readonly ICloudSyncService _cloudSyncService;
    private StudySession? _currentSession;

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

    /// <summary>
    /// Cloud sync status (Idle, Syncing, Synced, Error)
    /// </summary>
    [ObservableProperty]
    private CloudSyncStatus cloudSyncStatus = CloudSyncStatus.Idle;

    /// <summary>
    /// Cloud sync error message (if any)
    /// </summary>
    [ObservableProperty]
    private string cloudSyncErrorMessage = string.Empty;

    /// <summary>
    /// Is cloud sync enabled?
    /// </summary>
    [ObservableProperty]
    private bool isCloudSyncEnabled = false;

    /// <summary>
    /// Cloud sync status display text
    /// </summary>
    public string CloudSyncStatusText => CloudSyncStatus switch
    {
        CloudSyncStatus.Idle => "Cloud sync ready",
        CloudSyncStatus.Syncing => "Syncing to cloud...",
        CloudSyncStatus.Synced => "✓ Synced to cloud",
        CloudSyncStatus.Error => $"✗ Sync error: {CloudSyncErrorMessage}",
        CloudSyncStatus.Disabled => "Cloud sync disabled",
        _ => "Cloud sync ready"
    };

    /// <summary>
    /// Cloud sync status indicator (emoji/icon)
    /// </summary>
    public string CloudSyncIndicator => CloudSyncStatus switch
    {
        CloudSyncStatus.Idle => "⏱️",
        CloudSyncStatus.Syncing => "⏳",
        CloudSyncStatus.Synced => "✅",
        CloudSyncStatus.Error => "❌",
        CloudSyncStatus.Disabled => "🚫",
        _ => "⏱️"
    };

    /// <summary>
    /// Initializes a new instance of StudyTimerViewModel with dependency injection.
    /// </summary>
    /// <param name="sessionRepository">Repository for database operations.</param>
    /// <param name="cloudSyncService">Service for cloud synchronization (optional).</param>
    public StudyTimerViewModel(ISessionRepository sessionRepository, ICloudSyncService? cloudSyncService = null)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _cloudSyncService = cloudSyncService ?? new NoOpCloudSyncService();
        _sessionStartTime = DateTime.Now;
        _currentSession = null;
        
        // Check if cloud sync is configured
        IsCloudSyncEnabled = !string.IsNullOrWhiteSpace(Preferences.Get("cloud_server_url", ""));
        
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
            // Reduced from 100ms to 500ms for better performance (updates twice per second instead of 10 times)
            _timer.Interval = TimeSpan.FromMilliseconds(500);
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
            
            // Create new session for database tracking
            _currentSession = new StudySession
            {
                SessionId = Guid.NewGuid(),
                StartTime = _sessionStartTime,
                Status = SessionStatus.Active,
                Category = "Mobile Study"
            };
            
            Debug.WriteLine("[Timer] Started new session");
        }
        else if (CurrentState == TimerState.Paused)
        {
            _sessionStartTime = DateTime.Now - ElapsedTime;
            if (_currentSession != null)
            {
                _currentSession.Status = SessionStatus.Active;
            }
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
            var previousDisplayTime = DisplayTime;
            ElapsedTime = DateTime.Now - _sessionStartTime;

            // Only notify UI if display value actually changed (optimization to reduce UI updates)
            if (DisplayTime != previousDisplayTime)
            {
                // Batch property notifications for efficiency
                OnPropertyChanged(nameof(DisplayTime));
                OnPropertyChanged(nameof(FormattedElapsedTime));
                OnPropertyChanged(nameof(FormattedRemainingTime));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(RemainingTime));
            }

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
    /// Save completed session to database and sync to cloud (if enabled)
    /// </summary>
    private async Task SaveSessionAsync()
    {
        try
        {
            if (_currentSession == null)
            {
                Debug.WriteLine("[Timer] No session to save");
                return;
            }

            // Update session with completion data
            _currentSession.EndTime = DateTime.UtcNow;
            _currentSession.DurationMinutes = (int)ElapsedTime.TotalMinutes;
            _currentSession.SessionNotes = SessionNotes;
            _currentSession.Status = SessionStatus.Completed;
            _currentSession.UpdatedAt = DateTime.UtcNow;

            // Save to local database first (always works)
            var savedSession = await _sessionRepository.CreateSessionAsync(_currentSession);
            Debug.WriteLine($"[Timer] Session saved to local database: {savedSession.SessionId}");
            Debug.WriteLine($"[Timer] Duration: {ElapsedTime.TotalMinutes:F1}m - Notes: {SessionNotes}");
            MessageChanged?.Invoke(this, "Session saved locally ✓");

            // Attempt cloud sync if enabled
            if (IsCloudSyncEnabled)
            {
                await SyncSessionToCloudAsync(savedSession);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Timer] Save error: {ex.Message}");
            MessageChanged?.Invoke(this, $"Error saving session: {ex.Message}");
        }
    }

    /// <summary>
    /// Sync a saved session to cloud (async, doesn't block local app)
    /// </summary>
    private async Task SyncSessionToCloudAsync(StudySession session)
    {
        try
        {
            CloudSyncStatus = CloudSyncStatus.Syncing;
            CloudSyncErrorMessage = string.Empty;
            MessageChanged?.Invoke(this, "Syncing to cloud...");

            // Get stored auth token (if available)
            var authToken = Preferences.Get("cloud_auth_token", "");
            if (string.IsNullOrWhiteSpace(authToken))
            {
                // No auth token, can't sync
                CloudSyncStatus = CloudSyncStatus.Idle;
                CloudSyncErrorMessage = "Not authenticated with cloud server";
                Debug.WriteLine("[Cloud] Not authenticated - skipping sync");
                return;
            }

            // Attempt to sync
            var syncSuccess = await _cloudSyncService.SyncSessionAsync(session, authToken);

            if (syncSuccess)
            {
                CloudSyncStatus = CloudSyncStatus.Synced;
                MessageChanged?.Invoke(this, "Session synced to cloud ☁️");
                Debug.WriteLine($"[Cloud] Session {session.SessionId} synced successfully");
            }
            else
            {
                CloudSyncStatus = CloudSyncStatus.Error;
                CloudSyncErrorMessage = "Cloud sync failed";
                MessageChanged?.Invoke(this, "Failed to sync to cloud (saved locally)");
                Debug.WriteLine("[Cloud] Sync failed but local save succeeded");
            }

            // Reset status after a delay
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(3000);
                if (CloudSyncStatus == CloudSyncStatus.Synced)
                {
                    CloudSyncStatus = CloudSyncStatus.Idle;
                }
            });
        }
        catch (Exception ex)
        {
            CloudSyncStatus = CloudSyncStatus.Error;
            CloudSyncErrorMessage = ex.Message;
            Debug.WriteLine($"[Cloud] Sync error: {ex.Message}");
            MessageChanged?.Invoke(this, $"Cloud sync error (saved locally): {ex.Message}");
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
