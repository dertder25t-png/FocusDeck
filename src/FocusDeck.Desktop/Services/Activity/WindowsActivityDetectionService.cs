using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Services.Activity;
using Microsoft.Extensions.Logging;

#if NET8_0_WINDOWS || NET9_0_WINDOWS || WINDOWS

namespace FocusDeck.Desktop.Services.Activity
{
    /// <summary>
    /// Windows-specific activity detection using WinEventHook P/Invoke.
    /// Tracks focused window and keyboard/mouse activity.
    /// </summary>
    public class WindowsActivityDetectionService : ActivityDetectionService
    {
        private IntPtr _m_hook = IntPtr.Zero;
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private HookProc? _hookProc;
        private Queue<DateTime> _activityHistory = new();
        private DateTime _lastKeyboardMouseActivity = DateTime.UtcNow;

        // P/Invoke declarations for window management
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventHook, HookProc lpfnWinEventHook, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Window event constants
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint EVENT_SYSTEM_FOCUS = 4;

        // Cursor tracking
        private POINT _lastCursorPos;
        private const int CURSOR_MOVEMENT_THRESHOLD = 5;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public WindowsActivityDetectionService(ILogger<WindowsActivityDetectionService> logger)
            : base(logger)
        {
            // Initialize hook procedure delegate
            _hookProc = HookHandler;

            // Set up window focus hook
            _m_hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _hookProc, 0, 0, 0);

            if (_m_hook == IntPtr.Zero)
            {
                logger.LogWarning("Failed to set WinEventHook");
            }
            else
            {
                logger.LogInformation("WinEventHook initialized successfully");
            }

            GetCursorPos(out _lastCursorPos);
        }

        /// <summary>
        /// Hook handler for window focus changes.
        /// Called when foreground window changes.
        /// </summary>
        private IntPtr HookHandler(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                RecordActivity();
                _ = GetCurrentActivityAsync(CancellationToken.None).ConfigureAwait(false);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Get currently focused application (window name and process).
        /// </summary>
        protected override Task<FocusedApplication?> GetFocusedApplicationInternalAsync(CancellationToken ct)
        {
            try
            {
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                    return Task.FromResult<FocusedApplication?>(null);

                var sb = new StringBuilder(256);
                GetWindowText(foregroundWindow, sb, 256);
                var windowTitle = sb.ToString();

                // Get process ID from window handle
                _ = GetWindowThreadProcessId(foregroundWindow, out var pid);
                
                try
                {
                    var process = Process.GetProcessById((int)pid);

                    return Task.FromResult<FocusedApplication?>(new FocusedApplication
                    {
                        WindowTitle = windowTitle,
                        AppName = process.ProcessName,
                        ProcessPath = process.MainModule?.FileName ?? string.Empty,
                        Tags = ClassifyApplication(process.ProcessName),
                        SwitchedAt = DateTime.UtcNow
                    });
                }
                catch (ArgumentException)
                {
                    // Process no longer exists
                    return Task.FromResult<FocusedApplication?>(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get focused application");
                return Task.FromResult<FocusedApplication?>(null);
            }
        }

        /// <summary>
        /// Detect keyboard and mouse activity intensity.
        /// Tracks cursor movement and key presses in the time window.
        /// </summary>
        protected override Task<int> GetActivityIntensityInternalAsync(int minutesWindow, CancellationToken ct)
        {
            try
            {
                int intensity = 0;

                // Check cursor movement
                if (GetCursorPos(out var currentPos))
                {
                    int distance = Math.Abs(currentPos.X - _lastCursorPos.X) + Math.Abs(currentPos.Y - _lastCursorPos.Y);
                    if (distance > CURSOR_MOVEMENT_THRESHOLD)
                    {
                        intensity += 15;
                        _lastCursorPos = currentPos;
                    }
                }

                // Check for key presses (sample common keys)
                int[] keysToCheck = { 0x20, 0x41, 0x4B, 0x08, 0x0D };  // Space, A, K, Backspace, Enter
                foreach (var key in keysToCheck)
                {
                    if ((GetAsyncKeyState(key) & 0x8000) != 0)
                    {
                        intensity += 15;
                        break;
                    }
                }

                // Recent activity contributes to intensity
                if (DateTime.UtcNow - _lastKeyboardMouseActivity < TimeSpan.FromSeconds(5))
                {
                    intensity += 20;
                }

                // Clean up old activity history (older than window)
                var cutoffTime = DateTime.UtcNow.AddMinutes(-minutesWindow);
                while (_activityHistory.Count > 0 && _activityHistory.Peek() < cutoffTime)
                {
                    _activityHistory.Dequeue();
                }

                // Activity history contributes to intensity
                intensity += Math.Min(_activityHistory.Count * 5, 30);

                return Task.FromResult(Math.Min(intensity, 100));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get activity intensity");
                return Task.FromResult(0);
            }
        }

        /// <summary>
        /// Classify application by process name for tagging.
        /// Helps categorize productivity, distraction, focus music, etc.
        /// </summary>
        private string[] ClassifyApplication(string appName)
        {
            var lower = appName.ToLowerInvariant();

            if (lower.Contains("winword") || lower.Contains("excel") || lower.Contains("powerpoint"))
                return new[] { "productivity", "office" };

            if (lower.Contains("chrome") || lower.Contains("firefox") || lower.Contains("edge") || lower.Contains("msedge"))
                return new[] { "browser" };

            if (lower.Contains("discord") || lower.Contains("slack") || lower.Contains("teams"))
                return new[] { "communication", "distraction" };

            if (lower.Contains("spotify") || lower.Contains("youtube") || lower.Contains("music"))
                return new[] { "focus_music", "media" };

            if (lower.Contains("code") || lower.Contains("visual") || lower.Contains("notepad"))
                return new[] { "coding", "productivity" };

            if (lower.Contains("explorer") || lower.Contains("cmd"))
                return new[] { "system" };

            return new[] { "other" };
        }

        /// <summary>
        /// Record keyboard or mouse activity.
        /// Called internally when input is detected.
        /// </summary>
        public void RecordKeyboardMouseActivity()
        {
            _lastKeyboardMouseActivity = DateTime.UtcNow;
            _activityHistory.Enqueue(DateTime.UtcNow);
            RecordActivity();
        }

        /// <summary>
        /// Cleanup: unhook the window event.
        /// </summary>
        ~WindowsActivityDetectionService()
        {
            if (_m_hook != IntPtr.Zero)
            {
                if (!UnhookWinEvent(_m_hook))
                {
                    _logger.LogWarning("Failed to unhook WinEventHook");
                }
                _m_hook = IntPtr.Zero;
            }
        }
    }
}

#endif
