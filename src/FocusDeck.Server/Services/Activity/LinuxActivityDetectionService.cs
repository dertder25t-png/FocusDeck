using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Services.Activity;
using Microsoft.Extensions.Logging;

#if NET8_0 || NET9_0

namespace FocusDeck.Server.Services.Activity
{
    /// <summary>
    /// Linux-specific activity detection using wmctrl and xdotool.
    /// Tracks focused window on X11/Wayland desktops.
    /// </summary>
    public class LinuxActivityDetectionService : ActivityDetectionService
    {
        private DateTime _lastCheck = DateTime.UtcNow;
        private Queue<DateTime> _activityHistory = new();
        private const int ACTIVITY_HISTORY_CAPACITY = 100;

        public LinuxActivityDetectionService(ILogger<LinuxActivityDetectionService> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get currently focused window using wmctrl and xdotool.
        /// </summary>
        protected override async Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct)
        {
            try
            {
                // Check if Wayland is in use, as xdotool/wmctrl might fail or return incorrect data
                var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
                if (string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Wayland detected. xdotool/wmctrl may not work for window tracking.");
                    // In a real implementation, we would use GNOME Shell extensions or similar here.
                    // For now, fail gracefully or try anyway but expect failure.
                }

                // Use xdotool to get active window ID
                var windowIdOutput = await ExecuteCommandAsync("xdotool", "getactivewindow", ct);
                if (string.IsNullOrWhiteSpace(windowIdOutput))
                    return null;

                var windowId = windowIdOutput.Trim();

                // Get window title
                var titleOutput = await ExecuteCommandAsync("xdotool", $"getwindowname {windowId}", ct);
                if (string.IsNullOrWhiteSpace(titleOutput))
                    return null;

                var windowTitle = titleOutput.Trim();

                // Get process name from window ID
                var appName = await ExtractAppNameFromXdotoolAsync(windowId);

                // Get full process path if possible
                var processPath = await GetProcessPathFromWindowIdAsync(windowId, ct);

                var focusedApp = new FocusedApplication
                {
                    WindowTitle = windowTitle,
                    AppName = appName,
                    ProcessPath = processPath,
                    Tags = ClassifyApplication(appName),
                    SwitchedAt = DateTime.UtcNow
                };

                return focusedApp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get focused application on Linux");
                return null;
            }
        }

        /// <summary>
        /// Detect activity intensity from keyboard/mouse input.
        /// Monitors /proc/interrupts for input device activity.
        /// </summary>
        protected override async Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct)
        {
            try
            {
                int intensity = 0;

                // Check for recent activity in history
                var cutoffTime = DateTime.UtcNow.AddMinutes(-minutesWindow);
                while (_activityHistory.Count > 0 && _activityHistory.Peek() < cutoffTime)
                {
                    _activityHistory.Dequeue();
                }

                // Activity history contributes to intensity
                intensity = Math.Min(_activityHistory.Count * 5, 50);

                // Try to sample recent input events
                try
                {
                    var eventsOutput = await ExecuteCommandAsync("xinput", "list --id-only", ct);
                    if (!string.IsNullOrWhiteSpace(eventsOutput))
                    {
                        var deviceCount = eventsOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length;
                        intensity += Math.Min(deviceCount * 5, 30);
                    }
                }
                catch
                {
                    // xinput not available, continue with other methods
                }

                // Check for mouse movement
                try
                {
                    var mouseOutput = await ExecuteCommandAsync("xdotool", "getmouselocation", ct);
                    if (!string.IsNullOrWhiteSpace(mouseOutput))
                    {
                        intensity += 15;
                    }
                }
                catch
                {
                    // xdotool query failed, continue
                }

                return Math.Min(intensity, 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get activity intensity on Linux");
                return 0;
            }
        }

        /// <summary>
        /// Extract application name from xdotool window ID.
        /// Uses wmctrl to get application details.
        /// </summary>
        private async Task<string> ExtractAppNameFromXdotoolAsync(string windowId)
        {
            try
            {
                // Use wmctrl to list all windows and find this one
                var wmctrlOutput = await ExecuteCommandAsync("wmctrl", "-l", CancellationToken.None);
                var lines = wmctrlOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    // wmctrl output format: id desk x y w h host client
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && parts[0].Trim() == windowId)
                    {
                        // Last part is usually the app name
                        return parts.Length > 7 ? parts[7] : "unknown";
                    }
                }

                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Get process path from window ID by reading /proc filesystem.
        /// </summary>
        private async Task<string> GetProcessPathFromWindowIdAsync(string windowId, CancellationToken ct)
        {
            try
            {
                // Try to extract PID from window ID
                var pidOutput = await ExecuteCommandAsync("xdotool", $"getwindowpid {windowId}", ct);
                if (int.TryParse(pidOutput.Trim(), out var pid))
                {
                    var exePath = $"/proc/{pid}/exe";
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                }

                return windowId;
            }
            catch
            {
                return windowId;
            }
        }

        /// <summary>
        /// Classify application by name for tagging.
        /// </summary>
        private string[] ClassifyApplication(string appName)
        {
            var lower = appName.ToLowerInvariant();

            if (lower.Contains("libreoffice") || lower.Contains("gedit") || lower.Contains("vim"))
                return new[] { "productivity", "office" };

            if (lower.Contains("firefox") || lower.Contains("chrome") || lower.Contains("brave"))
                return new[] { "browser" };

            if (lower.Contains("discord") || lower.Contains("slack") || lower.Contains("telegram"))
                return new[] { "communication", "distraction" };

            if (lower.Contains("spotify") || lower.Contains("vlc") || lower.Contains("audacious"))
                return new[] { "focus_music", "media" };

            if (lower.Contains("code") || lower.Contains("nvim") || lower.Contains("emacs"))
                return new[] { "coding", "productivity" };

            if (lower.Contains("nautilus") || lower.Contains("dolphin") || lower.Contains("caja"))
                return new[] { "system", "file_manager" };

            if (lower.Contains("terminal") || lower.Contains("konsole") || lower.Contains("xterm"))
                return new[] { "terminal", "system" };

            return new[] { "other" };
        }

        /// <summary>
        /// Record activity in history for intensity calculation.
        /// </summary>
        public void RecordLinuxActivity()
        {
            _activityHistory.Enqueue(DateTime.UtcNow);
            if (_activityHistory.Count > ACTIVITY_HISTORY_CAPACITY)
            {
                _activityHistory.Dequeue();
            }
            RecordActivity();
        }

        /// <summary>
        /// Execute a shell command and return output.
        /// </summary>
        private async Task<string> ExecuteCommandAsync(string command, string args, CancellationToken ct)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                        return string.Empty;

                    using (ct.Register(() => process.Kill()))
                    {
                        var output = await process.StandardOutput.ReadToEndAsync(ct);
                        await process.WaitForExitAsync(ct);
                        return output;
                    }
                }
            }
            catch (Exception ex)
            {
                // Only log at Debug level for missing tools (xdotool, xinput not available on headless servers)
                if (ex is System.ComponentModel.Win32Exception && (command == "xdotool" || command == "xinput"))
                {
                    _logger.LogDebug(ex, "Activity detection tool not available: {Command}", command);
                }
                else
                {
                    _logger.LogWarning(ex, "Failed to execute command: {Command} {Args}", command, args);
                }
                return string.Empty;
            }
        }
    }
}

#endif
