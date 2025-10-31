using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FocusDock.SystemInterop;

public class WindowInfo
{
    public nint Hwnd { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public bool IsVisible { get; set; }
    public bool IsPinned { get; set; }
}

public static class User32
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxLength);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public const int SW_RESTORE = 9;

    public const int WM_GETICON = 0x7F;
    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;
    public const int ICON_SMALL2 = 2;

    [DllImport("user32.dll", EntryPoint = "GetClassLong")] // 32-bit
    public static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")] // 64-bit
    public static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

    public const int GCL_HICON = -14;
    public const int GCL_HICONSM = -34;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

[Flags]
public enum SetWindowPosFlags : uint
{
    SWP_NOSIZE = 0x0001,
    SWP_NOMOVE = 0x0002,
    SWP_NOZORDER = 0x0004,
    SWP_NOREDRAW = 0x0008,
    SWP_NOACTIVATE = 0x0010,
    SWP_FRAMECHANGED = 0x0020,
    SWP_SHOWWINDOW = 0x0040,
    SWP_HIDEWINDOW = 0x0080,
    SWP_NOCOPYBITS = 0x0100,
    SWP_NOOWNERZORDER = 0x0200,
    SWP_NOSENDCHANGING = 0x0400,
}

public class WindowTracker
{
    private readonly System.Timers.Timer _timer = new(2000); // Increased to 2 seconds for better performance
    private readonly StringBuilder _sharedStringBuilder = new(256); // Reusable StringBuilder to reduce allocations
    public event EventHandler<List<WindowInfo>>? WindowsUpdated;

    public WindowTracker()
    {
        _timer.Elapsed += (_, _) =>
        {
            var list = GetCurrentWindows();
            WindowsUpdated?.Invoke(this, list);
        };
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    public List<WindowInfo> GetCurrentWindows()
    {
        var windows = new List<WindowInfo>();
        IntPtr shellWindow = User32.GetShellWindow();

        User32.EnumWindows((hWnd, lParam) =>
        {
            if (hWnd == shellWindow) return true;
            if (!User32.IsWindowVisible(hWnd)) return true;

            int length = User32.GetWindowTextLength(hWnd);
            if (length == 0) return true;

            // Reuse StringBuilder instead of creating new one each time
            _sharedStringBuilder.Clear();
            if (_sharedStringBuilder.Capacity < length + 1)
            {
                _sharedStringBuilder.Capacity = length + 1;
            }
            
            User32.GetWindowText(hWnd, _sharedStringBuilder, _sharedStringBuilder.Capacity);
            string title = _sharedStringBuilder.ToString();

            if (string.IsNullOrWhiteSpace(title)) return true;

            User32.GetWindowThreadProcessId(hWnd, out var pid);
            string processName = "";
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch { /* ignore */ }

            windows.Add(new WindowInfo
            {
                Hwnd = hWnd,
                Title = title,
                ProcessId = (int)pid,
                ProcessName = processName,
                IsVisible = true,
            });

            return true;
        }, IntPtr.Zero);

        return windows;
    }
}
