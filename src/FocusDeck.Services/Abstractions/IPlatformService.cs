namespace FocusDeck.Services.Abstractions;

/// <summary>
/// Abstraction layer for platform-specific operations.
/// Implementations: Windows, iOS, Android, Web, macOS, Linux
/// </summary>
public interface IPlatformService
{
    // File system operations
    /// <summary>Gets the app data path (e.g., %APPDATA% on Windows)</summary>
    Task<string> GetAppDataPath();

    /// <summary>Gets the path for audio file storage</summary>
    Task<string> GetAudioStoragePath();

    /// <summary>Checks if a directory exists</summary>
    Task<bool> DirectoryExists(string path);

    /// <summary>Creates a directory (creates parent directories if needed)</summary>
    Task CreateDirectory(string path);

    // Notifications
    /// <summary>Requests permission to send notifications (if required by platform)</summary>
    Task RequestNotificationPermission();

    /// <summary>Sends a notification to the user</summary>
    Task SendNotification(string title, string message, int durationMs = 5000);

    // Screen/Window operations
    /// <summary>Gets the primary screen resolution</summary>
    Task<(int Width, int Height)> GetScreenSize();

    /// <summary>Checks if the app is the foreground window</summary>
    Task<bool> IsAppForeground();

    // External operations
    /// <summary>Launches a URL in the default browser</summary>
    Task LaunchUrl(string url);

    /// <summary>Gets text from the system clipboard</summary>
    Task<string> GetClipboardText();

    /// <summary>Sets text to the system clipboard</summary>
    Task SetClipboardText(string text);
}

/// <summary>Platform types supported by FocusDeck</summary>
public enum PlatformType
{
    Windows,
    MacOS,
    Linux,
    iOS,
    Android,
    Web
}
