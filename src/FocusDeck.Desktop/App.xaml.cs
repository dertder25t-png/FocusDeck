using FocusDeck.Desktop.Services;
using FocusDeck.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace FocusDeck.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    public static new App Current => (App)Application.Current;
    public IServiceProvider Services => _serviceProvider!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // If using real server, load persisted tokens or prompt for sign-in
        var api = _serviceProvider.GetRequiredService<IApiClient>();
        if (!(Environment.GetEnvironmentVariable("USE_FAKE_SERVER") == "true"))
        {
            var store = new FocusDeck.Desktop.Services.Auth.TokenStore();
            var rec = store.Load();
            if (rec != null && !string.IsNullOrEmpty(rec.AccessToken))
            {
                api.AccessToken = rec.AccessToken;
            }
            if (string.IsNullOrEmpty(api.AccessToken))
            {
                var onboarding = new Views.OnboardingWindow();
                onboarding.ShowDialog();
            }
            // Start refresh timer
            var kp = _serviceProvider.GetRequiredService<FocusDeck.Desktop.Services.Auth.IKeyProvisioningService>();
            kp.StartAutoRefresh();
        }

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
                // Default dev server URL matches standardized FocusDeck.Server port
                // Use HTTP to avoid local dev cert issues unless HTTPS is explicitly configured
                client.BaseAddress = new Uri("http://localhost:5000");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddStandardResilienceHandler(); // Adds retry, circuit breaker, timeout policies
        }

        // Register services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();
        services.AddSingleton<IAudioRecorderService, AudioRecorderService>();
        services.AddSingleton<FocusDeck.Services.Implementations.Core.EncryptionService>();
        services.AddSingleton<FocusDeck.Desktop.Services.Auth.IKeyProvisioningService, FocusDeck.Desktop.Services.Auth.KeyProvisioningService>();
        services.AddSingleton<IRemoteControllerService, RemoteControllerService>();

        // Register views
        services.AddSingleton<ShellWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

