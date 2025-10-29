using System;
using System.Windows;

namespace FocusDock.Core.Services;

public class DockStateManager
{
    private readonly Window _window;
    private readonly System.Timers.Timer _collapseTimer = new(600);
    // Tuned for better readability while keeping footprint small
    public double CollapsedHeight { get; } = 8;
    public double ExpandedHeight { get; } = 56;
    private bool _focusMode;

    public DockStateManager(Window window)
    {
        _window = window;
        _collapseTimer.AutoReset = false;
        _collapseTimer.Elapsed += (_, _) => _window.Dispatcher.Invoke(Collapse);
    }

    public void Expand()
    {
        _collapseTimer.Stop();
        _window.Height = ExpandedHeight;
        _window.Opacity = 0.98;
    }

    public void Collapse()
    {
        _window.Height = CollapsedHeight;
        _window.Opacity = 0.8;
    }

    public void CollapseIfAway()
    {
        _collapseTimer.Stop();
        _collapseTimer.Start();
    }

    public void ToggleFocusMode()
    {
        SetFocusMode(!_focusMode);
    }

    public void SetFocusMode(bool on)
    {
        _focusMode = on;
        _window.Opacity = _focusMode ? 0.92 : 0.98;
    }
}
