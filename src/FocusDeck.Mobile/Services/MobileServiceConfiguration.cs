using FocusDeck.Mobile.Services;
using FocusDeck.Mobile.Data;
using FocusDeck.Mobile.Data.Repositories;
using FocusDeck.Mobile.Pages;
using FocusDeck.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Mobile;

/// <summary>
/// Configures all services for the FocusDeck mobile application.
/// Registers database context, repository, cloud sync, viewmodels, and platform-specific services.
/// </summary>
public static class MobileServiceConfiguration
{
    public static IServiceCollection AddMobileServices(this IServiceCollection services, string cloudServerUrl = "")
    {
        // Register database context
        services.AddDbContext<StudySessionDbContext>();
        
        // Register repository for local data access
        services.AddScoped<ISessionRepository, SessionRepository>();
        
        // Register cloud sync service (PocketBase by default)
        services.AddSingleton<ICloudSyncService>(sp => 
            new PocketBaseCloudSyncService(cloudServerUrl));
        
        // Register device identification service
        services.AddSingleton<IDeviceIdService, DeviceIdService>();
        
        // Register device pairing service
        services.AddSingleton<IDevicePairingService, DevicePairingService>();
        
        // Register WebSocket client service
        services.AddSingleton<IWebSocketClientService, WebSocketClientService>();
        
        // Register heartbeat service (disabled by default)
        services.AddSingleton<IHeartbeatService, HeartbeatService>();
        
        // Register ViewModels
        services.AddSingleton<StudyTimerViewModel>();
        services.AddSingleton<CloudSettingsViewModel>();
        
        // Register Pages
        services.AddSingleton<StudyTimerPage>();
        services.AddSingleton<SettingsPage>();
        
        // Register platform-specific mobile services
        services.AddSingleton<IMobileAudioRecordingService, MobileAudioRecordingService>();
        services.AddSingleton<IMobileNotificationService, MobileNotificationService>();
        services.AddSingleton<IMobileStorageService, MobileStorageService>();
        
        return services;
    }
}
