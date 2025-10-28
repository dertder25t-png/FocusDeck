using System;
using System.Runtime.InteropServices;

namespace FocusDock.SystemInterop;

/// <summary>
/// Detects Windows lock/unlock and session change events
/// </summary>
public class SessionMonitor
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, uint Flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterPowerSettingNotification(IntPtr hHandle);

    private static readonly Guid GUID_SESSION_DISPLAY_OFF = new("0e0e75697-9f64-475d-b260-5f15b0d2cb47");
    private static readonly Guid GUID_SESSION_USER_PRESENT = new("3c0f643d-e563-4522-a406-0257132 8f13c");

    public event EventHandler<SessionEventArgs>? SessionStatusChanged;

    private IntPtr _notificationHandle = IntPtr.Zero;

    public void Start()
    {
        // Could register for power setting notifications, but for now we'll use a simpler approach
        // by hooking into system events through a hidden form window
    }

    public void Stop()
    {
        if (_notificationHandle != IntPtr.Zero)
        {
            UnregisterPowerSettingNotification(_notificationHandle);
            _notificationHandle = IntPtr.Zero;
        }
    }

    protected void OnSessionStatusChanged(SessionStatus status, string reason)
    {
        SessionStatusChanged?.Invoke(this, new SessionEventArgs { Status = status, Reason = reason });
    }
}

public enum SessionStatus
{
    Locked,
    Unlocked,
    ScreenOff,
    ScreenOn
}

public class SessionEventArgs : EventArgs
{
    public SessionStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
}
