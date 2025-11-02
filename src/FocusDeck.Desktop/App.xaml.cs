using FocusDeck.Desktop.Services;
using FocusDeck.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace FocusDeck.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var shell = _serviceProvider.GetRequiredService<ShellWindow>();
        shell.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Check if we should use fake server for development
        var useFakeServer = Environment.GetEnvironmentVariable("USE_FAKE_SERVER") == "true";

        if (useFakeServer)
        {
            // Use fake API client with canned data for development without backend
            services.AddSingleton<IApiClient, FakeApiClient>();
        }
        else
        {
            // Register HttpClient with Polly retry policy
            services.AddHttpClient<IApiClient, ApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:5239"); // Configure from settings
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddStandardResilienceHandler(); // Adds retry, circuit breaker, timeout policies
        }

        // Register services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();

        // Register views
        services.AddSingleton<ShellWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
