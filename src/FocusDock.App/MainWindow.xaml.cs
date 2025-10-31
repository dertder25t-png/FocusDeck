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
using Microsoft.Extensions.DependencyInjection;

namespace FocusDock.App;

public partial class MainWindow : Window
{
    private readonly DockStateManager _dockState;
    private readonly WindowTracker _windowTracker;
    private readonly LayoutManager _layoutManager;
    private readonly System.Timers.Timer _clockTimer = new(1000);
    private readonly ReminderService _reminder;
    private readonly PinService _pins;
    private readonly WorkspaceManager _workspaces;
    private readonly CalendarService _calendar;
    private readonly TodoService _todos;
    private readonly NotesService _notes;
    private readonly StudyPlanService _studyPlans;
    private AppSettings _settings;
    private AutomationService _automation;
    private System.Windows.Threading.DispatcherTimer _automationPreviewTimer = new();
    private TimeRule? _automationPendingRule;
    private int _automationCountdown = 0;
    // Placeholder panels to satisfy legacy references when running with modern XAML that doesn't define these named elements.
    private StackPanel PanelLeft = new();
    private StackPanel PanelRight = new();
    private PlannerWindow? _plannerWindow = null;

    public DockViewModel VM { get; } = new();

    public MainWindow(
        FocusDock.SystemInterop.WindowTracker windowTracker,
        LayoutManager layoutManager,
        PinService pinService,
        ReminderService reminderService,
        WorkspaceManager workspaceManager,
        CalendarService calendarService,
        TodoService todoService,
        NotesService notesService,
        StudyPlanService studyPlanService,
        AutomationService automationService)
    {
        InitializeComponent();

        // Optimize process priority for minimal resource usage
        try
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            process.PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
        }
        catch { /* Ignore if unable to set priority */ }

        DataContext = VM;

        // Inject dependencies
        _windowTracker = windowTracker;
        _layoutManager = layoutManager;
        _pins = pinService;
        _reminder = reminderService;
        _workspaces = workspaceManager;
        _calendar = calendarService;
        _todos = todoService;
        _notes = notesService;
        _studyPlans = studyPlanService;
        _automation = automationService;
        
        _dockState = new DockStateManager(this);
        _settings = SettingsStore.LoadSettings();

        _windowTracker.WindowsUpdated += (s, e) =>
        {
            // Filter out FocusDock itself from the window list
            var filteredWindows = e.Where(w => 
                !w.ProcessName.Equals("FocusDock", StringComparison.OrdinalIgnoreCase) &&
                !w.ProcessName.Equals("FocusDock.App", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            var groups = filteredWindows.GroupBy(w => w.ProcessName)
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
            _reminder.UpdateSeen(filteredWindows);
            
            // Use BeginInvoke with Background priority for better performance
            Dispatcher.BeginInvoke(new Action(() => 
            {
                VM.WindowGroups = groups;
                UpdateDockWidth();
                UpdateDockPosition();
            }), System.Windows.Threading.DispatcherPriority.Background);
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

        // Wire up mouse events to DockStateManager
        MouseEnter += (_, _) => _dockState.OnMouseEnter();
        MouseLeave += (_, _) => _dockState.OnMouseLeave();

        BtnFocus.Click += (_, _) => _dockState.ToggleFocusMode();
        BtnDock.Click += (_, _) => ShowDockMenu();
        BtnCalendar.Click += (_, _) => ShowPlannerWindow();
        BtnReminders.Click += (_, _) => ShowPlannerWindow();
        BtnNotes.Click += (_, _) => ShowNotesMenu();
        BtnStudySession.Click += (_, _) => ShowStudySessionMenu();
        BtnWorkspace.Click += (_, _) => ShowWorkspaceMenu();
        BtnAutomations.Click += (_, _) => ShowAutomationsMenu();
        BtnSettings.Click += (_, _) => ShowSettingsWindow();
        BtnLayouts.Click += (_, _) =>
        {
            ShowLayoutsMenu();
        };

        _clockTimer.Elapsed += (_, _) =>
        {
            // Only update if window is visible to save resources
            if (!IsVisible) return;
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtClock.Text = DateTime.Now.ToString("h:mm tt");
            }), System.Windows.Threading.DispatcherPriority.Background);
        };
        _clockTimer.Start();

