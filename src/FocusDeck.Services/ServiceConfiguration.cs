namespace FocusDeck.Services;

using Microsoft.Extensions.DependencyInjection;
using FocusDeck.Services.Abstractions;
using FocusDeck.Services.Implementations.Windows;
using FocusDeck.Services.Implementations.Core;

/// <summary>
/// Extension methods for configuring FocusDeck services.
/// Usage: services.AddFocusDeckCoreServices().AddPlatformServices(PlatformType.Windows);
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Adds all cross-platform FocusDeck core services.
    /// These services work on any platform (Windows, iOS, Android, Web, etc.)
    /// </summary>
    public static IServiceCollection AddFocusDeckCoreServices(
        this IServiceCollection services)
    {
        // Register core services that are platform-agnostic
        services.AddSingleton<IStudySessionService, StudySessionService>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        
        return services;
    }

    /// <summary>
    /// Adds platform-specific service implementations.
    /// Must be called after AddFocusDeckCoreServices.
    /// </summary>
    public static IServiceCollection AddPlatformServices(
        this IServiceCollection services,
        PlatformType platformType)
    {
        return platformType switch
        {
            PlatformType.Windows => services
                .AddScoped<IPlatformService, WindowsPlatformService>()
                .AddScoped<IAudioRecordingService, WindowsAudioRecordingService>()
                .AddScoped<IAudioPlaybackService, WindowsAudioPlaybackService>(),

            PlatformType.iOS => services
                .AddScoped<IPlatformService, iOSPlatformService>()
                .AddScoped<IAudioRecordingService, iOSAudioRecordingService>()
                .AddScoped<IAudioPlaybackService, iOSAudioPlaybackService>(),

            PlatformType.Android => services
                .AddScoped<IPlatformService, AndroidPlatformService>()
                .AddScoped<IAudioRecordingService, AndroidAudioRecordingService>()
                .AddScoped<IAudioPlaybackService, AndroidAudioPlaybackService>(),

            PlatformType.Web => services
                .AddScoped<IPlatformService, WebPlatformService>()
                .AddScoped<IAudioRecordingService, WebAudioRecordingService>()
                .AddScoped<IAudioPlaybackService, WebAudioPlaybackService>(),

            _ => throw new NotSupportedException($"Platform {platformType} is not supported yet.")
        };
    }
}

// Real Windows implementations are in Implementations/Windows/*.cs files
// See: WindowsPlatformService.cs, WindowsAudioRecordingService.cs, WindowsAudioPlaybackService.cs
//
// iOS, Android, Web implementations will be created in Phase 6
// For now, they throw NotImplementedException

internal class iOSPlatformService : IPlatformService
{
    public Task<string> GetAppDataPath() => throw new NotImplementedException("iOS implementation - Phase 6");
    public Task<string> GetAudioStoragePath() => throw new NotImplementedException();
    public Task<bool> DirectoryExists(string path) => throw new NotImplementedException();
    public Task CreateDirectory(string path) => throw new NotImplementedException();
    public Task RequestNotificationPermission() => throw new NotImplementedException();
    public Task SendNotification(string title, string message, int durationMs = 5000) => throw new NotImplementedException();
    public Task<(int Width, int Height)> GetScreenSize() => throw new NotImplementedException();
    public Task<bool> IsAppForeground() => throw new NotImplementedException();
    public Task LaunchUrl(string url) => throw new NotImplementedException();
    public Task<string> GetClipboardText() => throw new NotImplementedException();
    public Task SetClipboardText(string text) => throw new NotImplementedException();
}

internal class iOSAudioRecordingService : IAudioRecordingService
{
    public event EventHandler<double>? RecordingProgressChanged;
    public event EventHandler<string>? RecordingError;

    public Task<string> StartRecording() => throw new NotImplementedException("iOS - Phase 6");
    public Task<AudioRecording> StopRecording() => throw new NotImplementedException();
    public Task<string> TranscribeAudio(string filePath) => throw new NotImplementedException();
    public Task<List<AudioRecording>> GetNotesForDate(DateTime date) => throw new NotImplementedException();
}

internal class iOSAudioPlaybackService : IAudioPlaybackService
{
    public event EventHandler? PlaybackCompleted;

    public Task PlayAudio(string filePath) => throw new NotImplementedException("iOS - Phase 6");
    public Task PauseAudio() => throw new NotImplementedException();
    public Task ResumeAudio() => throw new NotImplementedException();
    public Task StopAudio() => throw new NotImplementedException();
    public Task SetVolume(int percentage) => throw new NotImplementedException();
    public Task PlayAmbientSound(AmbientSoundType type) => throw new NotImplementedException();
    public Task<long> GetCurrentPosition() => throw new NotImplementedException();
    public Task<long> GetDuration() => throw new NotImplementedException();
}

