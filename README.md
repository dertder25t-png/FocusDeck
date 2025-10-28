# FocusDeck 🎯# FocusDeck - Focus Session Management System



**A Smart Focus Session Management System for Windows Desktop & Android Mobile****Smart study timer app with cloud synchronization for desktop (Windows 10+) and mobile (Android)**



FocusDeck is a cross-platform productivity suite that combines smart study timers, session tracking, and cloud synchronization. Focus on what matters while we handle the rest.A cross-platform focus management system with study timers, session tracking, and cloud sync infrastructure.



---## Quick Start



## 🌟 What is FocusDeck?### 📥 Download & Install



FocusDeck is designed to help you maximize productivity through:**Desktop (Windows 10+):**

- **Intelligent Study Timers** - Configurable Pomodoro-style sessions with progress tracking- [GitHub Releases](https://github.com/dertder25t-png/FocusDeck/releases) → Download `FocusDeck-Desktop-v*.zip`

- **Session Analytics** - See your study patterns, focus times, and productivity trends- Extract and run `FocusDeck.exe`

- **Cloud Sync** - Seamlessly sync sessions across your Windows PC and Android phone

- **Window Management** (Desktop) - Auto-organize application windows into layouts**Mobile (Android 8+):**

- **Cross-Platform** - Work on desktop, continue on mobile, and vice versa- [GitHub Releases](https://github.com/dertder25t-png/FocusDeck/releases) → Download `FocusDeck-Mobile-v*.apk`

- Install via ADB or direct download

---

**Server (Linux):**

## 🚀 Quick Start```bash

sudo bash <(curl -fsSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/setup-server.sh)

### Installation```



#### Windows Desktop---

1. Visit [GitHub Releases](https://github.com/dertder25t-png/FocusDeck/releases)

2. Download the latest `FocusDeck-Desktop-*.zip` file## 📊 Project Status

3. Extract the ZIP file

4. Run `FocusDeck.exe`| Component | Phase | Status | Build |

|-----------|-------|--------|-------|

**System Requirements:**| **Desktop (WPF)** | 1-5 | ✅ Complete | 0 Errors |

- Windows 10 or later (version 19041 or higher)| **Mobile (MAUI)** | 6b | ⏳ Week 1 Done | 0 Errors |

- .NET 8.0 Runtime (included with installer)| **Server (ASP.NET Core)** | 6a | ✅ Complete | Ready |

| **Cloud Sync** | 6a | ✅ Complete | OAuth2 Ready |

#### Android Mobile

1. Visit [GitHub Releases](https://github.com/dertder25t-png/FocusDeck/releases)**Current:** Phase 6b Week 2 - Study Timer Page Implementation

2. Download the latest `FocusDeck-Mobile-*.apk` file

3. Enable "Install from Unknown Sources" in your device settings---

4. Open the APK file and tap "Install"

## Phase 1 Features ✅

**System Requirements:**

- Android 8.0 (API 21) or later- Auto-collapsing dock UI (configurable edge: top/bottom/left/right)

- 50MB free storage- Real-time window tracking via Win32 P/Invoke

- Windows grouped by process name with live updates

#### Linux Server (Cloud Sync Backend)- **Pin system with persistent storage** ✅

```bash- Layout templates: Two-Column, Three-Column, Grid 2x2

sudo bash <(curl -fsSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/setup-server.sh)- Save & apply named layout presets

```- Multi-monitor aware (apply layouts per monitor)

- **Workspace system with auto-restore** ✅

**System Requirements:**- **"Park Today" feature for end-of-day save** ✅

- Ubuntu 22.04+ or Debian 12+- Time-based automations with 2-second preview + undo

- Proxmox VM or dedicated Linux server- Stale window detection and reminders

- 2GB RAM, 10GB storage- Dock position management (monitor + edge selection)

- Clock display

---- Full JSON persistence for all settings



## 📋 Features---



### ✅ Phase 1-5: Desktop Features (Complete)## Phase 2 Features 🎓 **NEW!**

- **Window Auto-Organize** - Group and arrange windows by application

- **Layout Templates** - Save and reuse layouts (2-column, 3-column, grid)### Calendar Integration 📅

- **Workspace Manager** - Switch between saved desktop configurations- **Google Calendar API support** (ready for setup)

- **Pin System** - Keep important windows persistent across layouts- **Canvas LMS assignment tracking** (ready for setup)

- **Multi-Monitor Support** - Manage different layouts per monitor- Upcoming events/assignments display in dock

- **Time-Based Automation** - Schedule automatic window arrangements- Manual event creation and persistence

- **"Park Today" Feature** - Save desktop state for tomorrow- Automatic sync every 15 minutes (configurable)

- Events can trigger workspace/layout changes

### 🔄 Phase 6a: Cloud Infrastructure (Complete)

- OAuth2 authentication ready### To-Do List Management ✓

- OneDrive integration prepared- Create, prioritize, and complete tasks

- Google Drive integration prepared- Priority levels: Low, Medium, High, Urgent

- Offline-first architecture scaffolded- Due date tracking with overdue alerts

- Canvas assignment sync (auto-create tasks)

### 🟢 Phase 6b: Mobile Features (In Progress)- Quick task statistics: "5/12 completed • 3 active • 1 overdue"

- Bulk operations (clear completed, filter by tag)

#### Week 1 - MAUI Foundation ✅- Full task persistence to JSON

- MAUI project created with proper platform targeting

- 4-tab navigation shell (Study, History, Analytics, Settings)### AI Study Planner 📚

- Dependency injection configured- Auto-generate study plans from assignments

- Android build pipeline established- Distribute study hours across available days

- Create timed study sessions with Pomodoro recommendations

#### Week 2 - Study Timer Page 🔄- Track study session history with effectiveness ratings

- **StudyTimerViewModel** ✅- Productivity summary: "8 sessions • 12.5h • 4.2/5 effectiveness"

  - State machine (Stopped, Running, Paused)- Generate plans in <500ms

  - 8 commands (Start, Pause, Stop, Reset, SetCustomTime, Set 15/25/45/60 min)

  - Observable properties with automatic change notifications### New UI Elements

  - Haptic feedback on completion- 📅 Calendar button - shows events + assignments

  - ✓ To-Do button - shows tasks + options

- **StudyTimerPage UI** ✅- Both accessible via dropdown menus

  - Large 300px timer display with MM:SS format- Integrated with existing dock menu system

  - Control buttons (Start, Pause, Stop, Reset)

  - Preset time buttons (Pomodoro patterns)---

  - Custom time input with validation

  - Session notes field## Phase 3 Features ✅ Complete

  - Progress bar with percentage display

  - Formatted elapsed/remaining time display### Google Calendar Integration ✅

  - Event-driven toast notifications- ✅ **OAuth2 provider** - Complete flow with token refresh

- ✅ **API client** - Fetch events, handle responses

#### Week 3 - Database & Sync Prep (Next)- ✅ **Settings window integration** - Configured UI with tabs

- SQLite database schema- ✅ **Interactive authorization flow** - Ready for user setup

- Local sync queue- ⏳ 15-minute auto-sync with real events (waiting for credentials)

- OAuth2 integration prep- ⏳ Event-based layout triggers (Phase 3 Future)



#### Week 4 - Cloud Sync Integration### Canvas LMS Integration ✅

- OneDrive & Google Drive sync- ✅ **Canvas API provider** - Complete course/assignment fetching

- Conflict resolution- ✅ **Connection testing** - Verify tokens and URLs

- Upload/download/delete operations- ✅ **Settings window integration** - Configured UI

- ⏳ Auto-create tasks from real assignments (Phase 3 Future)

#### Week 5 - Final Pages & Release- ⏳ Due date reminders (Phase 3 Future)

- Session History page- ⏳ Submission status tracking (Phase 3 Future)

- Analytics dashboard

- Settings page### Study Session UI ✅ **NEW IN PHASE 4a**

- App store preparation- ✅ **Real-time session timer** - Displays in MM:SS:SS format with 500ms updates

- ✅ **Active study tracking** - Progress bar with 60-minute session goal

---- ✅ **Break reminders** - Pomodoro-style alert at 25 minutes

- ✅ **Effectiveness rating popup** - 1-5 star rating dialog after session end

## 🏗️ Architecture- ✅ **Session logging** - Persists to JSON with timestamp, subject, breaks, rating



### Project Structure### Completed Phase 3 Infrastructure ✅

```- ✅ **GoogleCalendarProvider.cs** (280 lines)

FocusDeck/  - OAuth2 authorization URL generation

├── src/  - Token exchange and refresh

│   ├── FocusDock.App/              # Windows Desktop (WPF)  - Calendar event fetching (30-day lookhead)

│   │   ├── Controls/               # XAML User Controls  - Proper error handling

│   │   ├── Views/                  # Application Windows

│   │   └── Services/               # Business Logic- ✅ **CanvasApiProvider.cs** (200+ lines)

│   │  - Canvas API authentication

│   ├── FocusDock.Core/             # Desktop Core Services  - Course and assignment fetching

│   │   ├── Models/                 # Data Models  - Connection testing

│   │   ├── Services/               # Shared Services  - Submission status detection

│   │   └── Managers/               # State Management

│   │- ✅ **Settings Window** (XAML + C# codebehind)

│   ├── FocusDock.System/           # Windows System APIs  - 📅 Calendar tab: Google OAuth setup + Canvas config

│   │   └── User32.cs              # Win32 P/Invoke  - ✓ Tasks tab: Import options

│   │  - 📚 Study tab: Session preferences

│   └── FocusDeck.Mobile/           # Android/Mobile (MAUI)  - ℹ About tab: Version + resources

│       ├── Pages/                  # XAML Pages  - Settings button (⚙) integrated into dock

│       ├── ViewModels/             # MVVM ViewModels

│       ├── Services/               # Mobile Services- ✅ **CalendarService integration**

│       ├── Converters/             # Value Converters  - Automatic provider instantiation with saved credentials

│       └── Platforms/              # Platform-Specific Code  - Token management

│  - Error resilience for failed syncs

├── docs/                           # Documentation

├── .github/workflows/              # GitHub Actions CI/CD### Setup Guide

└── setup-server.sh                 # Linux Server Setup- 📖 **API_SETUP_GUIDE.md** - Step-by-step instructions

  - Google Cloud Console project creation

```  - Canvas API token generation

  - FocusDeck configuration

### Technology Stack  - Troubleshooting section

  - Privacy & security notes

| Layer | Technology | Purpose |

|-------|-----------|---------|---

| **Desktop** | WPF + C# 12 | Windows 10+ UI |

| **Mobile** | .NET MAUI | Cross-platform UI |## Phase 4 Features ✅ COMPLETE - Study Session & Productivity Tracking

| **Mobile VM** | MVVM Toolkit | Reactive bindings |

| **Core** | .NET 8.0 | Cross-platform base |### Phase 4a: Study Session UI ✅

| **Mobile Data** | SQLite | Offline storage |

| **Cloud** | ASP.NET Core 8 | Sync API |#### Study Session Window

| **Auth** | OAuth2 | Cloud authentication |- ✅ **Dark-themed timer UI** - Visual consistency with FocusDock aesthetic

- ✅ **Header with subject display** - Shows study topic (blue accent)

### MVVM Pattern (Mobile)- ✅ **Large timer display** - Real-time elapsed time (HH:MM:SS) updating every 500ms

- ✅ **Progress tracking** - Visual bar showing session progress (0-60 minutes target)

```csharp- ✅ **Control buttons**

StudyTimerViewModel (ObservableObject)  - Play/Pause toggle (⏸/▶) - Pause timer without ending session

├── [ObservableProperty] TotalTime, ElapsedTime, CurrentState  - Break button - Increment break counter manually

├── [RelayCommand] Start(), Pause(), Stop(), Reset()  - End Session button - Finish and rate effectiveness

├── [RelayCommand] Set15/25/45/60Minutes()- ✅ **Session stats footer**

├── Computed Properties: DisplayTime, ProgressPercentage, RemainingTime  - Focus rate percentage (active study time)

└── Events: TimerCompleted, MessageChanged  - Break count (auto-incremented)

  - Session stats display

StudyTimerPage (UI Layer)

└── Binds to ViewModel#### Pomodoro Integration

    └── Two-way binding: Entry ↔ MinutesInput- ✅ **25-minute break reminder** - Alert banner appears at 25-minute mark

    └── One-way binding: Label ← DisplayTime- ✅ **Break tracking** - Automatically counts breaks taken during session

    └── Command binding: Button → StartCommand- ✅ **Session pause** - Paused time doesn't count toward effective study time

```- ✅ **Configurable reminder** - BreakReminderMinutes constant (customizable per session)



---#### Effectiveness Rating System

- ✅ **Post-session rating dialog** - 1-5 star rating popup after session ends

## 📖 Developer Guide- ✅ **Session metadata collection**

  - Session ID (GUID for unique identification)

### Local Development Setup  - Subject (study topic)

  - Total duration (in minutes)

#### Prerequisites  - Breaks taken (count)

- .NET 8.0 SDK ([Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))  - Effectiveness rating (1-5 stars)

- Visual Studio 2022 (Community is free) or VS Code  - Optional notes field

- Git- ✅ **Persistent session logging**

- Windows 10+ for desktop development  - Saved to JSON via StudyPlanService.EndSession()

- Android SDK (if building for mobile)  - Includes timestamp and all session data

  - Queryable for history and analytics

#### Clone & Setup

```bash#### Integration with Main Window

git clone https://github.com/dertder25t-png/FocusDeck.git- ✅ **⏱ Study Session button** - New button added to dock UI

cd FocusDeck- ✅ **Study Plans menu** - Shows available study plans from earlier creation

- ✅ **Session list** - Browse and start sessions from existing plans

# Restore dependencies- ✅ **Quick start** - Manually enter subject for ad-hoc study sessions

dotnet restore

### Phase 4b: Study Session History Dashboard ✅

# Build all projects

dotnet build#### Session History Window

```- ✅ **Date range filtering** - Select "From" and "To" dates for custom ranges

- ✅ **Filter button** - Apply date range and refresh session list

#### Build Desktop App- ✅ **Complete session list display**

```bash  - Subject name with emoji indicator (📚)

cd src/FocusDock.App  - Date/time completed

dotnet build -c Release  - Duration in minutes

```  - Effectiveness rating (1-5)

  - Breaks taken count

#### Build Mobile App  - Organized by most recent first

```bash

cd src/FocusDeck.Mobile#### Statistics Strip (Real-time Updates)

dotnet build -c Release -f net8.0-android- ✅ **Total Sessions** - Count of all sessions in date range

```- ✅ **Total Hours** - Sum of all session time (formatted as X.Xh)

- ✅ **Avg Effectiveness** - Average 1-5 rating across sessions

#### Run Desktop App (Debug)- ✅ **Total Breaks** - Sum of breaks across all sessions

```bash

cd src/FocusDock.App#### Export Feature

dotnet run- ✅ **CSV Export** - Download session data for external analysis

```  - Columns: Subject, Date, Duration (min), Effectiveness, Breaks, Notes

  - Saved to Documents folder with timestamp

#### Run Mobile App (Android Emulator)  - Full session history preserved

```bash

cd src/FocusDeck.Mobile#### Integration

dotnet run -f net8.0-android- ✅ **Launch from Main Window** - "View Session History" menu option

```- ✅ **Accessible from context menus** - Quick access from dock buttons



### Project Files Reference### Phase 4c: Productivity Analytics Dashboard ✅



| File | Purpose |#### Summary Statistics

|------|---------|- ✅ **Total Study Time** - Aggregate hours studied in last 30 days

| `FocusDeck.sln` | Solution file - open in Visual Studio |- ✅ **Avg Daily Hours** - Total hours / unique study days

| `.github/workflows/build-desktop.yml` | CI/CD for Windows builds |- ✅ **Avg Effectiveness** - Average rating across all sessions

| `.github/workflows/build-mobile.yml` | CI/CD for Android builds |- ✅ **Most Studied Subject** - Subject with highest total minutes

| `setup-server.sh` | Automated Linux server setup |

#### Daily Study Pattern

### Adding New Features- ✅ **Daily breakdown** - Lists each day's study hours (last 30 days)

- ✅ **Format**: "M/d: X.Xh (XXXm)" for easy scanning

1. **Mobile UI Page:**- ✅ **Visual timeline** - Shows study habits and consistency

   ```bash

   # Create XAML page in src/FocusDeck.Mobile/Pages/#### Subject Breakdown Analysis

   # Create code-behind matching the .xaml.cs- ✅ **Subject statistics** - Shows minutes, percentage, and session count per subject

   # Create ViewModel in src/FocusDeck.Mobile/ViewModels/- ✅ **Ranked display** - Sorted by most studied to least studied

   ```- ✅ **Percentage calculation** - Helps visualize study focus areas

- ✅ **Session count** - Shows how many sessions per subject

2. **Desktop Feature:**

   ```bash#### Effectiveness Trend Tracking

   # Add to src/FocusDock.App/Views/ (UI)- ✅ **Weekly effectiveness** - Average rating per week for trend detection

   # Add logic to src/FocusDock.Core/Services/- ✅ **Trend visualization** - "Week 1: 4.2★ → Week 2: 4.5★" format

   ```- ✅ **Performance insights** - Identify if effectiveness is improving

- ✅ **Fallback message** - Alerts if no ratings recorded yet

3. **Update AppShell Navigation:**

   - Add new tab in `AppShell.xaml`#### Session Statistics

   - Register route and page- ✅ **Total sessions** - Count of all study sessions

- ✅ **Longest session** - Maximum session length in minutes

### Code Standards- ✅ **Avg session** - Mean session duration

- ✅ **Break patterns** - Total breaks and average breaks per session

- **C# 12** with nullable annotations (`#nullable enable`)

- **MVVM Pattern** for all UI (Toolkit.Mvvm)#### Launch Points

- **Async/Await** for all I/O operations- ✅ **From History Window** - "View Analytics" button opens dashboard

- **Comments** for public APIs and complex logic- ✅ **Modal dialog** - Opens as child of History window

- **XML Documentation** for classes and methods- ✅ **Real-time updates** - Pulls current data from session logs



---### Architecture & Implementation



## 🔧 Troubleshooting#### New Files Created (Phase 4)

1. **StudySessionWindow.xaml** (140 lines)

### Build Issues   - XAML UI with dark theme matching dock

   - Timer display, buttons, progress bar

**Error: "Unable to find .NET 8.0 SDK"**   - Session stats footer

```bash

# Download and install .NET 8.02. **StudySessionWindow.xaml.cs** (260+ lines)

# https://dotnet.microsoft.com/en-us/download/dotnet/8.0   - DispatcherTimer for real-time updates

   - Timer logic (play/pause/resume)

# Verify installation   - Break reminder at 25 minutes

dotnet --version  # Should show 8.x.x   - Effectiveness rating dialog

```   - Session persistence



**Error: "MAUI workload not installed"**3. **StudySessionHistoryWindow.xaml** (106 lines)

```bash   - Date range pickers

dotnet workload install maui   - Statistics strip

dotnet workload restore   - Session list display

```   - Export and analytics buttons



### Android Emulator Issues4. **StudySessionHistoryWindow.xaml.cs** (130 lines)

   - Date filtering logic

**APK Installation Fails**   - Statistics calculations

1. Ensure Android Emulator is running (`emulator -avd Pixel_5_API_31`)   - CSV export functionality

2. Check if ADB is accessible: `adb devices`   - Data binding for list display

3. Install manually: `adb install FocusDeck.Mobile-debug.apk`

5. **ProductivityAnalyticsWindow.xaml** (86 lines)

**Haptic Feedback Not Working**   - Analytics dashboard layout

- Emulator haptics are unsupported   - Statistics display areas

- Test on physical Android device   - Breakdown sections

- All haptic code is try-catch wrapped

6. **ProductivityAnalyticsWindow.xaml.cs** (138 lines)

### Desktop App Issues   - Analytics calculations

   - Weekly trend computation

**Window Doesn't Appear**   - Subject breakdown analysis

- Check task manager for process   - Session statistics

- Verify Windows 10/11 version (19041+)

- Run with administrator privileges#### Model Updates

- **StudySessionLog** enhanced with:

**Automation Not Working**  - `BreaksTaken` field (tracks break count)

- Enable "Allow apps to change window arrangement" in Settings  - `Subject` property alias (for UI compatibility)

- Some window managers (tiling WMs) may block automation  - `DurationMinutes` property alias (calculated from MinutesSpent)

  - `SessionEndTime` property (readonly, from EndTime)

---

#### Service Updates

## 📊 Project Status- **StudyPlanService.cs** added:

  - `GetSessionLogs()` - Returns all session logs for querying

### Build Status  - Session persistence via EndSession() method

- **Desktop**: ✅ 0 Errors

- **Mobile**: ✅ 0 Errors  #### UI Integration

- **Server**: ✅ Ready- **MainWindow.xaml** 

  - Added ⏱ Study Session button to dock

### Component Progress  - "View Session History" menu option

| Component | Phase | Status | Last Updated |  

|-----------|-------|--------|--------------|- **MainWindow.xaml.cs**

| Desktop App | 1-5 | ✅ 100% Complete | Oct 2024 |  - `ShowStudySessionMenu()` - Browse/start sessions

| Mobile App | 6b-Week 2 | 🔄 50% Complete | Oct 28, 2025 |  - `StartStudySession(subject)` - Launch timer window

| Cloud Sync | 6a | ✅ 100% Setup | Oct 2024 |

| Database | 6b-Week 3 | ⏳ Not Started | - |---



### Next Milestones## Next Steps: Future Phases (Optional)

- [ ] Week 2: Complete StudyTimerPage & bindings

- [ ] Week 3: SQLite database integration### Phase 5: Advanced Features (Planned)

- [ ] Week 4: Cloud sync (OneDrive/Google Drive)- [ ] Study session voice notes recording

- [ ] Week 5: Session History & Analytics pages- [ ] AI-powered study recommendation engine

- [ ] App Store release (Google Play)- [ ] Integration with Spotify focus playlists

- [ ] Custom break activities (stretching routines, etc.)

---- [ ] Mobile app for session tracking on the go

- [ ] Cloud synchronization across devices

## 🤝 Contributing- [ ] Focus mode integration with social apps blocking



We welcome contributions! Here's how:### Phase 6: Community & Social (Planned)

- [ ] Share study sessions with study groups

1. **Fork** the repository- [ ] Collaborative study planning

2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)- [ ] Leaderboards and achievement badges

3. **Commit** your changes (`git commit -m 'Add amazing feature'`)- [ ] Productivity insights sharing

4. **Push** to the branch (`git push origin feature/amazing-feature`)

5. **Open** a Pull Request---



### Contribution Guidelines## Stack & Architecture

- Follow C# coding standards

- Write tests for new features- **.NET 8** with WPF (Windows Desktop SDK)

- Update documentation- **Clean separation**: System → Data → Core → App layers (4 projects)

- Reference issues in commits- **Zero circular dependencies**

- **MVVM pattern** for data binding (UI)

---- **Event-driven** service architecture

- **Win32 P/Invoke** for window management

## 📄 License- **JSON serialization** for all persistence

- **Async/await** for non-blocking operations

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### Project Structure

---```

src/FocusDock.System/     → Win32 interop + window tracking (NO deps)

## 🙏 Acknowledgmentssrc/FocusDock.Data/       → Models + JSON persistence (dep: System)

src/FocusDock.Core/       → Business logic services (dep: System, Data)

- **Microsoft** for .NET 8.0, MAUI, and WPFsrc/FocusDock.App/        → WPF UI + menus (dep: all)

- **CommunityToolkit.Mvvm** for reactive bindings```

- **Contributors** like you making this possible

---

---

## Quick Start

## 📞 Support

**Run from Command Line:**

**Found a bug?** [Open an Issue](https://github.com/dertder25t-png/FocusDeck/issues)```powershell

cd C:\Users\Caleb\Desktop\FocusDeck

**Have a question?** [Start a Discussion](https://github.com/dertder25t-png/FocusDeck/discussions)dotnet run --project src/FocusDock.App/FocusDock.App.csproj

```

**Want to chat?** Reach out on [Discord](https://discord.gg/focusdeck) (coming soon)

**Or from Visual Studio:**

---1. Open `FocusDeck.sln`

2. Set `FocusDock.App` as startup project

## 🗺️ Roadmap3. Press F5



### Q4 2024**Or build & run:**

- ✅ Desktop app features complete```powershell

- ✅ MAUI foundationdotnet build

- 🔄 Study timer implementationdotnet run --project src/FocusDock.App/FocusDock.App.csproj

- 🔄 Cloud infrastructure```



### Q1 2025---

- Database integration

- Cloud sync (OneDrive/Google Drive)## First Time Usage

- Session analytics

- App store release prep1. **App launches** with dock at top (default)

2. **Hover to expand** dock to see all windows

### Q2 20253. **Click window** to pin it (green border = pinned)

- Web dashboard4. **Click 📅 Calendar** → "Create Test Event" to add an event

- Collaborative study features5. **Click ✓ Todos** → "Add Task" to create a task

- Advanced analytics6. **Go to Workspace** → "Park Today" to save current state

- API for third-party integrations7. **Close and relaunch** - it auto-restores! ✅



------



## 💡 Tips & Tricks## User Guide: Phase 1 + 2



### Study Timer Best Practices### Window Management

1. **Use Pomodoro** - 25 minutes focus + 5 min break- **Hover dock**: Expands to show windows

2. **Minimize Distractions** - Silence notifications during sessions- **Click window**: Toggle pin (green = pinned)

3. **Track Notes** - Add context to your sessions for better analytics- **Right-click window** (coming): Window options

4. **Sync Across Devices** - Start on desktop, continue on mobile- **Double-click**: Bring window to focus



### Performance Tips### Layout & Workspace

- Close unused applications before study sessions- **Layouts**: Apply 2-col, 3-col, or grid layout

- Keep Windows up to date- **Workspace**: Save and restore window arrangements

- Use SSD for faster database operations- **Park Today**: Quick-save for next session (saves to "Park Today" workspace)

- Limit cloud sync frequency on slow connections- **Restore on launch**: App automatically restores last workspace



---### Calendar & Assignments 📅

- **Click 📅 button**: See upcoming events and assignments

## 🎯 Desktop App Overview (Phase 1-5)- **Create Test Event**: Manual event entry (for testing without API)

- **Canvas Assignments**: Shows due dates and course names

The desktop application provides comprehensive window and workspace management:- **Auto-sync**: Every 15 minutes (once Canvas/Google Calendar API enabled)



- **Auto-Collapsing Dock** - Minimal visual footprint, expands on hover### To-Do List & Study Planning ✓

- **Window Grouping** - Organize windows by application or project- **Click ✓ button**: See active tasks and options

- **Layout Templates** - 2-column, 3-column, and grid layouts- **Add Task**: Quick-create with default due date (tomorrow)

- **Workspace Persistence** - Save and restore complete desktop states- **View All Tasks**: Dashboard with stats and filters

- **Smart Automation** - Time-based and context-aware window management- **Create Study Plan**: AI generates study sessions from assignments

- **Multi-Monitor Support** - Different layouts per monitor- **Task stats**: "X/N completed • Y active • Z overdue"



---### Automations & Reminders

- **Automations**: Set time-based rules (e.g., 9-5 apply "Focus" layout)

## 📱 Mobile App Roadmap- **Reminders**: Get alerts for stale windows (20+ min inactive)

- **Dock**: Change monitor or edge (top/bottom/left/right)

The mobile companion app (Phases 6b-6e) brings study management to Android:- **Focus Mode**: Toggle to hide all windows except pinned ones



**Phase 6b (Current)**---

- Study timer with Pomodoro presets

- Session progress tracking## Data Storage

- Haptic feedback on completion

All data automatically saved to:

**Phase 6c (Next)**```

- SQLite offline storage%LOCALAPPDATA%\FocusDeck\

- Local session history├── settings.json              (dock config)

- Database synchronization setup├── presets.json               (layout presets)

├── workspaces.json            (saved workspaces)

**Phase 6d**├── pins.json                  (pinned windows)

- Cloud sync with OneDrive/Google Drive├── automation.json            (automation rules)

- OAuth2 authentication├── todos.json                 (✓ NEW - tasks)

- Cross-device session sync├── study_plans.json           (✓ NEW - study plans)

├── study_sessions.json        (✓ NEW - session history)

**Phase 6e**├── calendar_events.json       (📅 NEW - manual events)

- Session history browser├── canvas_assignments.json    (📅 NEW - assignments)

- Analytics dashboard└── calendar_settings.json     (📅 NEW - API config)

- Settings management```



---**Backup:** Copy entire folder to backup all data



**Made with 💜 for students and professionals who want to focus.**---


## Documentation

- **QUICKSTART.md** - Complete user guide with examples (380 lines)
- **DEVELOPMENT.md** - Architecture & developer guide (320 lines)
- **PHASE2_IMPLEMENTATION.md** - Phase 2 features detailed (NEW!)
- **PHASE2_TESTING.md** - Testing guide & scenarios (NEW!)
- **STATUS.md** - Implementation summary & current status
- **IMPLEMENTATION_SUMMARY.md** - What was built & how
- **00_START_HERE.md** - Main entry point for new users

---

## What's Included

### Phase 1 Features ✅
✅ Window management via dock
✅ Pin system with persistence
✅ Layout presets (2-col, 3-col, grid)
✅ Workspace save/restore
✅ "Park Today" quick-save
✅ Time-based automations
✅ Stale window reminders
✅ Multi-monitor support
✅ Full data persistence

### Phase 2 Features ✅ NEW!
✅ Calendar event management
✅ Canvas assignment tracking (API ready)
✅ To-do list with priorities
✅ AI study plan generation
✅ Study session tracking
✅ Task statistics & summaries
✅ Complete data persistence
✅ UI buttons + dropdown menus

---

## What's Next (Phase 2.1+)

### Planned: Phase 2.1 (Google Calendar & Canvas APIs)
- Implement Google Calendar OAuth2 + event fetch
- Implement Canvas API client + assignment fetch
- Auto-apply layouts based on class times
- Study session tracking UI
- Productivity analytics dashboard

### Planned: Phase 2.2 (Notes & Resources)
- Quick note capture + linking
- Assignment resource tracker
- Study material organization
- Integration with calendar/tasks

### Planned: Phase 3 (Mobile)
- Android companion app
- Task/reminder sync
- Study session control
- Calendar view on mobile



New in This Release ✨
---------
✅ **Pin Persistence** - Pins now saved across sessions
✅ **Workspace Auto-Restore** - App restores saved state on startup  
✅ **Park Today Feature** - One-click end-of-day save
✅ **Fixed Build** - All 24 errors resolved
✅ **Clean Architecture** - Zero circular dependencies
✅ **Documentation** - 1000+ lines of guides

What's Next (Phase 2+)
------
- Visual polish for vertical dock when on left/right edges
- Confirm safe-apply UX (grace/undo) before automation changes layout


this is the goal of the project 
- Here’s a **complete outline** of everything we’ve designed so far for your app — including structure, layout, modules, and every feature category we’ve discussed (from the dock to notes, AI, automation, and study tools).
This serves as a **master blueprint** for the entire system.

---

# 🧭 Project Overview: “FocusDock”

### **Goal**

A modular productivity and learning platform that **replaces the Windows taskbar** with a **personal cognitive assistant** — built for focus, automation, and organization for students, creatives, and ADHD users.

### **Core Vision**

A single, minimal interface that combines:

* Smart window & layout management,
* Adaptive automation (based on time, context, or device),
* Deep school integration (Canvas, Google Calendar),
* Built-in note-taking and lecture capture,
* Flashcards and AI-assisted studying,
* Calm, distraction-free design with subtle visual feedback.

---

# ⚙️ 1. Dock & Layout System (The Core Shell)

### **Purpose**

Replace the taskbar with a flexible, customizable dock that visually organizes open windows, apps, and projects.

### **Main Features**

* **Auto-Collapse Dock:** A 2–4px ribbon that expands on hover or click.
* **Positions:** Placeable top, bottom, sides, or on multiple monitors.
* **Visual Stacks:** Group windows/tabs by app or project with live thumbnails.
* **Pin System:** Keep important windows always visible even when minimized.
* **Focus Mode:** Hide everything but pinned or essential apps.
* **Quick Actions:** Close/Save/Snooze entire groups at once.
* **Workspaces:** Save entire app layouts (e.g., “Bible Class,” “Study Mode,” “Design Layout”).
* **Session Restore:** On reboot, reopens everything where you left off.

### **UI States**

1. **Collapsed:** Ribbon only, with subtle pulses for reminders or activity.
2. **Peek:** Hover briefly → shows mini view (counts, next class, to-dos).
3. **Expanded:** Hover longer or click → full interactive dock (windows, tasks, reminders, calendar, notes).

---

# 🧩 2. Window & App Management

### **Smart Layouts**

* Define zones (grids, ratios, or exact pixels).
* Assign apps to zones (“Notion right, Chrome left”).
* Save & recall as presets (triggered manually or by automation).

### **Adaptive Layout Engine**

* Auto-applies layouts based on:

  * Time (class/work hours),
  * Wi-Fi network (campus, dorm),
  * Connected monitor(s),
  * Active app combination.

### **Rules Engine Example**

> If time is 2:45–3:49 PM and Wi-Fi = PCC-Campus → Load Bible Class Layout
> If session unlock after 9 PM → Load “Study Mode”

---

# 🔄 3. Automation & AI Scheduling System

### **Triggers**

* Time (hour/day/class schedule)
* Device state (lid open, unlock, power source)
* App usage (if Chrome + OBS open → “Podcast Layout”)
* Wi-Fi network
* Location (optional via Android)
* Calendar events (from Google or Canvas)

### **Actions**

* Load workspace/layout
* Hide/show app stacks
* Toggle Focus Mode / DND
* Open resources or notes for a class
* Start recording or study session

### **Safety Features**

* Never interrupts while clicking or typing.
* Soft-preview before applying layout (2s grace).
* Undo button after every automation.

### **Template Learning**

* Learns your preferences:

  > “You always open Notion + Logos during Bible class — apply this to Church History too?”

---

# 📆 4. Calendar & Daily Agenda

### **Integrations**

* **Google Calendar:** Class times, meetings, events.
* **Canvas Calendar:** Assignment due dates, class sessions, tests.

### **Views**

* **Agenda Strip:** Today/Tomorrow summary.
* **Calendar Chips:** Color-coded class/time markers.
* **Mini Pill Alerts:** “Class starts in 10 minutes.”

### **Automation Hooks**

* Class time triggers → Auto layout + Focus Mode.
* Due assignments → AI prompts study plan.

---

# ✅ 5. Daily To-Do & Task System

### **Unified To-Do**

* Canvas assignments, calendar tasks, and custom tasks all in one list.
* Grouped by course, priority, or due date.

### **Smart Actions**

* Check-off = marks Canvas assignment as done (optional).
* Snooze = reschedules.
* “Study Mode” = opens related notes, flashcards, Focus Mode.
* “AI Plan” = generates subtasks & time estimates automatically.

---

# 📝 6. Note-Taking System (Course-Based)

### **Organization**

* Per course → per class session → per page.
* Automatically titled by date & class.

### **Features**

* **Rich text editing** (markdown/lightweight).
* **Inline timestamps** (linked to recording playback).
* **Canvas links & assignment previews**.
* **Drag-and-drop images/PDFs**.
* **Star key lines** for flashcards or summary.

### **Storage**

* Saved locally in SQLite + file system.
* Linked to calendar events and assignments.

---

# 🎙️ 7. Lecture Audio Recording + Transcription

### **Capture**

* One-click record button (auto-names with class & date).
* Pause/resume; stores to local audio folder.
* Timestamp synchronization with notes.
* Optional noise suppression & auto-leveling.

### **Transcription**

* **Local (Whisper)** or **cloud** (for speed).
* Speaker separation (basic diarization).
* Segment mapping (links text to timestamps).

### **Use Case**

* Review by clicking timestamp in notes.
* Run AI “Coverage Check” after class.

---

# 💡 8. AI Systems (Study, Organization, and Focus)

### **A. Reminder & Cleanup AI**

* Detects idle or unused tabs/windows:

  > “These 6 tabs haven’t been touched in 4 days. Close, save, or snooze?”
* Tracks repeated app combos and proposes layouts.

### **B. Study/Research AI**

* Summarizes assignments & rubrics into objectives.
* Analyzes notes + lecture transcripts:

  > “You missed 2 key concepts: Atonement models, Historical context.”
* Generates:

  * Study summaries
  * Missing points list
  * Flashcards for weak areas
  * Suggested re-listen timestamps

### **C. Focus AI**

* Detects when you drift (idle app switching, too many tabs).
* Suggests Focus Mode or hides distractions.
* Optionally auto-triggers Focus Layouts (e.g., during study hours).

---

# 🎴 9. Flashcards & Study System

### **Creation**

* Manual: select notes text → “Make Flashcard.”
* Auto: AI extracts terms/definitions from notes + audio.

### **Card Types**

* Q&A
* Cloze (fill-in-the-blank)
* Verse or concept recall
* Image occlusion

### **Decks**

* Per course or topic.
* Links back to note lines & timestamps.
* Includes context preview for reference.

### **Review Mode**

* Spaced repetition (SM-2 algorithm).
* Daily goal (e.g., 20 new + 50 review cards).
* Quick Focus Study layout (cards on right, notes/audio left).

---

# 🎓 10. AI Coverage Check (for Exams & Projects)

### **Purpose**

To make sure your notes and audio cover all key material for a test or paper.

### **Input Sources**

* Canvas assignment & rubric.
* Lecture audio transcription.
* Class notes.
* Linked readings or slides.

### **Output**

* Coverage percentage.
* Missing or weak topics (with audio jump link).
* Suggested study plan (sessions, focus areas, flashcards).

### **Access**

* “Run Coverage Check” button on notes page or after recording.
* Appears as a dock pill: “Coverage ready – 81% (Bible Class).”

---

# 🧠 11. Study Mode (Immersive Workspace)

### **Layout**

* Full-screen: Notes left, Flashcards or reference right, timer top.
* Auto-hides dock, notifications, and unrelated apps.

### **Features**

* Timer cycles (Pomodoro or custom).
* Real-time progress bar.
* “I’m Stuck” → opens relevant note/audio.
* Post-session summary (“Studied 32 min, reviewed 20 cards”).

---

# 📚 12. Canvas Integration (Deep Linking)

### **Data Synced**

* Courses
* Assignments
* Calendar events
* Pages & files (optional for AI indexing)

### **Use Cases**

* Assignment preview in dock.
* “Open in Canvas” link.
* Auto-create note page for new assignment.
* AI can read Canvas assignment details to generate outlines.

---

# 📱 13. Android Companion App (Optional)

### **Features**

* Dock remote control (expand, collapse, switch workspace).
* Notifications digest (class reminders, tasks, AI insights).
* Quick study review (flashcards & daily summary).
* Geofence or Wi-Fi trigger for automations.

---

# 🎨 14. Design Philosophy

### **Aesthetics**

* Clean, translucent, modern (Fluent/Glass).
* Color-coded by course or workspace.
* Gentle animations, no harsh notifications.

### **Usability**

* **Mouse-first** → hover, scroll, click, drag.
* Context menus & radial controls.
* Multi-monitor aware, lightweight (low CPU).

---

# 🧱 15. Technical Architecture

### **Core Layers**

| Layer                 | Description                                                                    |
| --------------------- | ------------------------------------------------------------------------------ |
| **UI Layer**          | WinUI 3 or Avalonia UI – dock, panels, notes editor                            |
| **System Hooks**      | Win32 API for window tracking, positioning                                     |
| **Data Layer**        | SQLite for persistent state                                                    |
| **Automation Engine** | JSON-based rules evaluator + scheduler                                         |
| **Integration Layer** | Canvas API, Google Calendar API, browser extension                             |
| **AI Layer**          | Modular inference system (Whisper, summarizer, card generator, coverage model) |
| **Audio Layer**       | WASAPI recorder + transcription pipeline                                       |
| **IPC/Bridge**        | Local WebSocket for browser, optional mobile companion                         |

---

# 🧩 16. MVP Milestones (Build Order)

### **Phase 1: Foundations**

* Dock (collapse/expand)
* Window stacks, pin/unpin
* Layout zones + manual apply/save
* Reminder AI (aged tabs, zombies)

### **Phase 2: Automations**

* Time-based layout triggers
* Wi-Fi & unlock detection
* “Park Today” end-of-day save
* Template learning

### **Phase 3: Calendar + To-Do**

* Google + Canvas sync
* Unified agenda + daily list
* Simple AI “Plan Study” feature

### **Phase 4: Notes + Audio**

* Note editor + audio recording
* Timestamp linking
* Local transcription

### **Phase 5: Flashcards + Coverage Check**

* Card generator + review UI
* AI analysis vs assignment/syllabus

### **Phase 6: Study Mode**

* Immersive view + timer + analytics

### **Phase 7: Polish & Companion**

* Android app
* Visual themes, gestures, performance tuning.

---

# 🔮 17. Long-Term Potential

Future modules could include:

* Cloud sync (encrypted backups)
* Collaboration (shared notes/projects)
* Habit tracking (based on study focus)
* Cross-device AI continuity (“continue where you left off”)
* Smart insights (“You study best after 9PM—schedule Bible class review then?”)

---

Would you like me to make a **visual layout diagram or wireframe** next (showing how the dock, panels, and study areas fit together on screen)? That would help you or a designer start prototyping the UI flow visually.
