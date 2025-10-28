namespace FocusDeck.Services.Implementations.Windows;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FocusDeck.Services.Abstractions;

/// <summary>
/// Windows implementation of platform service.
/// Handles file system, notifications, and system interactions on Windows.
/// </summary>
public class WindowsPlatformService : IPlatformService
{
    private readonly string _appDataPath;
    private readonly string _audioStoragePath;

    public WindowsPlatformService()
    {
        // Initialize paths
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocusDeck");

        _audioStoragePath = Path.Combine(_appDataPath, "audio");
    }

    /// <summary>Gets the Windows AppData path for FocusDeck</summary>
    public Task<string> GetAppDataPath()
    {
        return Task.FromResult(_appDataPath);
    }

    /// <summary>Gets the path for storing audio files</summary>
    public Task<string> GetAudioStoragePath()
    {
        return Task.FromResult(_audioStoragePath);
    }

    /// <summary>Checks if a directory exists on the file system</summary>
    public Task<bool> DirectoryExists(string path)
    {
        try
        {
            return Task.FromResult(Directory.Exists(path));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking directory: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>Creates a directory and any parent directories if needed</summary>
    public Task CreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating directory: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    /// <summary>Requests notification permission (not needed on Windows 10+)</summary>
    public Task RequestNotificationPermission()
    {
        // Windows 10+ always allows notifications for desktop apps
        return Task.CompletedTask;
    }

    /// <summary>Sends a notification to the user</summary>
    public async Task SendNotification(string title, string message, int durationMs = 5000)
    {
        try
        {
            // For Windows, use system tray notification
            // For desktop apps, we'll just log it for now
            // Real implementation would use Windows.UI.Notifications or WinRT
            System.Diagnostics.Debug.WriteLine($"NOTIFICATION: {title} - {message}");
            await Task.Delay(Math.Min(durationMs, 5000));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending notification: {ex.Message}");
        }
    }

    /// <summary>Gets the primary screen resolution</summary>
    public Task<(int Width, int Height)> GetScreenSize()
    {
        try
        {
            // Use P/Invoke to get screen size
            const int SM_CXSCREEN = 0;
            const int SM_CYSCREEN = 1;
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            return Task.FromResult((width, height));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting screen size: {ex.Message}");
            return Task.FromResult((1920, 1080)); // Default fallback
        }
    }

    /// <summary>Checks if the app is the foreground window</summary>
    public Task<bool> IsAppForeground()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var foregroundWindowHandle = GetForegroundWindow();
            return Task.FromResult(currentProcess.MainWindowHandle == foregroundWindowHandle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking foreground window: {ex.Message}");
            return Task.FromResult(true); // Assume foreground if we can't determine
        }
    }

    /// <summary>Launches a URL in the default browser</summary>
    public async Task LaunchUrl(string url)
    {
        try
        {
            await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error launching URL: {ex.Message}");
        }
    }

    /// <summary>Gets text from the Windows clipboard</summary>
    public Task<string> GetClipboardText()
    {
        return Task.Run(() =>
        {
            try
            {
                // Clipboard access requires COM - simplified version
                // In production, would use InteropServices or proper clipboard API
                System.Diagnostics.Debug.WriteLine("GetClipboardText called");
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting clipboard: {ex.Message}");
                return string.Empty;
            }
        });
    }

    /// <summary>Sets text to the Windows clipboard</summary>
    public Task SetClipboardText(string text)
    {
        return Task.Run(() =>
        {
            try
            {
                // Clipboard access requires COM - simplified version
                // In production, would use InteropServices or proper clipboard API
                System.Diagnostics.Debug.WriteLine($"SetClipboardText called: {text}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting clipboard: {ex.Message}");
            }
        });
    }

    // P/Invoke declarations for Windows API calls
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
