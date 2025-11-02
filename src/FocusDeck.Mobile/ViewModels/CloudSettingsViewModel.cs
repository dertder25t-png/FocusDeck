using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using FocusDeck.Mobile.Services;
using FocusDeck.Mobile.Data.Repositories;

namespace FocusDeck.Mobile.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// Manages cloud synchronization configuration and data statistics.
/// </summary>
public partial class CloudSettingsViewModel : ObservableObject
{
    private readonly ICloudSyncService _cloudSyncService;
    private readonly ISessionRepository _sessionRepository;

    /// <summary>
    /// Cloud server URL
    /// </summary>
    [ObservableProperty]
    private string cloudServerUrl = string.Empty;

    /// <summary>
    /// Cloud user email
    /// </summary>
    [ObservableProperty]
    private string cloudEmail = string.Empty;

    /// <summary>
    /// Cloud user password
    /// </summary>
    [ObservableProperty]
    private string cloudPassword = string.Empty;

    /// <summary>
    /// Is currently testing connection?
    /// </summary>
    [ObservableProperty]
    private bool isTestingConnection = false;

    /// <summary>
    /// Is currently signing in?
    /// </summary>
    [ObservableProperty]
    private bool isSigningIn = false;

    /// <summary>
    /// Sign in button text
    /// </summary>
    [ObservableProperty]
    private string signInButtonText = "Sign In";

    /// <summary>
    /// Should show connection test result?
    /// </summary>
    [ObservableProperty]
    private bool showConnectionTestResult = false;

    /// <summary>
    /// Connection test result title
    /// </summary>
    [ObservableProperty]
    private string connectionTestTitle = string.Empty;

    /// <summary>
    /// Connection test result message
    /// </summary>
    [ObservableProperty]
    private string connectionTestMessage = string.Empty;

    /// <summary>
    /// Connection test background color
    /// </summary>
    [ObservableProperty]
    private Color connectionTestBackgroundColor = Colors.White;

    /// <summary>
    /// Connection test border color
    /// </summary>
    [ObservableProperty]
    private Color connectionTestBorderColor = Colors.Gray;

    /// <summary>
    /// Connection test title color
    /// </summary>
    [ObservableProperty]
    private Color connectionTestTitleColor = Colors.Gray;

    /// <summary>
    /// Connection test message color
    /// </summary>
    [ObservableProperty]
    private Color connectionTestMessageColor = Colors.Gray;

    /// <summary>
    /// Cloud connection status text
    /// </summary>
    [ObservableProperty]
    private string cloudConnectionStatus = "Not Configured";

    /// <summary>
    /// Cloud status message
    /// </summary>
    [ObservableProperty]
    private string cloudStatusMessage = "Configure your PocketBase server";

    /// <summary>
    /// Cloud status icon (emoji)
    /// </summary>
    [ObservableProperty]
    private string cloudStatusIcon = "⚙️";

    /// <summary>
    /// Test button text
    /// </summary>
    [ObservableProperty]
    private string testButtonText = "Test Connection";

    /// <summary>
    /// Total sessions count
    /// </summary>
    [ObservableProperty]
    private int totalSessionsCount = 0;

    /// <summary>
    /// Total study time (formatted)
    /// </summary>
    [ObservableProperty]
    private string totalStudyTime = "0h 0m";

    /// <summary>
    /// Synced sessions count
    /// </summary>
    [ObservableProperty]
    private int syncedSessionsCount = 0;

    /// <summary>
    /// Last sync time (formatted)
    /// </summary>
    [ObservableProperty]
    private string lastSyncTime = "Never";

