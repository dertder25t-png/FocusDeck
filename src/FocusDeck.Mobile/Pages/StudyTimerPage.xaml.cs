using FocusDeck.Mobile.ViewModels;

namespace FocusDeck.Mobile.Pages;

/// <summary>
/// Study Timer Page - Main UI for the study timer feature
/// Displays a large timer, controls, and progress information
/// </summary>
public partial class StudyTimerPage : ContentPage
{
    public StudyTimerPage()
    {
        InitializeComponent();
        
        // Bind the ViewModel to this page
        BindingContext = new StudyTimerViewModel();
        
        // Subscribe to ViewModel events
        if (BindingContext is StudyTimerViewModel viewModel)
        {
            viewModel.TimerCompleted += OnTimerCompleted;
            viewModel.MessageChanged += OnMessageChanged;
        }
    }

    /// <summary>
    /// Called when the timer session completes
    /// </summary>
    private void OnTimerCompleted(object? sender, EventArgs e)
    {
        // Show completion toast/alert
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Study Session Complete", "Great job! Your study session is done.", "OK");
        });
    }

    /// <summary>
    /// Called when a message should be displayed to the user
    /// </summary>
    private void OnMessageChanged(object? sender, string message)
    {
        // Display the message
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Use a short toast-like display
            await DisplayAlert("Timer", message, "OK");
        });
    }
}
