using FocusDeck.Mobile.ViewModels;
using FocusDeck.Mobile.Data.Repositories;

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
        
        // Resolve ViewModel with dependencies from DI container
        var sessionRepository = Application.Current!.Handler.MauiContext!.Services.GetService<ISessionRepository>();
        if (sessionRepository == null)
            throw new InvalidOperationException("ISessionRepository service not registered");

        // Bind the ViewModel to this page
        BindingContext = new StudyTimerViewModel(sessionRepository);
        
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
