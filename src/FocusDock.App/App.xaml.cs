namespace FocusDock.App;

using System;
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

        // Global exception handlers
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var message = $"Unhandled Exception:\n{ex}\n\nInner:\n{ex?.InnerException}";
            Console.WriteLine(message);
            MessageBox.Show(message, "FocusDock Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            var details = args.Exception.ToString();
            var inner = args.Exception.InnerException?.ToString() ?? "(none)";
            var message = $"UI Exception:\n{details}\n\nInner:\n{inner}";
            Console.WriteLine(message);
            MessageBox.Show(message, "FocusDock UI Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
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
        catch (Exception ex)
        {
            MessageBox.Show($"Startup Error:\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                "FocusDock Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}

