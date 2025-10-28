using System;
using System.Collections.Generic;
using FocusDock.Data.Models;
using FocusDock.SystemInterop;

namespace FocusDock.Core.Services;

public class PinService
{
    private readonly HashSet<nint> _pinned = new();
    private readonly HashSet<(string Process, string Title)> _pinnedKeys;
    public event EventHandler? PinsChanged;

    public PinService()
    {
        _pinnedKeys = FocusDock.Data.PinsStore.Load();
    }

    public bool IsPinned(WindowInfo w) => _pinned.Contains(w.Hwnd) || _pinnedKeys.Contains((w.ProcessName, w.Title));

    public void TogglePin(WindowInfo w)
    {
        if (_pinned.Contains(w.Hwnd))
        {
            _pinned.Remove(w.Hwnd);
            w.IsPinned = false;
            _pinnedKeys.Remove((w.ProcessName, w.Title));
        }
        else
        {
            _pinned.Add(w.Hwnd);
            w.IsPinned = true;
            _pinnedKeys.Add((w.ProcessName, w.Title));
        }
        FocusDock.Data.PinsStore.Save(_pinnedKeys);
        PinsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetPinned(WindowInfo w, bool on)
    {
        if (on)
        {
            _pinned.Add(w.Hwnd);
            _pinnedKeys.Add((w.ProcessName, w.Title));
            w.IsPinned = true;
        }
        else
        {
            _pinned.Remove(w.Hwnd);
            _pinnedKeys.Remove((w.ProcessName, w.Title));
            w.IsPinned = false;
        }
        FocusDock.Data.PinsStore.Save(_pinnedKeys);
        PinsChanged?.Invoke(this, EventArgs.Empty);
    }
}
