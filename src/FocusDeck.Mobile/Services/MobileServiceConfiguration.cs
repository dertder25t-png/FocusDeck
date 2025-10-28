using FocusDeck.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Mobile;

/// <summary>
/// Configures all services for the FocusDeck mobile application.
/// Note: For Phase 6b, we register only mobile-specific services.
/// In Phase 6c, we'll add a cross-platform shared library for business logic
/// that both Desktop and Mobile apps can use without platform-specific dependencies.
/// </summary>
public static class MobileServiceConfiguration
{
    public static IServiceCollection AddMobileServices(this IServiceCollection services)
    {
        // TODO: Phase 6b - Register platform-specific mobile services
        // services.AddSingleton<IStudySessionService, MobileStudySessionService>();
        // services.AddSingleton<IAnalyticsService, MobileAnalyticsService>();
        // services.AddSingleton<ICloudSyncService, MobileCloudSyncService>();
        // services.AddSingleton<IEncryptionService, EncryptionService>();
        
        // Register platform-specific mobile services
        services.AddSingleton<IMobileAudioRecordingService, MobileAudioRecordingService>();
        services.AddSingleton<IMobileNotificationService, MobileNotificationService>();
        services.AddSingleton<IMobileStorageService, MobileStorageService>();
        
        return services;
    }
}
