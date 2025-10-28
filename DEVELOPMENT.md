# FocusDock Development Guide

## Project Structure

```
FocusDeck/
├── src/
│   ├── FocusDock.System/           # Win32 interop + system hooks
│   │   ├── User32.cs              # P/Invoke declarations
│   │   ├── SessionMonitor.cs       # Lock/unlock detection
│   │   └── WindowTracker.cs        # Window enumeration
│   │
│   ├── FocusDock.Data/             # Data models + persistence
│   │   ├── Models/
│   │   │   ├── LayoutModels.cs     # LayoutPreset, LayoutZone
│   │   │   ├── WorkspaceModels.cs  # Workspace, WindowKey
│   │   │   ├── AppSettings.cs      # DockEdge, AppSettings
│   │   │   ├── AutomationModels.cs # TimeRule, RuleAction, AutomationConfig
│   │   │   └── WindowGroup.cs      # GroupedWindowInfo
│   │   ├── LocalStore.cs           # Preset JSON storage
│   │   ├── PresetService.cs        # Preset CRUD
│   │   ├── SettingsStore.cs        # Settings JSON storage
│   │   ├── WorkspaceStore.cs       # Workspace JSON storage
│   │   ├── PinsStore.cs            # Pin JSON storage
│   │   └── AutomationStore.cs      # Automation JSON storage
│   │
│   ├── FocusDock.Core/             # Business logic
│   │   ├── Services/
│   │   │   ├── DockStateManager.cs    # Dock expand/collapse
│   │   │   ├── LayoutManager.cs       # Apply layout zones to windows
│   │   │   ├── WorkspaceManager.cs    # Capture & restore workspaces
│   │   │   ├── PinService.cs          # Track pinned windows
│   │   │   ├── ReminderService.cs     # Stale window detection
│   │   │   ├── AutomationService.cs   # Time-based rule evaluation
│   │   │   └── ObservableObject.cs    # MVVM base
│   │
│   └── FocusDock.App/              # WPF UI
│       ├── MainWindow.xaml         # Dock UI layout
│       ├── MainWindow.xaml.cs      # Dock logic & menus
│       ├── App.xaml                # App config
│       ├── App.xaml.cs             # App startup
│       ├── Controls/
│       │   └── InputDialog.xaml    # Name prompt dialog
│       └── DockViewModel.cs        # Data binding (in MainWindow.xaml.cs)
│
├── README.md                       # Project vision
├── QUICKSTART.md                   # User guide
└── FocusDeck.sln                   # Visual Studio solution
```

## Dependency Flow

```
FocusDock.System (no dependencies)
    ↑
    ├← FocusDock.Data (depends on System)
    │
    ├← FocusDock.Core (depends on System + Data)
    │
    └← FocusDock.App (depends on System + Core + Data)
```

**Key Rule**: Data layer has NO business logic, only persistence. Core layer has business logic but NO UI or persistence calls. App layer is the only one that ties everything together.

---

## Adding New Features

### Example: Add a "Clear All Pins" button

1. **Add UI** (App layer)
   - Edit `MainWindow.xaml`: Add button in the Reminders menu
   - Edit `MainWindow.xaml.cs`: Add click handler

2. **Implement Logic** (Core layer)
   - If complex, add method to `PinService.cs`:
     ```csharp
     public void ClearAllPins()
     {
         _pinned.Clear();
         _pinnedKeys.Clear();
         FocusDock.Data.PinsStore.Save(_pinnedKeys);
         PinsChanged?.Invoke(this, EventArgs.Empty);
     }
     ```

3. **Persist** (Data layer)
   - Already handled by `PinsStore.Save()` called from Core

4. **Bind Data** (App layer)
   - Notify VM of changes via event
   - UI automatically updates via data binding

### Example: Add a new Automation Trigger (e.g., Network Detection)

1. **Add Model** (Data layer - `AutomationModels.cs`)
   ```csharp
   public enum NetworkTrigger
   {
       OnSSID,
       OffSSID
   }
   
   public class NetworkRule : TimeRule
   {
       public NetworkTrigger Trigger { get; set; }
       public string SSID { get; set; } = "";
   }
   ```

