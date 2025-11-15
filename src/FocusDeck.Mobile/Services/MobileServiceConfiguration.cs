using FocusDeck.Mobile.Services;
using FocusDeck.Mobile.Data;
using FocusDeck.Mobile.Data.Repositories;
using FocusDeck.Mobile.Pages;
using FocusDeck.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using FocusDeck.Mobile.Services.Auth;
using Microsoft.Maui.Storage;
using FocusDeck.Mobile.Services.Privacy;

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
        services.AddScoped<NoteRepository>();

        services.AddSingleton<MobileTokenStore>();
        services.AddSingleton<MobileVaultService>();

        services.AddSingleton<ICloudSyncService>(sp =>
        {
            var configuredUrl = string.IsNullOrWhiteSpace(cloudServerUrl)
                ? Preferences.Get("cloud_server_url", string.Empty)
                : cloudServerUrl;

            if (IsFocusDeckServer(configuredUrl))
            {
                return ActivatorUtilities.CreateInstance<FocusDeckServerSyncService>(sp, configuredUrl);
            }

            return new PocketBaseCloudSyncService(configuredUrl);
        });

        services.AddHttpClient<IMobileAuthService, MobilePakeAuthService>();
        
        // Register device identification service
        services.AddSingleton<IDeviceIdService, DeviceIdService>();
        
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
        services.AddSingleton<IMobileCloudApiClient, MobileCloudApiClient>();
        services.AddSingleton<IMobilePrivacySettingsClient, MobilePrivacySettingsClient>();
        services.AddSingleton<IMobileActivitySignalClient, MobileActivitySignalClient>();
        services.AddSingleton<IMobilePrivacyGate, MobilePrivacyGate>();
        
        return services;
    }

    private static bool IsFocusDeckServer(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var normalized = url.ToLowerInvariant();
        return normalized.Contains("focusdeck") || normalized.Contains("/v1/");
    }
}
