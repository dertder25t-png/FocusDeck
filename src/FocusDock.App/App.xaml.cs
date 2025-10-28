namespace FocusDock.App;

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using FocusDeck.Services;
using FocusDeck.Services.Abstractions;

public partial class App : System.Windows.Application
{
    /// <summary>Service provider for dependency injection</summary>
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure dependency injection
        var services = new ServiceCollection();

        // Register all cross-platform core services
        services.AddFocusDeckCoreServices();

        // Add platform-specific services (Windows)
        services.AddPlatformServices(PlatformType.Windows);

        // Build the service provider
        Services = services.BuildServiceProvider();

        System.Diagnostics.Debug.WriteLine("App started - Services initialized");
    }
}

