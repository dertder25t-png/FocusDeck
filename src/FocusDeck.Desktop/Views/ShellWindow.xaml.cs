using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Contracts.MultiTenancy;
using FocusDeck.Desktop.Services;
using FocusDeck.Desktop.Services.Auth;
using FocusDeck.Desktop.Services.Privacy;

namespace FocusDeck.Desktop.Views;

public partial class ShellWindow : Window
{
    private readonly IThemeService _themeService;
    private readonly ISnackbarService _snackbarService;
    private readonly ICommandPaletteService _commandPaletteService;
    private readonly IKeyProvisioningService _provisioning;
    private readonly IPrivacySettingsClient _privacySettingsClient;
    private readonly List<PrivacySettingDto> _privacySettings = new();
    private readonly HashSet<string> _pendingPrivacy = new();
    private CurrentTenantDto? _currentTenant;

    public ShellWindow(
        IThemeService themeService,
        ISnackbarService snackbarService,
        ICommandPaletteService commandPaletteService,
        IKeyProvisioningService provisioning,
        IPrivacySettingsClient privacySettingsClient)
    {
        InitializeComponent();
        _themeService = themeService;
        _snackbarService = snackbarService;
        _commandPaletteService = commandPaletteService;
        _provisioning = provisioning;
        _privacySettingsClient = privacySettingsClient;
        _provisioning.ForcedLogout += OnForcedLogout;

        // Subscribe to snackbar service
        _snackbarService.MessageReceived += OnSnackbarMessage;

        // Subscribe to command palette service
        _commandPaletteService.ShowRequested += OnShowCommandPalette;
        _commandPaletteService.HideRequested += OnHideCommandPalette;
        _provisioning.CurrentTenantChanged += OnCurrentTenantChanged;
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _themeService.ToggleTheme();
    }

