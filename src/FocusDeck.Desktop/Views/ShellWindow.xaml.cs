using FocusDeck.Desktop.Services;
using System.Windows;
using System.Windows.Input;

namespace FocusDeck.Desktop.Views;

public partial class ShellWindow : Window
{
    private readonly IThemeService _themeService;
    private readonly ISnackbarService _snackbarService;
    private readonly ICommandPaletteService _commandPaletteService;

    public ShellWindow(
        IThemeService themeService,
        ISnackbarService snackbarService,
        ICommandPaletteService commandPaletteService)
    {
        InitializeComponent();
        _themeService = themeService;
        _snackbarService = snackbarService;
        _commandPaletteService = commandPaletteService;

        // Subscribe to snackbar service
        _snackbarService.MessageReceived += OnSnackbarMessage;

        // Subscribe to command palette service
        _commandPaletteService.ShowRequested += OnShowCommandPalette;
        _commandPaletteService.HideRequested += OnHideCommandPalette;
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
}