        // Automation (already injected)
        _automation.Config = AutomationStore.Load();
        _automation.RuleTriggered += OnAutomationRuleTriggered;
        _automation.Start();

        _automationPreviewTimer.Interval = TimeSpan.FromSeconds(1);
        _automationPreviewTimer.Tick += OnAutomationPreviewTick;
        
        // Auto-restore last workspace on startup
        Loaded += (_, _) =>
        {
            // Configure dock state for auto-hide
            _dockState.SetEdge(_settings.Edge);
            _dockState.SetAutoHide(true);
            
            Top = -77; // Start hidden
            UpdateDockWidth();
            UpdateDockPosition();
            RestoreLastWorkspace();
            
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
        // Temporarily comment out PositionDock to keep window centered and visible for UI development
        // PositionDock();
        
        // Force expanded state for visibility during development
        _dockState.Expand();
        Opacity = 1.0;
    }

    private void PositionDock()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        var index = _settings.MonitorIndex >= 0 && _settings.MonitorIndex < screens.Length ? _settings.MonitorIndex : 0;
        var screen = screens[index];
        if (screen is null) return;
        var area = screen.WorkingArea;
        
        // Inform DockStateManager about the edge
        _dockState.SetEdge(_settings.Edge);
        
        bool vertical = _settings.Edge == DockEdge.Left || _settings.Edge == DockEdge.Right;
        
        switch (_settings.Edge)
        {
            case DockEdge.Top:
                Left = area.Left + 10; 
                Width = area.Width - 20; 
                Height = _dockState.ExpandedHeight; 
                Top = area.Top; 
                break;
            case DockEdge.Bottom:
                Left = area.Left + 10; 
                Width = area.Width - 20; 
                Height = _dockState.ExpandedHeight; 
                Top = area.Bottom - _dockState.CollapsedHeight; 
                break;
            case DockEdge.Left:
                Left = area.Left + 6; 
                Width = 220; 
                Height = area.Height - 20; 
                Top = area.Top + 10;
                
                // Switch to vertical layout
                try
                {
                    var panel = (ItemsPanelTemplate)FindResource("VerticalItemsPanel");
                    if (GroupsList != null)
                    {
                        GroupsList.ItemsPanel = panel;
                    }
                }
                catch { /* Resource might not exist yet */ }
                break;
                
            case DockEdge.Right:
                Left = area.Right - 220 - 6; 
                Width = 220; 
                Height = area.Height - 20; 
                Top = area.Top + 10;
                
                // Switch to vertical layout
                try
                {
                    var panel = (ItemsPanelTemplate)FindResource("VerticalItemsPanel");
                    if (GroupsList != null)
                    {
                        GroupsList.ItemsPanel = panel;
                    }
                }
                catch { /* Resource might not exist yet */ }
                break;
        }
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
        var current = _windowTracker.GetCurrentWindows();
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
        var window = new AutomationsWindow();
        window.Owner = this;
        window.ShowDialog();
        
        // Reload automation config after dialog closes
        _automation.Config = AutomationStore.Load();
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
        
        // Apply premium glassmorphism style
        menu.Style = (Style)FindResource("PremiumContextMenu");

        void AddMenuItem(string header, Action onClick)
        {
            var mi = new System.Windows.Controls.MenuItem { Header = header };
            mi.Style = (Style)FindResource("PremiumMenuItem");
            mi.Click += (_, _) => onClick();
            menu.Items.Add(mi);
        }

