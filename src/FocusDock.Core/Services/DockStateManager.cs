using System;
using System.Windows;
using System.Windows.Media.Animation;
using FocusDock.Data.Models;

namespace FocusDock.Core.Services;

public class DockStateManager
{
    private readonly Window _window;
    private readonly System.Timers.Timer _collapseTimer = new(600);
    private readonly System.Windows.Threading.DispatcherTimer? _hideTimer;
    
    // Tuned for better readability while keeping footprint small
    public double CollapsedHeight { get; } = 8;
    public double ExpandedHeight { get; } = 56;
    
    // Auto-hide settings
    private const int AUTO_HIDE_OFFSET = -80;
    private const int VISIBLE_PEEK = 3;
    
    private bool _focusMode;
    private DockEdge _currentEdge = DockEdge.Top;
    private bool _autoHideEnabled = false;

    public DockStateManager(Window window)
    {
        _window = window;
        _collapseTimer.AutoReset = false;
        _collapseTimer.Elapsed += (_, _) => _window.Dispatcher.Invoke(Collapse);
        
        // Initialize auto-hide timer
        _hideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        _hideTimer.Tick += (s, args) =>
        {
            _hideTimer.Stop();
            if (!_window.IsMouseOver && _autoHideEnabled)
            {
                HideDock();
            }
        };
    }

    public void SetEdge(DockEdge edge)
    {
        _currentEdge = edge;
    }

    public void SetAutoHide(bool enabled)
    {
        _autoHideEnabled = enabled;
        if (!enabled)
        {
            ShowDock();
        }
    }

    public void OnMouseEnter()
    {
        _hideTimer?.Stop();
        if (_autoHideEnabled)
        {
            ShowDock();
        }
        else
        {
            Expand();
        }
    }

    public void OnMouseLeave()
    {
        if (_autoHideEnabled)
        {
            _hideTimer?.Start();
        }
        else
        {
            CollapseIfAway();
        }
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

    private void ShowDock()
    {
        _window.Dispatcher.Invoke(() =>
        {
            DoubleAnimation anim;
            
            switch (_currentEdge)
            {
                case DockEdge.Top:
                    anim = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    _window.BeginAnimation(Window.TopProperty, anim);
                    break;
                    
                case DockEdge.Bottom:
                    anim = new DoubleAnimation
                    {
                        To = SystemParameters.WorkArea.Height - _window.Height,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    _window.BeginAnimation(Window.TopProperty, anim);
                    break;
                    
                case DockEdge.Left:
                    anim = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    _window.BeginAnimation(Window.LeftProperty, anim);
                    break;
                    
                case DockEdge.Right:
                    anim = new DoubleAnimation
                    {
                        To = SystemParameters.WorkArea.Width - _window.Width,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    _window.BeginAnimation(Window.LeftProperty, anim);
                    break;
            }
            
            _window.Opacity = 0.98;
        });
    }

    private void HideDock()
    {
        _window.Dispatcher.Invoke(() =>
        {
            DoubleAnimation anim;
            
            switch (_currentEdge)
            {
                case DockEdge.Top:
                    anim = new DoubleAnimation
                    {
                        To = AUTO_HIDE_OFFSET + VISIBLE_PEEK,
                        Duration = TimeSpan.FromMilliseconds(250),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    _window.BeginAnimation(Window.TopProperty, anim);
                    break;
                    
                case DockEdge.Bottom:
                    anim = new DoubleAnimation
                    {
                        To = SystemParameters.WorkArea.Height - VISIBLE_PEEK,
                        Duration = TimeSpan.FromMilliseconds(250),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    _window.BeginAnimation(Window.TopProperty, anim);
                    break;
                    
                case DockEdge.Left:
                    anim = new DoubleAnimation
                    {
                        To = AUTO_HIDE_OFFSET + VISIBLE_PEEK,
                        Duration = TimeSpan.FromMilliseconds(250),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    _window.BeginAnimation(Window.LeftProperty, anim);
                    break;
                    
                case DockEdge.Right:
                    anim = new DoubleAnimation
                    {
                        To = SystemParameters.WorkArea.Width - VISIBLE_PEEK,
                        Duration = TimeSpan.FromMilliseconds(250),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    _window.BeginAnimation(Window.LeftProperty, anim);
                    break;
            }
            
            _window.Opacity = 0.8;
        });
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
