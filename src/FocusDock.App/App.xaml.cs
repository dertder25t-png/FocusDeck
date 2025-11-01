// FocusDock Desktop Application - Last Updated: October 29, 2025
namespace FocusDock.App;

using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using FocusDeck.Services;
using FocusDeck.Services.Abstractions;
using FocusDock.App.Services;
using FocusDock.Data;
using FocusDock.Core.Services;

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

            // Register FocusDock-specific services
            services.AddSingleton<FocusDock.SystemInterop.WindowTracker>();
            services.AddSingleton<FocusDock.Core.Services.LayoutManager>();
            services.AddSingleton<FocusDock.Core.Services.PinService>();
            services.AddSingleton<FocusDock.Core.Services.ReminderService>();
            services.AddSingleton<FocusDock.Core.Services.WorkspaceManager>();
            services.AddSingleton<FocusDock.Core.Services.CalendarService>();
            services.AddSingleton<FocusDock.Core.Services.TodoService>();
            services.AddSingleton<FocusDock.Core.Services.NotesService>();
            services.AddSingleton<FocusDock.Core.Services.StudyPlanService>();
            services.AddSingleton<FocusDock.Core.Services.AutomationService>();
            
            // Register ApiClient for server communication
            services.AddSingleton<ApiClient>();

            // Register SyncClientService (desktop sync)
            services.AddSingleton<SyncClientService>();

            // Register Windows
            services.AddSingleton<MainWindow>();
            services.AddTransient<PlannerWindow>();

            // Build the service provider
            Services = services.BuildServiceProvider();

            // Configure ApiClient with saved URL
            var appSettings = SettingsStore.LoadSettings();
            var apiClient = Services.GetRequiredService<ApiClient>();
            if (!string.IsNullOrEmpty(appSettings.ServerUrl))
            {
                apiClient.SetServerUrl(appSettings.ServerUrl);
            }

            // Initialize background sync (best-effort, non-blocking)
            try
            {
                var syncClient = Services.GetRequiredService<SyncClientService>();
                _ = syncClient.InitializeAsync();
                // Link ApiClient to Sync for deck tracking
                apiClient.SetSyncService(syncClient);

                // Wire Notes -> Sync
                var notes = Services.GetRequiredService<NotesService>();
                notes.NoteAdded += async (s, note) =>
                {
                    try { syncClient.TrackNoteCreated(note); await syncClient.PushAsync(syncClient.ChangeTracker); } catch { }
                };
                notes.NoteUpdated += async (s, note) =>
                {
                    try { syncClient.TrackNoteUpdated(note); await syncClient.PushAsync(syncClient.ChangeTracker); } catch { }
                };
                notes.NoteDeleted += async (s, id) =>
                {
                    try { syncClient.TrackNoteDeleted(id); await syncClient.PushAsync(syncClient.ChangeTracker); } catch { }
                };

                // Wire Todos -> Sync
                var todos = Services.GetRequiredService<TodoService>();
                todos.TodoAdded += async (s, todo) =>
                {
                    try { syncClient.TrackTaskCreated(todo); await syncClient.PushAsync(syncClient.ChangeTracker); } catch { }
                };
                todos.TodoUpdated += async (s, todo) =>
                {
                    try { syncClient.TrackTaskUpdated(todo); await syncClient.PushAsync(syncClient.ChangeTracker); } catch { }
                };
                todos.TodoDeleted += async (s, id) =>
                {
                    try { syncClient.TrackTaskDeleted(id); await syncClient.PushAsync(syncClient.ChangeTracker); } catch { }
                };
            }
            catch { /* ignore sync init errors at startup */ }

            // Resolve and show MainWindow
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

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