    /// <summary>
    /// Initializes a new instance of CloudSettingsViewModel.
    /// </summary>
    public CloudSettingsViewModel(ICloudSyncService cloudSyncService, ISessionRepository sessionRepository)
    {
        _cloudSyncService = cloudSyncService ?? new NoOpCloudSyncService();
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    /// <summary>
    /// Load settings from preferences
    /// </summary>
    [RelayCommand]
    public void LoadSettings()
    {
        try
        {
            // Load from preferences
            CloudServerUrl = Preferences.Get("cloud_server_url", "");
            CloudEmail = Preferences.Get("cloud_email", "");
            CloudPassword = Preferences.Get("cloud_password", "");

            // Update status
            UpdateCloudStatus();

            // Load statistics (fire and forget using async void)
            _ = LoadStatisticsAsync();

            Debug.WriteLine("[Settings] Settings loaded from preferences");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Test connection to cloud server
    /// </summary>
    [RelayCommand]
    public async Task TestConnection()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CloudServerUrl))
            {
                ShowTestResult("Invalid", "Please enter a server URL", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
                return;
            }

            IsTestingConnection = true;
            TestButtonText = "Testing...";

            // Create a test service instance with the provided URL
            var testService = new PocketBaseCloudSyncService(CloudServerUrl);

            // Test basic connectivity
            var isHealthy = await testService.CheckServerHealthAsync();

            if (isHealthy)
            {
                ShowTestResult("✓ Success", "Connected to cloud server!", Colors.LightGreen, Colors.Green, Colors.Green, Colors.Green);
                
                // If credentials provided, try authentication
                if (!string.IsNullOrWhiteSpace(CloudEmail) && !string.IsNullOrWhiteSpace(CloudPassword))
                {
                    var authToken = await testService.AuthenticateAsync(CloudEmail, CloudPassword);
                    if (!string.IsNullOrWhiteSpace(authToken))
                    {
                        ShowTestResult("✓ Authenticated", "Connected and authenticated!", Colors.LightGreen, Colors.Green, Colors.Green, Colors.Green);
                    }
                }
            }
            else
            {
                ShowTestResult("✗ Failed", "Cannot reach server. Check URL and try again.", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
            }

            Debug.WriteLine($"[Settings] Connection test complete");
        }
        catch (Exception ex)
        {
            ShowTestResult("✗ Error", $"Test failed: {ex.Message}", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
            Debug.WriteLine($"[Settings] Connection test error: {ex.Message}");
        }
        finally
        {
            IsTestingConnection = false;
            TestButtonText = "Test Connection";
        }
    }

    /// <summary>
    /// Save settings to preferences
    /// </summary>
    [RelayCommand]
    public void SaveSettings()
    {
        try
        {
            // Validate URL format
            if (!string.IsNullOrWhiteSpace(CloudServerUrl))
            {
                if (!CloudServerUrl.StartsWith("http://") && !CloudServerUrl.StartsWith("https://"))
                {
                    ShowTestResult("Invalid URL", "URL must start with http:// or https://", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
                    return;
                }
            }

            // Save to preferences
            Preferences.Set("cloud_server_url", CloudServerUrl ?? string.Empty);
            Preferences.Set("cloud_email", CloudEmail ?? string.Empty);
            Preferences.Set("cloud_password", CloudPassword ?? string.Empty);

            // Update status
            UpdateCloudStatus();

            // Show success
            ShowTestResult("✓ Saved", "Settings saved successfully", Colors.LightGreen, Colors.Green, Colors.Green, Colors.Green);

            Debug.WriteLine("[Settings] Settings saved to preferences");

            // Hide result after 2 seconds
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(2000);
                ShowConnectionTestResult = false;
            });
        }
        catch (Exception ex)
        {
            ShowTestResult("✗ Error", $"Failed to save: {ex.Message}", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
            Debug.WriteLine($"[Settings] Error saving settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Sign in to the server
    /// </summary>
    [RelayCommand]
    public async Task SignInAsync()
    {
        if (IsSigningIn)
        {
            return;
        }

        try
        {
            IsSigningIn = true;
            SignInButtonText = "Signing In...";

            // Validate inputs
            if (string.IsNullOrWhiteSpace(CloudServerUrl))
            {
                ShowTestResult("Invalid Input", "Please enter server URL", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
                return;
            }

            if (string.IsNullOrWhiteSpace(CloudEmail) || string.IsNullOrWhiteSpace(CloudPassword))
            {
                ShowTestResult("Invalid Input", "Please enter username and password", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
                return;
            }

            Debug.WriteLine($"[Settings] Attempting sign in to {CloudServerUrl}");

            // Create a test service to authenticate
            var testService = new PocketBaseCloudSyncService(CloudServerUrl);
            var authToken = await testService.AuthenticateAsync(CloudEmail, CloudPassword);

            if (!string.IsNullOrWhiteSpace(authToken))
            {
                // Save credentials on successful authentication
                Preferences.Set("cloud_server_url", CloudServerUrl);
                Preferences.Set("cloud_email", CloudEmail);
                Preferences.Set("cloud_password", CloudPassword);
                Preferences.Set("auth_token", authToken);

                // Update status
                UpdateCloudStatus();

                ShowTestResult("✓ Signed In", "Successfully authenticated! Your sessions will now sync automatically.", Colors.LightGreen, Colors.Green, Colors.Green, Colors.Green);
                Debug.WriteLine("[Settings] Sign in successful");

                // Hide result after 3 seconds
                await Task.Delay(3000);
                ShowConnectionTestResult = false;
            }
            else
            {
                ShowTestResult("✗ Failed", "Authentication failed. Check your credentials.", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
            }
        }
        catch (Exception ex)
        {
            ShowTestResult("✗ Error", $"Sign in failed: {ex.Message}", Colors.LightPink, Colors.Red, Colors.Red, Colors.Red);
            Debug.WriteLine($"[Settings] Sign in error: {ex.Message}");
        }
        finally
        {
            IsSigningIn = false;
            SignInButtonText = "Sign In";
        }
    }

    /// <summary>
    /// Load statistics from local database
    /// </summary>
    private async Task LoadStatisticsAsync()
    {
        try
        {
            // Get all sessions
            var allSessions = await _sessionRepository.GetAllSessionsAsync();

            // Calculate statistics
            TotalSessionsCount = allSessions.Count;
            TotalStudyTime = FormatTotalTime(allSessions.Sum(s => s.DurationMinutes));

            // Synced sessions (for now, show sessions that were attempted to be synced)
            // In a full implementation, this would track cloud sync status per session
            SyncedSessionsCount = 0; // Will be updated based on cloud sync implementation

            // Last sync time
            LastSyncTime = Preferences.Get("last_sync_time", "Never");

            Debug.WriteLine($"[Settings] Loaded statistics: {TotalSessionsCount} sessions, {TotalStudyTime} total");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Error loading statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Update cloud connection status display
    /// </summary>
    private void UpdateCloudStatus()
    {
        if (string.IsNullOrWhiteSpace(CloudServerUrl))
        {
            CloudConnectionStatus = "Not Configured";
            CloudStatusMessage = "Configure your PocketBase server to enable cloud sync";
            CloudStatusIcon = "⚙️";
        }
        else
        {
            CloudConnectionStatus = "Configured";
            CloudStatusMessage = $"Server: {CloudServerUrl}";
            CloudStatusIcon = "✓";
        }
    }

    /// <summary>
    /// Show connection test result with styling
    /// </summary>
    private void ShowTestResult(string title, string message, Color bgColor, Color borderColor, Color titleColor, Color messageColor)
    {
        ConnectionTestTitle = title;
        ConnectionTestMessage = message;
        ConnectionTestBackgroundColor = bgColor;
        ConnectionTestBorderColor = borderColor;
        ConnectionTestTitleColor = titleColor;
        ConnectionTestMessageColor = messageColor;
        ShowConnectionTestResult = true;
    }

    /// <summary>
    /// Format total minutes to "Xh Ym" format
    /// </summary>
    private string FormatTotalTime(int totalMinutes)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        return $"{hours}h {minutes}m";
    }
}
