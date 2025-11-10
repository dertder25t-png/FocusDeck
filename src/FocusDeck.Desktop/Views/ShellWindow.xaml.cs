using FocusDeck.Contracts.MultiTenancy;
using FocusDeck.Desktop.Services;
using FocusDeck.Desktop.Services;
using FocusDeck.Desktop.Services.Auth;
using System.Windows;
using System.Windows.Input;

namespace FocusDeck.Desktop.Views;

public partial class ShellWindow : Window
{
    private readonly IThemeService _themeService;
    private readonly ISnackbarService _snackbarService;
    private readonly ICommandPaletteService _commandPaletteService;
    private readonly IKeyProvisioningService _provisioning;
    private CurrentTenantDto? _currentTenant;

    public ShellWindow(
        IThemeService themeService,
        ISnackbarService snackbarService,
        ICommandPaletteService commandPaletteService,
        IKeyProvisioningService provisioning)
    {
        InitializeComponent();
        _themeService = themeService;
        _snackbarService = snackbarService;
        _commandPaletteService = commandPaletteService;
        _provisioning = provisioning;
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
    }
}