        foreach (var scr in screens)
        {
            var idx = Array.IndexOf(screens, scr) + 1;
            var sub = new System.Windows.Controls.MenuItem { Header = $"Monitor {idx}" };
            sub.Style = (Style)FindResource("PremiumMenuItem");
            void Add(string header, Func<int, int, LayoutPreset> factory)
            {
                var mi = new System.Windows.Controls.MenuItem { Header = header };
                mi.Style = (Style)FindResource("PremiumMenuItem");
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

    private void ShowPlannerWindow()
    {
        // Check if window already exists and is open
        if (_plannerWindow != null)
        {
            // If window is already open, just activate it
            if (_plannerWindow.IsLoaded && _plannerWindow.IsVisible)
            {
                _plannerWindow.Activate();
                _plannerWindow.Focus();
                return;
            }
        }
        
        // Create new window if needed
        _plannerWindow = ((App)System.Windows.Application.Current).Services.GetRequiredService<PlannerWindow>();
        _plannerWindow.Owner = this;
        
        // Handle window closing to clean up reference
        _plannerWindow.Closed += (s, e) => _plannerWindow = null;
        
        _plannerWindow.Show();
    }

    private void ShowCalendarMenu()
    {
        // Legacy method - redirects to ShowPlannerWindow
        ShowPlannerWindow();
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

    private void ShowTasksWindow()
    {
        // Legacy method - redirects to ShowPlannerWindow
        ShowPlannerWindow();
    }

    private void ShowTodosMenu()
    {
        // Legacy method - redirects to ShowPlannerWindow
        ShowPlannerWindow();
    }

    private void ShowNotesMenu()
    {
        var notesWindow = new NotesWindow(_notes)
        {
            Owner = this
        };
        notesWindow.Show();
    }

    private void ShowSettingsWindow()
    {
        var settingsWindow = new SettingsWindow(_calendar)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }
    
    // ═════════════════════════════════════════════════════════════════════════
    // EXPANDABLE TOOLBAR HANDLERS
    // ═════════════════════════════════════════════════════════════════════════
    
    private System.Windows.Media.Animation.Storyboard? _expandStoryboard;
    private System.Windows.Media.Animation.Storyboard? _collapseStoryboard;
    private System.Windows.Threading.DispatcherTimer? _collapseTimer;
    
    internal void ExpandToolbar(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Cancel any pending collapse
        _collapseTimer?.Stop();
        _collapseStoryboard?.Stop();
        
        if (_expandStoryboard == null)
        {
            _expandStoryboard = new System.Windows.Media.Animation.Storyboard();
            
            // Expand MaxWidth animation
            var widthAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 520,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            System.Windows.Media.Animation.Storyboard.SetTargetName(widthAnim, "ToolsPanel");
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(widthAnim, new PropertyPath(FrameworkElement.MaxWidthProperty));
            
            // Fade in animation
            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            System.Windows.Media.Animation.Storyboard.SetTargetName(opacityAnim, "ToolsPanel");
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));
            
            _expandStoryboard.Children.Add(widthAnim);
            _expandStoryboard.Children.Add(opacityAnim);
        }
        
        _expandStoryboard.Begin(this);
    }
    
    internal void CollapseToolbar(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Use a timer to delay collapse check - prevents premature closing
        if (_collapseTimer == null)
        {
            _collapseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _collapseTimer.Tick += (s, args) =>
            {
                _collapseTimer.Stop();
                
                // Only collapse if mouse is truly outside both button and panel
                var toolbarButton = this.FindName("BtnToolbar") as System.Windows.Controls.Button;
                var toolsPanel = this.FindName("ToolsPanel") as StackPanel;
                
                bool mouseOverButton = toolbarButton != null && toolbarButton.IsMouseOver;
                bool mouseOverPanel = toolsPanel != null && toolsPanel.IsMouseOver;
                
                if (!mouseOverButton && !mouseOverPanel)
                {
                    _expandStoryboard?.Stop();
                    
                    if (_collapseStoryboard == null)
                    {
                        _collapseStoryboard = new System.Windows.Media.Animation.Storyboard();
                        
                        var widthAnim = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            To = 0,
                            Duration = TimeSpan.FromMilliseconds(200),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                        };
                        System.Windows.Media.Animation.Storyboard.SetTargetName(widthAnim, "ToolsPanel");
                        System.Windows.Media.Animation.Storyboard.SetTargetProperty(widthAnim, new PropertyPath(FrameworkElement.MaxWidthProperty));
                        
                        var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            To = 0,
                            Duration = TimeSpan.FromMilliseconds(200),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                        };
                        System.Windows.Media.Animation.Storyboard.SetTargetName(opacityAnim, "ToolsPanel");
                        System.Windows.Media.Animation.Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));
                        
                        _collapseStoryboard.Children.Add(widthAnim);
                        _collapseStoryboard.Children.Add(opacityAnim);
                    }
                    
                    _collapseStoryboard.Begin(this);
                }
            };
        }
        
        _collapseTimer.Start();
    }

    // Mouse event handlers for dock show/hide
    private void OnDockMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _dockState.OnMouseEnter();
    }

    private void OnDockMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _dockState.OnMouseLeave();
    }
    
    private void UpdateDockWidth()
    {
        // Calculate dynamic width based on content
        int windowCount = VM.WindowGroups?.Sum(g => g.Windows?.Count ?? 0) ?? 0;
        
        // Base width: Left section (60) + Right toolbar (~250) + Margins (~50)
        double baseWidth = 360;
        
        // Add width per window chip (approximately 50px per chip)
        double chipWidth = 50;
        double contentWidth = baseWidth + (windowCount * chipWidth);
        
        // Clamp between min and max
        double minWidth = 400;
        double maxWidth = SystemParameters.WorkArea.Width * 0.8; // Max 80% of screen width
        
        Width = Math.Max(minWidth, Math.Min(contentWidth, maxWidth));
    }
    
    private void UpdateDockPosition()
    {
        var workArea = SystemParameters.WorkArea;
        
        // Apply alignment setting
        switch (_settings.Alignment)
        {
            case DockAlignment.Left:
                Left = workArea.Left + 20;
                break;
            case DockAlignment.Right:
                Left = workArea.Right - Width - 20;
                break;
            case DockAlignment.Center:
            default:
                Left = workArea.Left + (workArea.Width - Width) / 2;
                break;
        }
    }
    
    public void ApplyPositionSettings(AppSettings settings)
    {
        _settings = settings;
        UpdateDockWidth();
        UpdateDockPosition();
    }
    
    private void OnWindowClick(object sender, MouseButtonEventArgs e)
    {
        // Check if the click is outside the toolbar area
        var toolbarButton = this.FindName("BtnToolbar") as System.Windows.Controls.Button;
        var toolsPanel = this.FindName("ToolsPanel") as StackPanel;
        
        bool clickOnButton = toolbarButton != null && toolbarButton.IsMouseOver;
        bool clickOnPanel = toolsPanel != null && toolsPanel.IsMouseOver;
        
        // If clicked outside toolbar, force it closed immediately
        if (!clickOnButton && !clickOnPanel)
        {
            _collapseTimer?.Stop();
            _expandStoryboard?.Stop();
            
            if (_collapseStoryboard == null)
            {
                _collapseStoryboard = new System.Windows.Media.Animation.Storyboard();
                
                var widthAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                };
                System.Windows.Media.Animation.Storyboard.SetTargetName(widthAnim, "ToolsPanel");
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(widthAnim, new PropertyPath(FrameworkElement.MaxWidthProperty));
                
                var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                };
                System.Windows.Media.Animation.Storyboard.SetTargetName(opacityAnim, "ToolsPanel");
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));
                
                _collapseStoryboard.Children.Add(widthAnim);
                _collapseStoryboard.Children.Add(opacityAnim);
            }
            
            _collapseStoryboard.Begin(this);
        }
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

    private bool _iconOnlyMode = true; // Default to icon-only mode
    public bool IconOnlyMode
    {
        get => _iconOnlyMode;
        set => SetProperty(ref _iconOnlyMode, value);
    }
}




