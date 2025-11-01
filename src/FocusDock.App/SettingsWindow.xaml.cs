using System.Windows;
using System.Windows.Controls;
using FocusDock.Core.Services;
using FocusDock.Data;
using FocusDock.Data.Models;
using System.Diagnostics;
using FocusDock.App.Services;

namespace FocusDock.App;

public partial class SettingsWindow : Window
{
    private readonly CalendarService _calendarService;
    private CalendarSettings _settings;
    private AppSettings _appSettings;

    public SettingsWindow(CalendarService calendarService)
    {
        InitializeComponent();
        _calendarService = calendarService;
        _settings = _calendarService.GetSettings();
        _appSettings = SettingsStore.LoadSettings();

        LoadSettings();

        // Wire up slider value changed
        SliderSyncInterval.ValueChanged += (s, e) =>
        {
            TxtSyncInterval.Text = $"{(int)SliderSyncInterval.Value} minutes";
        };
        
        // Wire up alignment radio buttons to update preview
        RbAlignLeft.Checked += OnAlignmentChanged;
        RbAlignCenter.Checked += OnAlignmentChanged;
        RbAlignRight.Checked += OnAlignmentChanged;
    }

    private void LoadSettings()
    {
        TxtServerUrl.Text = _appSettings.ServerUrl ?? "";
        if (!string.IsNullOrWhiteSpace(_appSettings.JwtToken))
        {
            PwdJwtToken.Password = _appSettings.JwtToken;
        }
        TxtGoogleClientId.Text = _settings.GoogleClientId ?? "";
        TxtCanvasUrl.Text = _settings.CanvasInstanceUrl ?? "https://canvas.instructure.com";
        ChkEnableGoogle.IsChecked = _settings.EnableGoogleCalendar;
        ChkEnableCanvas.IsChecked = _settings.EnableCanvas;
        SliderSyncInterval.Value = _settings.SyncIntervalMinutes;
        TxtSyncInterval.Text = $"{_settings.SyncIntervalMinutes} minutes";
        
        // Load dock position settings
        switch (_appSettings.Alignment)
        {
            case DockAlignment.Left:
                RbAlignLeft.IsChecked = true;
                break;
            case DockAlignment.Right:
                RbAlignRight.IsChecked = true;
                break;
            case DockAlignment.Center:
            default:
                RbAlignCenter.IsChecked = true;
                break;
        }
        
        UpdatePreview();
    }
    
    private void OnAlignmentChanged(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
    }
    
    private void UpdatePreview()
    {
        if (PreviewDock == null) return;
        
        if (RbAlignLeft.IsChecked == true)
        {
            PreviewDock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            PreviewDock.Margin = new Thickness(20, 0, 0, 0);
        }
        else if (RbAlignRight.IsChecked == true)
        {
            PreviewDock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            PreviewDock.Margin = new Thickness(0, 0, 20, 0);
        }
        else // Center
        {
            PreviewDock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            PreviewDock.Margin = new Thickness(0);
        }
    }
    
    private void OnApplyPositionClick(object sender, RoutedEventArgs e)
    {
        // Update app settings with selected alignment
        if (RbAlignLeft.IsChecked == true)
            _appSettings.Alignment = DockAlignment.Left;
        else if (RbAlignRight.IsChecked == true)
            _appSettings.Alignment = DockAlignment.Right;
        else
            _appSettings.Alignment = DockAlignment.Center;
        
        // Save settings
        SettingsStore.SaveSettings(_appSettings);
        
        // Apply to main window immediately
        if (Owner is MainWindow mainWindow)
        {
            mainWindow.ApplyPositionSettings(_appSettings);
        }
        
        System.Windows.MessageBox.Show("Dock position updated!", "Success", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
        _appSettings.ServerUrl = TxtServerUrl.Text;
        _appSettings.JwtToken = PwdJwtToken.Password;
        SettingsStore.SaveSettings(_appSettings);

        // Update ApiClient base URL immediately
        try
        {
            var apiClient = ((App)System.Windows.Application.Current).Services.GetService(typeof(ApiClient)) as ApiClient;
            apiClient?.SetServerUrl(_appSettings.ServerUrl);
            var syncClient = ((App)System.Windows.Application.Current).Services.GetService(typeof(SyncClientService)) as SyncClientService;
            if (syncClient != null)
            {
                // Re-init to apply token
                _ = syncClient.InitializeAsync();
            }
        }
        catch { /* ignore */ }

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
        
        // Kick off background sync initialization (non-blocking)
        try
        {
            var syncClient = ((App)System.Windows.Application.Current).Services.GetService(typeof(SyncClientService)) as SyncClientService;
            if (syncClient != null)
            {
                _ = syncClient.InitializeAsync();
            }
        }
        catch { /* ignore */ }

        System.Windows.MessageBox.Show("Settings saved successfully!", "Success", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        
        this.Close();
    }

    private async void OnRefreshDevicesClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var syncClient = ((App)System.Windows.Application.Current).Services.GetService(typeof(SyncClientService)) as SyncClientService;
            if (syncClient == null) return;
            var devices = await syncClient.GetDevicesAsync() ?? new List<FocusDeck.Shared.Models.Sync.DeviceRegistration>();
            LstDevices.ItemsSource = devices;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load devices: {ex.Message}");
        }
    }

    private async void OnUnregisterDeviceClick(object sender, RoutedEventArgs e)
    {
        // For now, use API directly until a wrapper exists
        try
        {
            if (LstDevices.SelectedItem is FocusDeck.Shared.Models.Sync.DeviceRegistration dev)
            {
                var baseUrl = TxtServerUrl.Text?.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl)) return;
                using var http = new System.Net.Http.HttpClient();
                var resp = await http.DeleteAsync($"{baseUrl}/api/sync/devices/{System.Uri.EscapeDataString(dev.DeviceId)}");
                if (resp.IsSuccessStatusCode)
                {
                    OnRefreshDevicesClick(sender, e);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Unregister failed: HTTP {(int)resp.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to unregister: {ex.Message}");
        }
    }
}
