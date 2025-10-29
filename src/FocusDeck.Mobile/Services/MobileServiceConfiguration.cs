using FocusDeck.Mobile.Services;
using FocusDeck.Mobile.Data;
using FocusDeck.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Mobile;

/// <summary>
/// Configures all services for the FocusDeck mobile application.
/// Registers database context, repository, cloud sync, and platform-specific services.
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
        
        // Register platform-specific mobile services
        services.AddSingleton<IMobileAudioRecordingService, MobileAudioRecordingService>();
        services.AddSingleton<IMobileNotificationService, MobileNotificationService>();
        services.AddSingleton<IMobileStorageService, MobileStorageService>();
        
        return services;
    }
}
