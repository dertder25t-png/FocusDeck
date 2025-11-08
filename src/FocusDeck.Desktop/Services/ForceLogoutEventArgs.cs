using System;

namespace FocusDeck.Desktop.Services;

public class ForceLogoutEventArgs : EventArgs
{
    public string Reason { get; }
    public string? DeviceId { get; }

    public ForceLogoutEventArgs(string reason, string? deviceId)
    {
        Reason = string.IsNullOrWhiteSpace(reason) ? "Session expired" : reason;
        DeviceId = deviceId;
    }
}