2. **Add Detector** (System layer - `NetworkMonitor.cs`)
   ```csharp
   public class NetworkMonitor
   {
       public event EventHandler<string>? SSIDChanged;
       public string GetCurrentSSID() { ... }
   }
   ```

3. **Integrate** (Core layer - `AutomationService.cs`)
   ```csharp
   private NetworkMonitor _net;
   
   public AutomationService()
   {
       _net = new NetworkMonitor();
       _net.SSIDChanged += (_, ssid) => EvaluateNetworkRules(ssid);
   }
   
   private void EvaluateNetworkRules(string currentSSID)
   {
       foreach (var rule in Config.NetworkRules)
       {
           if (rule.SSID == currentSSID)
               RuleTriggered?.Invoke(this, rule);
       }
   }
   ```

4. **Persist & UI** (Data + App layers)
   - Add UI to set network rules
   - `AutomationStore` auto-saves rules

---

## Testing Architecture

Current approach:
- Manual testing via running the app
- XAML IntelliSense in VS Code requires WPF extension
- Build validates all types and references

Future:
- Add xUnit test projects for services
- Mock System layer for testing Core logic

---

## Common Tasks

### Modify Pin Persistence
- Edit: `FocusDock.Core/Services/PinService.cs`
- Storage: `FocusDock.Data/PinsStore.cs`
- Loading: `MainWindow.xaml.cs` constructor

### Modify Dock UI
- Layout: `FocusDock.App/MainWindow.xaml`
- Logic: `FocusDock.App/MainWindow.xaml.cs`
- Styling: Update `DockPillStyle` in MainWindow.xaml

### Add New Preset Type
- Model: `FocusDock.Data/Models/LayoutModels.cs` (add static method)
- Persist: `FocusDock.Data/PresetService.cs` (auto-handled)
- UI: `FocusDock.App/MainWindow.xaml.cs` ShowLayoutsMenu() (add menu item)

### New Automation Rule Type
- Model: `FocusDock.Data/Models/AutomationModels.cs`
- Logic: `FocusDock.Core/Services/AutomationService.cs`
- UI: `FocusDock.App/MainWindow.xaml.cs` ShowAutomationsMenu()
- Persist: `FocusDock.Data/AutomationStore.cs`

---

## Build Tips

```powershell
# Clean build
dotnet clean
rm -r src/*/bin,src/*/obj
dotnet build

# Run tests (future)
dotnet test

# Publish (release)
dotnet publish -c Release -o ./publish src/FocusDock.App/FocusDock.App.csproj
```

---

## Debugging

1. **Visual Studio**: F5 to debug, set breakpoints
2. **Console Output**: Check `System.Diagnostics.Debug.WriteLine()` in Output window
3. **JSON Files**: Edit directly in `%LOCALAPPDATA%\FocusDock\*.json` for testing
4. **Window Enumeration**: Add debug prints in `WindowTracker.GetCurrentWindows()`

---

## Code Standards

- **Naming**: PascalCase for public members, _camelCase for private fields
- **Async**: Not currently used (could add for I/O operations)
- **Null**: Use `??` and `?.` operators, handle gracefully
- **Events**: Use `EventHandler<T>` pattern, invoke on Dispatcher thread from timers
- **Comments**: Add XML docs to public APIs, inline comments for complex logic

---

## Known Technical Debt

1. **MainWindow.xaml.cs**: ~550 lines, could split into multiple services
2. **SessionMonitor**: Partially implemented, needs window hook or power event registration
3. **LayoutManager**: No validation of zone overlap or edge cases
4. **Error Handling**: Minimal, should add logging infrastructure
5. **UI Threading**: Uses `Dispatcher.Invoke()` for all updates, could use SynchronizationContext

---

## Next Phase Architecture

Phase 2 should add:
- **Logger Interface** → Core + App layers use it
- **Settings UI** → Dedicated settings window
- **Note Editor** → SQLite backend + WPF RichTextBox
- **Calendar Sync** → Google API client
- **Study Mode** → Separate immersive window
- **Async/Await** → For API calls and I/O

---

Happy coding! 🚀
