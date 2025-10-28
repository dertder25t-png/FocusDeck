using System;
using System.Collections.Generic;
using System.Linq;
using FocusDock.Data.Models;
using FocusDock.SystemInterop;

namespace FocusDock.Core.Services;

public class ReminderService
{
    private readonly Dictionary<nint, DateTime> _lastSeen = new();
    private readonly TimeSpan _staleThreshold = TimeSpan.FromMinutes(20);

    public event EventHandler<List<WindowInfo>>? StaleWindowsDetected;

    public void UpdateSeen(IEnumerable<WindowInfo> windows)
    {
        var now = DateTime.UtcNow;
        foreach (var w in windows)
        {
            _lastSeen[w.Hwnd] = now;
        }

        var stale = GetStale(windows, now);

        if (stale.Count > 0)
        {
            StaleWindowsDetected?.Invoke(this, stale!);
        }
    }

    public void Snooze(IEnumerable<WindowInfo> windows)
    {
        var now = DateTime.UtcNow;
        foreach (var w in windows)
        {
            _lastSeen[w.Hwnd] = now;
        }
    }

    public List<WindowInfo> GetStale(IEnumerable<WindowInfo> windows)
        => GetStale(windows, DateTime.UtcNow);

    private List<WindowInfo> GetStale(IEnumerable<WindowInfo> windows, DateTime now)
    {
        return _lastSeen
            .Where(kv => now - kv.Value > _staleThreshold)
            .Select(kv => windows.FirstOrDefault(w => w.Hwnd == kv.Key))
            .Where(w => w != null)!
            .ToList()!;
    }
}