internal class AndroidPlatformService : IPlatformService
{
    public Task<string> GetAppDataPath() => throw new NotImplementedException("Android - Phase 6");
    public Task<string> GetAudioStoragePath() => throw new NotImplementedException();
    public Task<bool> DirectoryExists(string path) => throw new NotImplementedException();
    public Task CreateDirectory(string path) => throw new NotImplementedException();
    public Task RequestNotificationPermission() => throw new NotImplementedException();
    public Task SendNotification(string title, string message, int durationMs = 5000) => throw new NotImplementedException();
    public Task<(int Width, int Height)> GetScreenSize() => throw new NotImplementedException();
    public Task<bool> IsAppForeground() => throw new NotImplementedException();
    public Task LaunchUrl(string url) => throw new NotImplementedException();
    public Task<string> GetClipboardText() => throw new NotImplementedException();
    public Task SetClipboardText(string text) => throw new NotImplementedException();
}

internal class AndroidAudioRecordingService : IAudioRecordingService
{
    public event EventHandler<double>? RecordingProgressChanged;
    public event EventHandler<string>? RecordingError;

    public Task<string> StartRecording() => throw new NotImplementedException("Android - Phase 6");
    public Task<AudioRecording> StopRecording() => throw new NotImplementedException();
    public Task<string> TranscribeAudio(string filePath) => throw new NotImplementedException();
    public Task<List<AudioRecording>> GetNotesForDate(DateTime date) => throw new NotImplementedException();
}

internal class AndroidAudioPlaybackService : IAudioPlaybackService
{
    public event EventHandler? PlaybackCompleted;

    public Task PlayAudio(string filePath) => throw new NotImplementedException("Android - Phase 6");
    public Task PauseAudio() => throw new NotImplementedException();
    public Task ResumeAudio() => throw new NotImplementedException();
    public Task StopAudio() => throw new NotImplementedException();
    public Task SetVolume(int percentage) => throw new NotImplementedException();
    public Task PlayAmbientSound(AmbientSoundType type) => throw new NotImplementedException();
    public Task<long> GetCurrentPosition() => throw new NotImplementedException();
    public Task<long> GetDuration() => throw new NotImplementedException();
}

internal class WebPlatformService : IPlatformService
{
    public Task<string> GetAppDataPath() => throw new NotImplementedException("Web - Phase 6");
    public Task<string> GetAudioStoragePath() => throw new NotImplementedException();
    public Task<bool> DirectoryExists(string path) => throw new NotImplementedException();
    public Task CreateDirectory(string path) => throw new NotImplementedException();
    public Task RequestNotificationPermission() => throw new NotImplementedException();
    public Task SendNotification(string title, string message, int durationMs = 5000) => throw new NotImplementedException();
    public Task<(int Width, int Height)> GetScreenSize() => throw new NotImplementedException();
    public Task<bool> IsAppForeground() => throw new NotImplementedException();
    public Task LaunchUrl(string url) => throw new NotImplementedException();
    public Task<string> GetClipboardText() => throw new NotImplementedException();
    public Task SetClipboardText(string text) => throw new NotImplementedException();
}

internal class WebAudioRecordingService : IAudioRecordingService
{
    public event EventHandler<double>? RecordingProgressChanged;
    public event EventHandler<string>? RecordingError;

    public Task<string> StartRecording() => throw new NotImplementedException("Web - Phase 6");
    public Task<AudioRecording> StopRecording() => throw new NotImplementedException();
    public Task<string> TranscribeAudio(string filePath) => throw new NotImplementedException();
    public Task<List<AudioRecording>> GetNotesForDate(DateTime date) => throw new NotImplementedException();
}

internal class WebAudioPlaybackService : IAudioPlaybackService
{
    public event EventHandler? PlaybackCompleted;

    public Task PlayAudio(string filePath) => throw new NotImplementedException("Web - Phase 6");
    public Task PauseAudio() => throw new NotImplementedException();
    public Task ResumeAudio() => throw new NotImplementedException();
    public Task StopAudio() => throw new NotImplementedException();
    public Task SetVolume(int percentage) => throw new NotImplementedException();
    public Task PlayAmbientSound(AmbientSoundType type) => throw new NotImplementedException();
    public Task<long> GetCurrentPosition() => throw new NotImplementedException();
    public Task<long> GetDuration() => throw new NotImplementedException();
}
