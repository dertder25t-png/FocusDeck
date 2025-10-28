using System.Windows;
using System.Windows.Controls;
using FocusDock.Core.Services;
using FocusDock.Data;
using FocusDock.Data.Models;
using System.Diagnostics;

namespace FocusDock.App;

public partial class SettingsWindow : Window
{
    private readonly CalendarService _calendarService;
    private CalendarSettings _settings;

    public SettingsWindow(CalendarService calendarService)
    {
        InitializeComponent();
        _calendarService = calendarService;
        _settings = _calendarService.GetSettings();

        LoadSettings();

        // Wire up slider value changed
        SliderSyncInterval.ValueChanged += (s, e) =>
        {
            TxtSyncInterval.Text = $"{(int)SliderSyncInterval.Value} minutes";
        };
    }

    private void LoadSettings()
    {
        TxtGoogleClientId.Text = _settings.GoogleClientId ?? "";
        TxtCanvasUrl.Text = _settings.CanvasInstanceUrl ?? "https://canvas.instructure.com";
        ChkEnableGoogle.IsChecked = _settings.EnableGoogleCalendar;
        ChkEnableCanvas.IsChecked = _settings.EnableCanvas;
        SliderSyncInterval.Value = _settings.SyncIntervalMinutes;
        TxtSyncInterval.Text = $"{_settings.SyncIntervalMinutes} minutes";
    }

    private void OnGoogleAuthClick(object sender, RoutedEventArgs e)
    {
        var clientId = TxtGoogleClientId.Text;
        var clientSecret = PwdGoogleClientSecret.Password;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            System.Windows.MessageBox.Show("Please enter both Client ID and Client Secret.", "Missing Credentials", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var provider = new GoogleCalendarProvider(clientId, clientSecret);
            var authUrl = provider.GetAuthorizationUrl();
            
            // Open browser to authorization URL
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            System.Windows.MessageBox.Show(
                "A browser window has opened for authorization.\n\n" +
                "After granting permission, you'll receive an authorization code.\n" +
                "Paste it below when prompted.",
                "Google Authorization",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information
            );

            // TODO: Handle authorization code input in a dialog
            // For now, user can manually paste the token
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Authorization failed: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async void OnCanvasTestClick(object sender, RoutedEventArgs e)
    {
        var url = TxtCanvasUrl.Text;
        var token = PwdCanvasToken.Password;

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
        {
            TxtCanvasStatus.Text = "⚠️ Please enter both URL and API token.";
            return;
        }

        try
        {
            BtnCanvasTest.IsEnabled = false;
            TxtCanvasStatus.Text = "🔄 Testing connection...";

            var provider = new CanvasApiProvider(url, token);
            var success = await provider.TestConnection();
            
            if (success)
            {
                TxtCanvasStatus.Text = "✅ Connection successful!";
            }
            else
            {
                TxtCanvasStatus.Text = "❌ Connection failed. Check URL and token.";
            }

            provider.Dispose();
        }
        catch (Exception ex)
        {
            TxtCanvasStatus.Text = $"❌ Error: {ex.Message}";
        }
        finally
        {
            BtnCanvasTest.IsEnabled = true;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var updatedSettings = new CalendarSettings
        {
            GoogleClientId = TxtGoogleClientId.Text,
            GoogleClientSecret = PwdGoogleClientSecret.Password,
            GoogleCalendarToken = _settings.GoogleCalendarToken, // Keep existing token
            GoogleRefreshToken = _settings.GoogleRefreshToken,
            CanvasInstanceUrl = TxtCanvasUrl.Text,
            CanvasToken = PwdCanvasToken.Password,
            EnableGoogleCalendar = ChkEnableGoogle.IsChecked ?? false,
            EnableCanvas = ChkEnableCanvas.IsChecked ?? false,
            SyncIntervalMinutes = (int)SliderSyncInterval.Value,
            EventsLookAheadDays = _settings.EventsLookAheadDays,
            GoogleCalendarIds = _settings.GoogleCalendarIds,
            ShowPersonalEvents = _settings.ShowPersonalEvents,
            ShowCourseEvents = _settings.ShowCourseEvents
        };

        _calendarService.UpdateSettings(updatedSettings);
        
        System.Windows.MessageBox.Show("Settings saved successfully!", "Success", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        
        this.Close();
    }
}
