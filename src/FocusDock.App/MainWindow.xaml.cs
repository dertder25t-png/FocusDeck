using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using FocusDock.Core.Services;
using FocusDock.SystemInterop;
using FocusDock.Data;
using FocusDock.Data.Models;
using System.Collections.ObjectModel;
using FocusDock.App.Controls;

namespace FocusDock.App;

public partial class MainWindow : Window
{
    private readonly DockStateManager _dockState;
    private readonly WindowTracker _windowTracker;
    private readonly LayoutManager _layoutManager;
    private readonly System.Timers.Timer _clockTimer = new(1000);
    private readonly ReminderService _reminder = new();
    private readonly PinService _pins = new();
    private readonly WorkspaceManager _workspaces;
    private readonly CalendarService _calendar = new();
    private readonly TodoService _todos = new();
    private readonly StudyPlanService _studyPlans = new();
    private AppSettings _settings;
    private AutomationService _automation;
    private System.Windows.Threading.DispatcherTimer _automationPreviewTimer = new();
    private TimeRule? _automationPendingRule;
    private int _automationCountdown = 0;

    public DockViewModel VM { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = VM;

        _dockState = new DockStateManager(this);
        _windowTracker = new WindowTracker();
        _layoutManager = new LayoutManager();
        _workspaces = new WorkspaceManager(_pins);
        _settings = SettingsStore.LoadSettings();

        _windowTracker.WindowsUpdated += (s, e) =>
        {
            var groups = e.GroupBy(w => w.ProcessName)
                .Select(g => new WindowGroup
                {
                    GroupName = g.Key,
                    Windows = g.ToList()
                }).ToList();
            foreach (var g in groups)
            {
                foreach (var w in g.Windows)
                {
                    w.IsPinned = _pins.IsPinned(w);
                }
            }
            _reminder.UpdateSeen(e);
            Dispatcher.Invoke(() => VM.WindowGroups = groups);
        };

        _reminder.StaleWindowsDetected += (_, stale) =>
        {
            Dispatcher.Invoke(() =>
            {
                Title = $"FocusDock - {stale.Count} stale";
                TxtRemindersCount.Text = stale.Count.ToString();
            });
        };

        _windowTracker.Start();

        MouseEnter += (_, _) => _dockState.Expand();
        // Temporarily disable auto-collapse to aid visibility/debugging
        // MouseLeave += (_, _) => _dockState.CollapseIfAway();

        BtnFocus.Click += (_, _) => _dockState.ToggleFocusMode();
        BtnDock.Click += (_, _) => ShowDockMenu();
        BtnCalendar.Click += (_, _) => ShowCalendarMenu();
        BtnReminders.Click += (_, _) => ShowTodosMenu();
        BtnStudySession.Click += (_, _) => ShowStudySessionMenu();
        BtnWorkspace.Click += (_, _) => ShowWorkspaceMenu();
        BtnAutomations.Click += (_, _) => ShowAutomationsMenu();
        BtnReminders.Click += (_, _) => ShowRemindersMenu();
        BtnSettings.Click += (_, _) => ShowSettingsWindow();
        BtnLayouts.Click += (_, _) =>
        {
            ShowLayoutsMenu();
        };

        _clockTimer.Elapsed += (_, _) => Dispatcher.Invoke(() =>
        {
            TxtClock.Text = DateTime.Now.ToString("h:mm tt");
        });
        _clockTimer.Start();

        // Automation
        _automation = new AutomationService();
        _automation.Config = AutomationStore.Load();
        _automation.RuleTriggered += OnAutomationRuleTriggered;
        _automation.Start();

        _automationPreviewTimer.Interval = TimeSpan.FromSeconds(1);
        _automationPreviewTimer.Tick += OnAutomationPreviewTick;
        
        // Auto-restore last workspace on startup
        Loaded += (_, _) =>
        {
            RestoreLastWorkspace();
            _dockState.Expand();
            // Ensure window is in a visible, normal state on load
            WindowState = WindowState.Normal;
            Topmost = true;
            try { Focus(); } catch { }
        };

        // Dev fallback: F8 forces the dock to a visible size/position
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.F8)
            {
                try
                {
                    _dockState.Expand();
                    Opacity = 1.0;
                    WindowState = WindowState.Normal;
                    Topmost = true;
                    var wa = SystemParameters.WorkArea;
                    Width = Math.Min(1200, wa.Width - 200);
                    Height = 120;
                    Left = wa.Left + (wa.Width - Width) / 2;
                    Top = wa.Top + 100;
                }
                catch { }
            }
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PositionDock();
    }

    private void PositionDock()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        var index = _settings.MonitorIndex >= 0 && _settings.MonitorIndex < screens.Length ? _settings.MonitorIndex : 0;
        var screen = screens[index];
        if (screen is null) return;
        var area = screen.WorkingArea;
        switch (_settings.Edge)
        {
            case DockEdge.Top:
                Left = area.Left + 10; Width = area.Width - 20; Height = _dockState.ExpandedHeight; Top = area.Top; break;
            case DockEdge.Bottom:
                Left = area.Left + 10; Width = area.Width - 20; Height = _dockState.ExpandedHeight; Top = area.Bottom - _dockState.CollapsedHeight; break;
            case DockEdge.Left:
                Left = area.Left + 6; Width = 220; Height = area.Height - 20; Top = area.Top + 10; break;
            case DockEdge.Right:
                Left = area.Right - 220 - 6; Width = 220; Height = area.Height - 20; Top = area.Top + 10; break;
        }
        // Start visible; collapsing is handled on mouse leave and timers
        // _dockState.Collapse();

        // Adjust UI layout for vertical dock
        bool vertical = _settings.Edge == DockEdge.Left || _settings.Edge == DockEdge.Right;
        PanelLeft.Orientation = vertical ? System.Windows.Controls.Orientation.Vertical : System.Windows.Controls.Orientation.Horizontal;
        PanelRight.Orientation = vertical ? System.Windows.Controls.Orientation.Vertical : System.Windows.Controls.Orientation.Horizontal;
        var panel = vertical ? (ItemsPanelTemplate)FindResource("VerticalItemsPanel") : (ItemsPanelTemplate)FindResource("HorizontalItemsPanel");
        GroupsList.ItemsPanel = panel;
    }

    private void OnWindowItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is WindowInfo w)
        {
            _pins.TogglePin(w);
            // Update bound list to reflect pin state immediately
            var items = VM.WindowGroups.SelectMany(g => g.Windows).ToList();
            _reminder.UpdateSeen(items);
        }
    }

    private void OnWindowItemActivate(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is WindowInfo w)
        {
            try
            {
                var h = (IntPtr)w.Hwnd;
                if (User32.IsIconic(h))
                {
                    User32.ShowWindow(h, User32.SW_RESTORE);
                }
                User32.SetForegroundWindow(h);
            }
            catch { /* ignore focus errors */ }
        }
    }

    private void OnWindowItemRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is WindowInfo w)
        {
            var menu = new System.Windows.Controls.ContextMenu();
            var miFocus = new System.Windows.Controls.MenuItem { Header = "Focus" };
            miFocus.Click += (_, _) =>
            {
                try
                {
                    var h = (IntPtr)w.Hwnd;
                    if (User32.IsIconic(h)) User32.ShowWindow(h, User32.SW_RESTORE);
                    User32.SetForegroundWindow(h);
                }
                catch { }
            };
            var miPin = new System.Windows.Controls.MenuItem { Header = w.IsPinned ? "Unpin" : "Pin" };
            miPin.Click += (_, _) =>
            {
                _pins.TogglePin(w);
                var items = VM.WindowGroups.SelectMany(g => g.Windows).ToList();
                _reminder.UpdateSeen(items);
            };
            var miClose = new System.Windows.Controls.MenuItem { Header = "Close" };
            miClose.Click += (_, _) => User32.SendMessage((IntPtr)w.Hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
            menu.Items.Add(miFocus);
            menu.Items.Add(miPin);
            menu.Items.Add(new System.Windows.Controls.Separator());
            menu.Items.Add(miClose);
            btn.ContextMenu = menu;
            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }

    private void ShowDockMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        var monitors = System.Windows.Forms.Screen.AllScreens;
        var monRoot = new System.Windows.Controls.MenuItem { Header = "Monitor" };
        for (int i = 0; i < monitors.Length; i++)
        {
            var mi = new System.Windows.Controls.MenuItem { Header = $"Monitor {i + 1}", IsCheckable = true, IsChecked = _settings.MonitorIndex == i };
            int idx = i;
            mi.Click += (_, _) => { _settings.MonitorIndex = idx; SettingsStore.SaveSettings(_settings); PositionDock(); };
            monRoot.Items.Add(mi);
        }
        menu.Items.Add(monRoot);

        var edgeRoot = new System.Windows.Controls.MenuItem { Header = "Edge" };
        foreach (DockEdge edge in Enum.GetValues(typeof(DockEdge)))
        {
            var mi = new System.Windows.Controls.MenuItem { Header = edge.ToString(), IsCheckable = true, IsChecked = _settings.Edge == edge };
            var eedge = edge;
            mi.Click += (_, _) => { _settings.Edge = eedge; SettingsStore.SaveSettings(_settings); PositionDock(); };
            edgeRoot.Items.Add(mi);
        }
        menu.Items.Add(edgeRoot);

        menu.Items.Add(new System.Windows.Controls.Separator());
        var miIconOnly = new System.Windows.Controls.MenuItem { Header = "Icon-only mode", IsCheckable = true, IsChecked = VM.IconOnlyMode };
        miIconOnly.Click += (_, _) => { VM.IconOnlyMode = !VM.IconOnlyMode; };
        menu.Items.Add(miIconOnly);

        BtnDock.ContextMenu = menu;
        menu.PlacementTarget = BtnDock;
        menu.IsOpen = true;
    }

    private void ShowWorkspaceMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        void Add(string header, Action onClick)
        {
            var mi = new System.Windows.Controls.MenuItem { Header = header };
            mi.Click += (_, _) => onClick();
            menu.Items.Add(mi);
        }

        Add("Save Workspace As...", () =>
        {
            var name = PromptForName("Workspace");
            if (string.IsNullOrWhiteSpace(name)) return;
            var ws = _workspaces.CaptureCurrent(name!, VM.LastAppliedPresetName);
            WorkspaceStore.SaveOrUpdate(ws);
        });

        Add("Park Today", () => ParkToday());

        menu.Items.Add(new System.Windows.Controls.Separator());
        var all = WorkspaceStore.LoadAll();
        if (all.Count == 0)
        {
            menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "No workspaces", IsEnabled = false });
        }
        else
        {
            foreach (var ws in all)
            {
                var mi = new System.Windows.Controls.MenuItem { Header = $"Restore: {ws.Name}" };
                mi.Click += (_, _) =>
                {
                    _workspaces.Restore(ws);
                    if (!string.IsNullOrWhiteSpace(ws.PresetName))
                    {
                        var p = PresetService.Load().FirstOrDefault(x => x.Name == ws.PresetName);
                        if (p != null)
                        {
                            var b = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
                            _layoutManager.ApplyPresetForBounds(p, b.Width, b.Height);
                            VM.LastAppliedPresetName = p.Name;
                        }
                    }
                };
                menu.Items.Add(mi);
            }
        }

        BtnWorkspace.ContextMenu = menu;
        menu.PlacementTarget = BtnWorkspace;
        menu.IsOpen = true;
    }

    private void ShowRemindersMenu()
    {
        var tracker = new WindowTracker();
        var current = tracker.GetCurrentWindows();
        _reminder.UpdateSeen(current);
        var stale = _reminder.GetStale(current);

        var menu = new System.Windows.Controls.ContextMenu();
        if (stale.Count == 0)
        {
            menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "No reminders", IsEnabled = false });
        }
        else
        {
            foreach (var w in stale)
            {
                var root = new System.Windows.Controls.MenuItem { Header = w.Title };
                var miClose = new System.Windows.Controls.MenuItem { Header = "Close" };
                miClose.Click += (_, _) => User32.SendMessage((IntPtr)w.Hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                var miSnooze = new System.Windows.Controls.MenuItem { Header = "Snooze 20m" };
                miSnooze.Click += (_, _) => _reminder.Snooze(new[] { w });
                var miSave = new System.Windows.Controls.MenuItem { Header = "Save (Pin)" };
                miSave.Click += (_, _) => _pins.TogglePin(w);
                root.Items.Add(miClose);
                root.Items.Add(miSnooze);
                root.Items.Add(miSave);
                menu.Items.Add(root);
            }
        }

        BtnReminders.ContextMenu = menu;
        menu.PlacementTarget = BtnReminders;
        menu.IsOpen = true;
    }

    private void ShowAutomationsMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        void Add(string header, Action onClick)
        {
            var mi = new System.Windows.Controls.MenuItem { Header = header };
            mi.Click += (_, _) => onClick();
            menu.Items.Add(mi);
        }

        Add("Enable Example: Weekdays 9-5 Three Column (Monitor 1)", () =>
        {
            var rules = AutomationStore.Load();
            rules.Rules.Add(new TimeRule
            {
                Name = "Workday Layout",
                DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                Start = "09:00",
                End = "17:00",
                Action = RuleAction.ApplyPreset,
                PresetName = "Three Column",
                MonitorIndex = 0
            });
            AutomationStore.Save(rules);
            _automation.Config = rules;
        });

        Add("Enable Example: Evenings Focus Mode 19-23", () =>
        {
            var rules = AutomationStore.Load();
            rules.Rules.Add(new TimeRule
            {
                Name = "Evening Focus",
                DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                Start = "19:00",
                End = "23:00",
                Action = RuleAction.FocusModeOn
            });
            AutomationStore.Save(rules);
            _automation.Config = rules;
        });

        BtnAutomations.ContextMenu = menu;
        menu.PlacementTarget = BtnAutomations;
        menu.IsOpen = true;
    }

    private bool ApplyPresetByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var preset = PresetService.Load().FirstOrDefault(p => p.Name == name);
        if (preset == null) return false;
        var b = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
        _layoutManager.ApplyPresetForBounds(preset, b.Width, b.Height);
        VM.LastAppliedPresetName = preset.Name;
        return true;
    }

    private bool ApplyPresetByName(string? name, int? monitorIndex)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var preset = PresetService.Load().FirstOrDefault(p => p.Name == name);
        if (preset == null) return false;
        var screens = System.Windows.Forms.Screen.AllScreens;
        var screen = (monitorIndex.HasValue && monitorIndex.Value >= 0 && monitorIndex.Value < screens.Length)
            ? screens[monitorIndex.Value]
            : System.Windows.Forms.Screen.PrimaryScreen!;
        var b = screen.Bounds;
        _layoutManager.ApplyPresetForBounds(preset, b.Width, b.Height);
        VM.LastAppliedPresetName = preset.Name;
        return true;
    }

    private void SetFocusMode(bool on)
    {
        _dockState.SetFocusMode(on);
    }

    private void OnAutomationRuleTriggered(object? sender, TimeRule rule)
    {
        if (_automationPendingRule != null) return; // already previewing one
        _automationPendingRule = rule;
        _automationCountdown = 2;
        TxtAutomationMessage.Text = rule.Action switch
        {
            RuleAction.ApplyPreset => $"Applying preset '{rule.PresetName}'",
            RuleAction.FocusModeOn => "Enabling Focus Mode",
            RuleAction.FocusModeOff => "Disabling Focus Mode",
            _ => "Applying automation"
        };
        TxtAutomationCountdown.Text = $"in {_automationCountdown}s";
        AutomationBanner.Visibility = Visibility.Visible;
        BtnAutomationUndo.Click -= OnAutomationUndo;
        BtnAutomationUndo.Click += OnAutomationUndo;
        _automationPreviewTimer.Start();
    }

    private void OnAutomationUndo(object sender, RoutedEventArgs e)
    {
        _automationPreviewTimer.Stop();
        _automationPendingRule = null;
        AutomationBanner.Visibility = Visibility.Collapsed;
    }

    private void OnAutomationPreviewTick(object? sender, EventArgs e)
    {
        if (_automationPendingRule == null) { _automationPreviewTimer.Stop(); AutomationBanner.Visibility = Visibility.Collapsed; return; }
        _automationCountdown--;
        if (_automationCountdown > 0)
        {
            TxtAutomationCountdown.Text = $"in {_automationCountdown}s";
            return;
        }
        _automationPreviewTimer.Stop();
        var rule = _automationPendingRule; _automationPendingRule = null;
        AutomationBanner.Visibility = Visibility.Collapsed;
        if (rule == null) return;
        switch (rule.Action)
        {
            case RuleAction.ApplyPreset:
                ApplyPresetByName(rule.PresetName, rule.MonitorIndex);
                break;
            case RuleAction.FocusModeOn:
                SetFocusMode(true);
                break;
            case RuleAction.FocusModeOff:
                SetFocusMode(false);
                break;
        }
    }

    private void ShowLayoutsMenu()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;

        var menu = new System.Windows.Controls.ContextMenu();

        void AddMenuItem(string header, Action onClick)
        {
            var mi = new System.Windows.Controls.MenuItem { Header = header };
            mi.Click += (_, _) => onClick();
            menu.Items.Add(mi);
        }

        foreach (var scr in screens)
        {
            var idx = Array.IndexOf(screens, scr) + 1;
            var sub = new System.Windows.Controls.MenuItem { Header = $"Monitor {idx}" };
            void Add(string header, Func<int, int, LayoutPreset> factory)
            {
                var mi = new System.Windows.Controls.MenuItem { Header = header };
                mi.Click += (_, _) =>
                {
                    var b = scr.Bounds;
                    var preset = factory(b.Width, b.Height);
                    var name = PromptForName(preset.Name);
                    if (!string.IsNullOrWhiteSpace(name)) preset.Name = name!;
                    _layoutManager.ApplyPreset(preset);
                    PresetService.SaveOrUpdate(preset);
                    VM.LastAppliedPresetName = preset.Name;
                    RefreshPresets();
                };
                sub.Items.Add(mi);
            }
            Add("Two Column", LayoutPreset.CreateTwoColumn);
            Add("Three Column", LayoutPreset.CreateThreeColumn);
            Add("Grid 2x2", LayoutPreset.CreateGrid2x2);
            menu.Items.Add(sub);
        }

        menu.Items.Add(new System.Windows.Controls.Separator());

        var saved = PresetService.Load();
        if (saved.Count == 0)
        {
            var empty = new System.Windows.Controls.MenuItem { Header = "No saved presets", IsEnabled = false };
            menu.Items.Add(empty);
        }
        else
        {
            foreach (var p in saved)
            {
                var name = p.Name;
                AddMenuItem($"Apply Saved: {name}", () =>
                {
                    var b = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
                    _layoutManager.ApplyPresetForBounds(p, b.Width, b.Height);
                    VM.LastAppliedPresetName = p.Name;
                });
            }
        }

        BtnLayouts.ContextMenu = menu;
        menu.PlacementTarget = BtnLayouts;
        menu.IsOpen = true;
    }

    private void RefreshPresets()
    {
        VM.SavedPresets = new ObservableCollection<LayoutPreset>(PresetService.Load());
    }

    private string? PromptForName(string defaultName)
    {
        var dlg = new InputDialog("Save Preset", "Preset name:", defaultName) { Owner = this };
        return dlg.ShowDialog() == true ? dlg.ResultText : null;
    }

    private void RestoreLastWorkspace()
    {
        try
        {
            var all = WorkspaceStore.LoadAll();
            if (all.Count == 0) return;
            
            // Load the "Park Today" workspace if it exists, otherwise load the most recently modified
            var ws = all.FirstOrDefault(w => w.Name == "Park Today") ?? all.LastOrDefault();
            if (ws != null)
            {
                _workspaces.Restore(ws);
                if (!string.IsNullOrWhiteSpace(ws.PresetName))
                {
                    ApplyPresetByName(ws.PresetName);
                }
                Title = $"FocusDock - Restored: {ws.Name}";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to restore workspace: {ex.Message}");
        }
    }

    private void ParkToday()
    {
        try
        {
            var ws = _workspaces.CaptureCurrent("Park Today", VM.LastAppliedPresetName);
            WorkspaceStore.SaveOrUpdate(ws);
            Title = "FocusDock - Parked for today!";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to park today: {ex.Message}");
        }
    }

    private void ShowCalendarMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        void Add(string header, Action onClick)
        {
            var mi = new System.Windows.Controls.MenuItem { Header = header };
            mi.Click += (_, _) => onClick();
            menu.Items.Add(mi);
        }

        var upcoming = _calendar.GetUpcomingEvents(7);
        if (upcoming.Count > 0)
        {
            var eventsItem = new System.Windows.Controls.MenuItem { Header = "📅 Upcoming Events" };
            foreach (var evt in upcoming.Take(5))
            {
                var time = evt.StartTime.ToString("M/d h:mm tt");
                eventsItem.Items.Add(new System.Windows.Controls.MenuItem 
                { 
                    Header = $"{evt.Title} - {time}",
                    IsEnabled = false
                });
            }
            menu.Items.Add(eventsItem);
        }

        var assignments = _calendar.GetUpcomingAssignments(14);
        if (assignments.Count > 0)
        {
            var assignItem = new System.Windows.Controls.MenuItem { Header = "📋 Canvas Assignments" };
            foreach (var asn in assignments.Take(5))
            {
                var due = asn.DueDate.ToString("M/d h:mm tt");
                assignItem.Items.Add(new System.Windows.Controls.MenuItem 
                { 
                    Header = $"{asn.CourseName}: {asn.Title} - {due}",
                    IsEnabled = false
                });
            }
            menu.Items.Add(assignItem);
        }

        if (menu.Items.Count > 0)
        {
            menu.Items.Add(new System.Windows.Controls.Separator());
        }

        Add("Calendar Settings...", () =>
        {
            System.Windows.MessageBox.Show(
                "Calendar sync disabled - Configure in Settings\n\n" +
                "To enable Google Calendar:\n" +
                "1. Get API token from Google Cloud Console\n" +
                "2. Add in Calendar Settings\n\n" +
                "To enable Canvas:\n" +
                "1. Get API token from Canvas settings\n" +
                "2. Add Canvas instance URL",
                "Calendar Configuration");
        });

        Add("Refresh Now", async () => await _calendar.ManualSync());
        Add("Create Test Event", () =>
        {
            var testEvent = new FocusDock.Data.Models.CalendarEvent
            {
                Title = "Test Class",
                StartTime = DateTime.Now.AddHours(2),
                EndTime = DateTime.Now.AddHours(3),
                Source = "GoogleCalendar"
            };
            _calendar.AddEvent(testEvent);
            System.Windows.MessageBox.Show("Test event added!");
        });

        BtnCalendar.ContextMenu = menu;
        menu.PlacementTarget = BtnCalendar;
        menu.IsOpen = true;
    }

    private void ShowStudySessionMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        void Add(string header, Action onClick)
        {
            var mi = new System.Windows.Controls.MenuItem { Header = header };
            mi.Click += (_, _) => onClick();
            menu.Items.Add(mi);
        }

        var plans = _studyPlans.GetAllPlans();
        var activeSessions = plans.Where(p => p.Sessions.Count > 0).ToList();

        if (activeSessions.Count > 0)
        {
            var sessionsItem = new System.Windows.Controls.MenuItem { Header = "📚 Available Study Plans" };
            foreach (var plan in activeSessions.Take(8))
            {
                var planName = plan.Title;
                var sessionCount = plan.Sessions.Count;
                var submenu = new System.Windows.Controls.MenuItem { Header = $"{planName} ({sessionCount} sessions)" };
                
                foreach (var session in plan.Sessions.Take(5))
                {
                    var sessionLabel = $"{session.Subject} - {session.DurationMinutes}min";
                    var mi = new System.Windows.Controls.MenuItem { Header = sessionLabel };
                    mi.Click += (_, _) => StartStudySession(session.Subject);
                    submenu.Items.Add(mi);
                }
                sessionsItem.Items.Add(submenu);
            }
            menu.Items.Add(sessionsItem);
            menu.Items.Add(new System.Windows.Controls.Separator());
        }

        Add("Quick Study Session", () =>
        {
            var dlg = new Controls.InputDialog("Start Study Session", "Subject:", "Math");
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResultText))
            {
                StartStudySession(dlg.ResultText!);
            }
        });

        Add("View Session History", () =>
        {
            var historyWindow = new StudySessionHistoryWindow(_studyPlans) { Owner = this };
            historyWindow.ShowDialog();
        });

        BtnStudySession.ContextMenu = menu;
        menu.PlacementTarget = BtnStudySession;
        menu.IsOpen = true;
    }

    private void StartStudySession(string subject)
    {
        var sessionWindow = new StudySessionWindow(_studyPlans, subject) { Owner = this };
        sessionWindow.ShowDialog();
    }

    private void ShowTodosMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        void Add(string header, Action onClick)
        {
            var mi = new System.Windows.Controls.MenuItem { Header = header };
            mi.Click += (_, _) => onClick();
            menu.Items.Add(mi);
        }

        var activeTodos = _todos.GetActiveTodos();
        if (activeTodos.Count > 0)
        {
            var statsItem = new System.Windows.Controls.MenuItem 
            { 
                Header = _todos.GetStatsSummary(),
                IsEnabled = false
            };
            menu.Items.Add(statsItem);
            menu.Items.Add(new System.Windows.Controls.Separator());

            var todosItem = new System.Windows.Controls.MenuItem { Header = "Active Tasks" };
            foreach (var todo in activeTodos.Take(8))
            {
                var priority = todo.PriorityName;
                var daysLabel = todo.DaysDueIn() switch
                {
                    <= 0 => "Overdue",
                    1 => "Tomorrow",
                    var d => $"{d}d"
                };
                
                todosItem.Items.Add(new System.Windows.Controls.MenuItem 
                { 
                    Header = $"[{priority}] {todo.Title} ({daysLabel})",
                    IsEnabled = false
                });
            }
            menu.Items.Add(todosItem);
            menu.Items.Add(new System.Windows.Controls.Separator());
        }

        Add("Add Task", () =>
        {
            var dlg = new Controls.InputDialog("New Task", "Enter task name:", "");
            if (dlg.ShowDialog() == true)
            {
                var todo = new FocusDock.Data.Models.TodoItem 
                { 
                    Title = dlg.ResultText ?? "Untitled",
                    Priority = 2,
                    DueDate = DateTime.Now.AddDays(1)
                };
                _todos.AddTodo(todo);
                System.Windows.MessageBox.Show("Task added!");
            }
        });

        Add("View All Tasks", () =>
        {
            var sb = new System.Text.StringBuilder();
            var allTodos = _todos.GetAllTodos();
            sb.AppendLine($"Total: {allTodos.Count} tasks\n");
            
            var active = _todos.GetActiveTodos();
            sb.AppendLine($"Active: {active.Count}");
            foreach (var t in active.Take(5))
                sb.AppendLine($"  • [{t.PriorityName}] {t.Title}");
            
            System.Windows.MessageBox.Show(sb.ToString(), "Task Overview");
        });

        Add("Create Study Plan", () =>
        {
            var assignments = _calendar.GetUpcomingAssignments(14);
            if (assignments.Count == 0)
            {
                System.Windows.MessageBox.Show("No upcoming assignments. Add Canvas assignments first.", "No Data");
                return;
            }

            var dlg = new Controls.InputDialog("Study Plan", "Plan name:", "Midterm Review");
            if (dlg.ShowDialog() == true)
            {
                var plan = _studyPlans.CreatePlanFromAssignments(
                    dlg.ResultText ?? "Study Plan",
                    assignments,
                    assignments.Max(a => a.DueDate));
                System.Windows.MessageBox.Show($"Study plan created with {plan.Sessions.Count} sessions!", "Success");
            }
        });

        Add("Clear Completed", () =>
        {
            _todos.DeleteCompleted();
            System.Windows.MessageBox.Show("Completed tasks cleared!");
        });

        BtnReminders.ContextMenu = menu;
        menu.PlacementTarget = BtnReminders;
        menu.IsOpen = true;
    }

    private void ShowSettingsWindow()
    {
        var settingsWindow = new SettingsWindow(_calendar)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }
}

public class DockViewModel : ObservableObject
{
    private System.Collections.Generic.List<WindowGroup> _windowGroups = new();
    public System.Collections.Generic.List<WindowGroup> WindowGroups
    {
        get => _windowGroups; 
        set => SetProperty(ref _windowGroups, value);
    }

    private ObservableCollection<LayoutPreset> _savedPresets = new();
    public ObservableCollection<LayoutPreset> SavedPresets
    {
        get => _savedPresets;
        set => SetProperty(ref _savedPresets, value);
    }

    private string? _lastAppliedPresetName;
    public string? LastAppliedPresetName
    {
        get => _lastAppliedPresetName;
        set => SetProperty(ref _lastAppliedPresetName, value);
    }

    private bool _iconOnlyMode;
    public bool IconOnlyMode
    {
        get => _iconOnlyMode;
        set => SetProperty(ref _iconOnlyMode, value);
    }
}
