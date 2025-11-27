# FocusDock - Quick Start & Feature Guide

## 🚀 Running the Application

```powershell
cd src/FocusDock.App
dotnet run
```

Or from the root:
```powershell
dotnet run --project src/FocusDock.App/FocusDock.App.csproj
```

## 📋 What's Implemented (Phase 1 MVP)

### ✅ Completed Features

1. **Auto-Collapsing Dock UI**
   - Dock sits at configured edge (top/bottom/left/right)
   - Collapses to 6px ribbon, expands on hover
   - Opacity animation for focus mode

2. **Window Tracking & Grouping**
   - Real-time window enumeration (1.5s poll)
   - Windows grouped by process name
   - Live updates when windows open/close

3. **Pin System with Persistence**
   - Click window to toggle pin (green border = pinned)
   - Pins saved to `%LOCALAPPDATA%/FocusDock/pins.json`
   - Auto-loaded on app startup
   - Pins persist across sessions ✅ NEW

4. **Layout System**
   - Two Column, Three Column, Grid 2x2 layouts
   - Manual apply via "Layouts" menu
   - Save custom presets with names
   - Multi-monitor aware (apply to specific monitor)

5. **Workspace System** ✅ ENHANCED
   - Save pinned window state as workspace
   - Restore workspace to re-pin windows
   - **NEW: Auto-restore last workspace on startup**
   - **NEW: "Park Today" quick-save feature (saves as workspace + layout)**
   - Workspaces stored in `%LOCALAPPDATA%/FocusDock/workspaces.json`

6. **Reminder AI**
   - Detects stale/unused windows (20+ min no interaction)
   - Shows count in dock
   - Click "Reminders" for management options

7. **Time-Based Automations**
   - Set rules: "If Mon-Fri 9-5, apply Layout X" or "Enable Focus Mode"
   - 2-second preview before applying
   - Undo button if you didn't want it
   - Rules stored in `%LOCALAPPDATA%/FocusDock/automation.json`

8. **Dock Configuration**
   - Choose monitor and edge (Top/Bottom/Left/Right)
   - Settings saved to `%LOCALAPPDATA%/FocusDock/settings.json`

9. **Clock Display**
   - Current time in dock (updates every second)

### 🔧 Architecture

**FocusDock.System** - Win32 interop
- WindowTracker: Polls windows every 1.5s
- User32: P/Invoke for window management
- SessionMonitor: Hook for lock/unlock events (infrastructure ready)

**FocusDock.Data** - Persistence
- JSON-based storage (Presets, Workspaces, Pins, Settings, Automations)
- Automatic save on every change

**FocusDock.Core** - Business Logic
- LayoutManager: Applies zones to windows
- DockStateManager: Expand/collapse/focus mode
- ReminderService: Stale window detection
- PinService: Track pinned windows
- WorkspaceManager: Capture & restore pin state
- AutomationService: Time-based rule evaluation

**FocusDock.App** - WPF UI
- MainWindow: Dock UI with collapsible ribbon
- Menus for Layouts, Workspaces, Automations, Reminders, Dock config
- DockViewModel: Data binding for window groups

---

## 📝 Usage Guide

### Dock Basics
1. **Expand/Collapse**: Hover over dock or click it
2. **Click Window Chip**: Toggle pin on/off (pins highlight in green)
3. **Focus Mode**: Click "Focus" button to toggle

### Layouts
1. Go to **Layouts** menu
2. Select monitor
3. Choose: Two Column, Three Column, or Grid 2x2
4. Name your preset (optional)
5. **NEW**: Apply to all future saves

### Workspaces
1. Arrange your windows and pin what you want
2. Go to **Workspace** → **Save Workspace As...**
3. Name it (e.g., "Bible Class Layout")
4. Later: Go to **Workspace** → **Restore: [Name]** to restore
5. **NEW**: Click **Park Today** to quick-save current state for tomorrow

### Automations
1. Go to **Automations** → **Add Example Rule**
2. Set days of week, times, and action
3. When the rule triggers, you get a 2-second preview
4. Click **Undo** if you don't want it to apply

### Reminders
1. Go to **Reminders** menu (shows count if any stale windows)
2. Options to close, snooze, or save stale windows

### Dock Settings
1. Go to **Dock** menu
2. Choose Monitor and Edge (Top/Bottom/Left/Right)
3. Settings auto-save

---

## 🗂️ Data Storage

All data stored in: `%LOCALAPPDATA%\FocusDock\`

- `presets.json` - Layout presets
- `workspaces.json` - Saved workspaces (pinned state + optional layout)
- `pins.json` - Pinned window handles/keys
- `automation.json` - Automation rules
- `settings.json` - Dock position, monitor, edge

---

## 🎯 Next Phase Features (Not in MVP)

- **Calendar Integration** (Google Calendar, Canvas)
- **To-Do System** (unified task list with automations)
- **Note-Taking** (rich editor, course-based organization)
- **Audio Recording** (lecture capture with timestamps)
- **Flashcards** (spaced repetition study)
- **Study Mode** (immersive workspace for focused learning)
- **Android Companion App** (remote dock control, notifications)
- **Network Detection** (Wi-Fi SSID triggers)
- **Visual Themes** (light/dark modes, customizable colors)

---

## 🐛 Known Limitations

1. **Pin Restore**: Restores pin state but windows must be open
2. **Vertical Dock**: Works but UI could use more polish
3. **SessionMonitor**: Partially implemented (lock/unlock detection ready but not integrated)
4. **Single Focus**: No multi-workspace view switching yet

---

## 📊 Build & Test

```powershell
# Build
dotnet build

# Run
dotnet run --project src/FocusDock.App/FocusDock.App.csproj

# Clean
dotnet clean
rm -Recurse src/*/bin,src/*/obj
```

---

## 💡 Tips

1. **Keep it simple**: Start with one layout and one workspace
2. **Auto-restore**: App automatically restores "Park Today" on next launch
3. **Undo automations**: Automation preview gives you 2 seconds to undo
4. **No conflicts**: Pinning a window doesn't affect closing/minimizing it
5. **JSON direct edit**: Advanced users can edit JSON files directly (app reloads on restart)

---

Enjoy FocusDock! 🎯
