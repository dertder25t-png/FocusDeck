using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FocusDeck.Mobile.ViewModels;

/// <summary>
/// ViewModel for the Command Deck page that allows remote control of the desktop
/// </summary>
public class CommandDeckViewModel : BaseViewModel
{
    private string _connectionStatus = "Disconnected";
    private string _connectionIndicator = "🔴";
    private double _progressPercent = 0;
    private string _progressText = "0%";
    private string _focusState = "Idle";
    private string _selectedLayout = "NotesLeft";
    private string _focusButtonLabel = "Start";
    private string _focusButtonText = "Start focus session";
    private string _focusButtonColor = "#4CAF50";
    private bool _isFocusActive = false;

    public CommandDeckViewModel()
    {
        Title = "Command Deck";
        
        // Initialize available layouts
        AvailableLayouts = new ObservableCollection<string>
        {
            "NotesLeft",
            "AIRight",
            "Split50"
        };
        
        // Initialize recent notes
        RecentNotes = new ObservableCollection<NoteItem>
        {
            new NoteItem { Title = "Study Notes - Chapter 5" },
            new NoteItem { Title = "Meeting Notes 2024-11-03" },
            new NoteItem { Title = "Project Ideas" }
        };
        
        // Initialize activity log
        ActivityLog = new ObservableCollection<ActivityItem>
        {
            new ActivityItem { Timestamp = "10:30", Message = "Connected to desktop" },
            new ActivityItem { Timestamp = "10:25", Message = "Opened note: Study Notes" }
        };

        // Initialize commands
        OpenNoteCommand = CreateAsyncCommand(OpenNoteAsync);
        RearrangeLayoutCommand = CreateAsyncCommand(RearrangeLayoutAsync);
        ToggleFocusCommand = CreateAsyncCommand(ToggleFocusAsync);
        OpenRecentNoteCommand = CreateCommand<NoteItem>(OpenRecentNote);
        
        // Start telemetry listener (stub)
        _ = InitializeConnectionAsync();
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public string ConnectionIndicator
    {
        get => _connectionIndicator;
        set => SetProperty(ref _connectionIndicator, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        set
        {
            SetProperty(ref _progressPercent, value);
            ProgressText = $"{(int)(value * 100)}%";
        }
    }

    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }

    public string FocusState
    {
        get => _focusState;
        set => SetProperty(ref _focusState, value);
    }

    public string SelectedLayout
    {
        get => _selectedLayout;
        set => SetProperty(ref _selectedLayout, value);
    }

    public string FocusButtonLabel
    {
        get => _focusButtonLabel;
        set => SetProperty(ref _focusButtonLabel, value);
    }

    public string FocusButtonText
    {
        get => _focusButtonText;
        set => SetProperty(ref _focusButtonText, value);
    }

    public string FocusButtonColor
    {
        get => _focusButtonColor;
        set => SetProperty(ref _focusButtonColor, value);
    }

    public ObservableCollection<string> AvailableLayouts { get; }
    public ObservableCollection<NoteItem> RecentNotes { get; }
    public ObservableCollection<ActivityItem> ActivityLog { get; }

    public ICommand OpenNoteCommand { get; }
    public ICommand RearrangeLayoutCommand { get; }
    public ICommand ToggleFocusCommand { get; }
    public ICommand OpenRecentNoteCommand { get; }

    /// <summary>
    /// Initialize connection to server (stub)
    /// </summary>
    private async Task InitializeConnectionAsync()
    {
        await Task.Delay(1000); // Simulate connection
        
        ConnectionStatus = "Connected";
        ConnectionIndicator = "🟢";
        
        AddActivityLog("Connected to desktop");
    }

    /// <summary>
    /// Open note action
    /// </summary>
    private async Task OpenNoteAsync()
    {
        IsLoading = true;
        
        try
        {
            // TODO: Implement note search/selection dialog
            // For now, just send a sample action
            await SendRemoteActionAsync("OpenNote", new Dictionary<string, object>
            {
                { "noteId", "sample-note-id" }
            });
            
            AddActivityLog("Sent: Open Note");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open note: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Rearrange layout action
    /// </summary>
    private async Task RearrangeLayoutAsync()
    {
        IsLoading = true;
        
        try
        {
            await SendRemoteActionAsync("RearrangeLayout", new Dictionary<string, object>
            {
                { "preset", SelectedLayout }
            });
            
            AddActivityLog($"Sent: Rearrange to {SelectedLayout}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to rearrange layout: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggle focus session
    /// </summary>
    private async Task ToggleFocusAsync()
    {
        IsLoading = true;
        
        try
        {
            if (_isFocusActive)
            {
                await SendRemoteActionAsync("StopFocus", new Dictionary<string, object>());
                _isFocusActive = false;
                FocusButtonLabel = "Start";
                FocusButtonText = "Start focus session";
                FocusButtonColor = "#4CAF50";
                AddActivityLog("Sent: Stop Focus");
            }
            else
            {
                await SendRemoteActionAsync("StartFocus", new Dictionary<string, object>
                {
                    { "duration", 25 }
                });
                _isFocusActive = true;
                FocusButtonLabel = "Stop";
                FocusButtonText = "Stop focus session";
                FocusButtonColor = "#F44336";
                AddActivityLog("Sent: Start Focus");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to toggle focus: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Open a recent note
    /// </summary>
    private void OpenRecentNote(NoteItem note)
    {
        if (note != null)
        {
            _ = SendRemoteActionAsync("OpenNote", new Dictionary<string, object>
            {
                { "noteId", note.Title }
            });
            
            AddActivityLog($"Sent: Open {note.Title}");
        }
    }

    /// <summary>
    /// Send a remote action to the server (stub)
    /// </summary>
    private async Task SendRemoteActionAsync(string kind, Dictionary<string, object> payload)
    {
        // TODO: Implement actual API call to /v1/remote/actions
        await Task.Delay(500); // Simulate network call
    }

    /// <summary>
    /// Add an entry to the activity log
    /// </summary>
    private void AddActivityLog(string message)
    {
        ActivityLog.Insert(0, new ActivityItem
        {
            Timestamp = DateTime.Now.ToString("HH:mm"),
            Message = message
        });
        
        // Keep only last 20 entries
        while (ActivityLog.Count > 20)
        {
            ActivityLog.RemoveAt(ActivityLog.Count - 1);
        }
    }

    /// <summary>
    /// Create command with parameter
    /// </summary>
    protected Command<T> CreateCommand<T>(Action<T> action)
    {
        return new Command<T>(action);
    }
}

/// <summary>
/// Model for note items
/// </summary>
public class NoteItem
{
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Model for activity log items
/// </summary>
public class ActivityItem
{
    public string Timestamp { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