    private void CommandHint_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _commandPaletteService.Show();
    }

    private void SnackbarClose_Click(object sender, RoutedEventArgs e)
    {
        SnackbarContainer.Visibility = Visibility.Collapsed;
    }

    private void OnSnackbarMessage(object? sender, SnackbarMessageEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            SnackbarText.Text = e.Message;
            SnackbarContainer.Visibility = Visibility.Visible;

            if (e.Duration > TimeSpan.Zero)
            {
                Task.Delay(e.Duration).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        SnackbarContainer.Visibility = Visibility.Collapsed;
                    });
                });
            }
        });
    }

    private void OnShowCommandPalette(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            CommandPaletteOverlay.Visibility = Visibility.Visible;
            CommandSearch.Focus();
        });
    }

    private void OnHideCommandPalette(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            CommandPaletteOverlay.Visibility = Visibility.Collapsed;
            CommandSearch.Text = string.Empty;
        });
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        // Handle Ctrl+K for command palette
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _commandPaletteService.Show();
            e.Handled = true;
        }

        // Handle Escape to close command palette
        if (e.Key == Key.Escape && CommandPaletteOverlay.Visibility == Visibility.Visible)
        {
            _commandPaletteService.Hide();
            e.Handled = true;
        }
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _provisioning.LogoutAsync();
            _snackbarService.Show("Logged out", TimeSpan.FromSeconds(3));
            var onboarding = new OnboardingWindow
            {
                Owner = this
            };
            onboarding.ShowDialog();
        }
        catch
        {
            _snackbarService.Show("Logout failed", TimeSpan.FromSeconds(3));
        }
    }

    private void OnForcedLogout(object? sender, ForceLogoutEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var message = string.IsNullOrWhiteSpace(e.Reason) ? "Your session has ended" : e.Reason;
            _snackbarService.Show(message, TimeSpan.FromSeconds(4));
            UpdateTenantBadge(null);

            var onboarding = new OnboardingWindow
            {
                Owner = this
            };
            onboarding.ShowDialog();
        });
    }

    private void OnCurrentTenantChanged(object? sender, CurrentTenantDto? tenant)
    {
        Dispatcher.Invoke(() =>
        {
            _currentTenant = tenant;
            UpdateTenantBadge(tenant);
        });
        if (tenant != null)
        {
            _ = RefreshPrivacySettingsAsync();
        }
    }

    private void UpdateTenantBadge(CurrentTenantDto? tenant)
    {
        if (tenant == null)
        {
            TenantBadgeText.Text = "No tenant selected";
            TenantBadgeText.Visibility = Visibility.Collapsed;
            return;
        }

        TenantBadgeText.Text = $"{tenant.Name} /{tenant.Slug}";
        TenantBadgeText.Visibility = Visibility.Visible;
    }

    private async void ShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _provisioning.RefreshCurrentTenantAsync();
        await RefreshPrivacySettingsAsync();
    }

    private async void RefreshPrivacySettings_Click(object sender, RoutedEventArgs e)
    {
        await RefreshPrivacySettingsAsync();
    }

    private async Task RefreshPrivacySettingsAsync()
    {
        SetPrivacyStatus("Loading privacy controls…");
        try
        {
            var settings = (await _privacySettingsClient.GetConsentAsync()).ToList();
            _privacySettings.Clear();
            _privacySettings.AddRange(settings);

            RefreshPrivacyList();
            SetPrivacyStatus(_privacySettings.Count == 0 ? "No privacy controls configured." : "Privacy settings updated.");
        }
        catch (Exception ex)
        {
            SetPrivacyStatus("Unable to load privacy controls.");
            _snackbarService.Show("Failed to refresh privacy settings", TimeSpan.FromSeconds(3));
        }
    }

    private void RefreshPrivacyList()
    {
        Dispatcher.Invoke(() =>
        {
            PrivacySettingsPanel.Children.Clear();

            if (_privacySettings.Count == 0)
            {
                PrivacySettingsPanel.Children.Add(new TextBlock
                {
                    Text = "No privacy controls are available yet.",
                    Foreground = Brushes.Gray,
                    FontSize = 12
                });
                return;
            }

            foreach (var setting in _privacySettings)
            {
                PrivacySettingsPanel.Children.Add(CreatePrivacySettingRow(setting));
            }
        });
    }

    private UIElement CreatePrivacySettingRow(PrivacySettingDto setting)
    {
        var buttonText = _pendingPrivacy.Contains(setting.ContextType)
            ? "Updating…"
            : setting.IsEnabled
                ? "Enabled"
                : "Enable";

        var toggleButton = new System.Windows.Controls.Button
        {
            Content = buttonText,
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 110,
            IsEnabled = !_pendingPrivacy.Contains(setting.ContextType),
            Tag = setting.ContextType,
            Background = setting.IsEnabled ? Brushes.DimGray : Brushes.DodgerBlue,
            Foreground = Brushes.White
        };
        toggleButton.Click += async (_, __) => await TogglePrivacySettingAsync(setting.ContextType);

        var container = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
        container.Children.Add(new TextBlock
        {
            Text = setting.DisplayName,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White
        });
        container.Children.Add(new TextBlock
        {
            Text = setting.Description,
            FontSize = 12,
            Foreground = Brushes.LightGray,
            TextWrapping = TextWrapping.Wrap
        });
        container.Children.Add(new TextBlock
        {
            Text = $"Tier: {setting.Tier} · Default: {(setting.DefaultEnabled ? "On" : "Off")}",
            FontSize = 11,
            Foreground = Brushes.Gray
        });

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(container, 0);
        Grid.SetColumn(toggleButton, 1);

        row.Children.Add(container);
        row.Children.Add(toggleButton);

        return new Border
        {
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 6),
            Child = row
        };
    }

    private async Task TogglePrivacySettingAsync(string contextType)
    {
        if (_pendingPrivacy.Contains(contextType))
        {
            return;
        }

        var existing = _privacySettings.FirstOrDefault(s => s.ContextType.Equals(contextType, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            return;
        }

        _pendingPrivacy.Add(contextType);
        RefreshPrivacyList();
        SetPrivacyStatus("Updating privacy preference…");

        try
        {
            var updated = await _privacySettingsClient.UpdateConsentAsync(contextType, !existing.IsEnabled);
            if (updated != null)
            {
                var index = _privacySettings.FindIndex(s => s.ContextType.Equals(contextType, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    _privacySettings[index] = updated;
                }

                _snackbarService.Show("Privacy preference saved", TimeSpan.FromSeconds(2));
            }
            else
            {
                _snackbarService.Show("Failed to save privacy preference", TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Show("Error updating privacy preference", TimeSpan.FromSeconds(3));
        }
        finally
        {
            _pendingPrivacy.Remove(contextType);
            RefreshPrivacyList();
            SetPrivacyStatus("Privacy controls updated.");
        }
    }

    private void SetPrivacyStatus(string message)
    {
        Dispatcher.Invoke(() => { PrivacyStatusText.Text = message; });
    }
}
