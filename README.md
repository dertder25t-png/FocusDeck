FocusDock - Study Productivity Platform
======================================

A WPF-based productivity dock combining window management, calendar integration, task tracking, and AI-powered study planning into one elegant application.

## Status Dashboard

| Phase | Status | Build | Details |
|-------|--------|-------|---------|
| Phase 1 | ✅ Complete | 0 Errors | Window management, workspaces, automations |
| Phase 2 | ✅ Complete | 0 Errors | Calendar, tasks, study planner infrastructure |
| Phase 3 | ✅ Complete | 0 Errors | Google/Canvas APIs ready, Settings UI working |
| **Phase 4** | **✅ Complete** | **0 Errors** | **Study Session UI, History Dashboard, Analytics** |

**Latest Build:** ✅ 0 Errors | 1 Warning (non-blocking SDK) | 2.3s compile | All Phases Complete

---

## Phase 1 Features ✅

- Auto-collapsing dock UI (configurable edge: top/bottom/left/right)
- Real-time window tracking via Win32 P/Invoke
- Windows grouped by process name with live updates
- **Pin system with persistent storage** ✅
- Layout templates: Two-Column, Three-Column, Grid 2x2
- Save & apply named layout presets
- Multi-monitor aware (apply layouts per monitor)
- **Workspace system with auto-restore** ✅
- **"Park Today" feature for end-of-day save** ✅
- Time-based automations with 2-second preview + undo
- Stale window detection and reminders
- Dock position management (monitor + edge selection)
- Clock display
- Full JSON persistence for all settings

---

## Phase 2 Features 🎓 **NEW!**

### Calendar Integration 📅
- **Google Calendar API support** (ready for setup)
- **Canvas LMS assignment tracking** (ready for setup)
- Upcoming events/assignments display in dock
- Manual event creation and persistence
- Automatic sync every 15 minutes (configurable)
- Events can trigger workspace/layout changes

### To-Do List Management ✓
- Create, prioritize, and complete tasks
- Priority levels: Low, Medium, High, Urgent
- Due date tracking with overdue alerts
- Canvas assignment sync (auto-create tasks)
- Quick task statistics: "5/12 completed • 3 active • 1 overdue"
- Bulk operations (clear completed, filter by tag)
- Full task persistence to JSON

### AI Study Planner 📚
- Auto-generate study plans from assignments
- Distribute study hours across available days
- Create timed study sessions with Pomodoro recommendations
- Track study session history with effectiveness ratings
- Productivity summary: "8 sessions • 12.5h • 4.2/5 effectiveness"
- Generate plans in <500ms

### New UI Elements
- 📅 Calendar button - shows events + assignments
- ✓ To-Do button - shows tasks + options
- Both accessible via dropdown menus
- Integrated with existing dock menu system

---

## Phase 3 Features ✅ Complete

### Google Calendar Integration ✅
- ✅ **OAuth2 provider** - Complete flow with token refresh
- ✅ **API client** - Fetch events, handle responses
- ✅ **Settings window integration** - Configured UI with tabs
- ✅ **Interactive authorization flow** - Ready for user setup
- ⏳ 15-minute auto-sync with real events (waiting for credentials)
- ⏳ Event-based layout triggers (Phase 3 Future)

### Canvas LMS Integration ✅
- ✅ **Canvas API provider** - Complete course/assignment fetching
- ✅ **Connection testing** - Verify tokens and URLs
- ✅ **Settings window integration** - Configured UI
- ⏳ Auto-create tasks from real assignments (Phase 3 Future)
- ⏳ Due date reminders (Phase 3 Future)
- ⏳ Submission status tracking (Phase 3 Future)

### Study Session UI ✅ **NEW IN PHASE 4a**
- ✅ **Real-time session timer** - Displays in MM:SS:SS format with 500ms updates
- ✅ **Active study tracking** - Progress bar with 60-minute session goal
- ✅ **Break reminders** - Pomodoro-style alert at 25 minutes
- ✅ **Effectiveness rating popup** - 1-5 star rating dialog after session end
- ✅ **Session logging** - Persists to JSON with timestamp, subject, breaks, rating

### Completed Phase 3 Infrastructure ✅
- ✅ **GoogleCalendarProvider.cs** (280 lines)
  - OAuth2 authorization URL generation
  - Token exchange and refresh
  - Calendar event fetching (30-day lookhead)
  - Proper error handling

- ✅ **CanvasApiProvider.cs** (200+ lines)
  - Canvas API authentication
  - Course and assignment fetching
  - Connection testing
  - Submission status detection

- ✅ **Settings Window** (XAML + C# codebehind)
  - 📅 Calendar tab: Google OAuth setup + Canvas config
  - ✓ Tasks tab: Import options
  - 📚 Study tab: Session preferences
  - ℹ About tab: Version + resources
  - Settings button (⚙) integrated into dock

- ✅ **CalendarService integration**
  - Automatic provider instantiation with saved credentials
  - Token management
  - Error resilience for failed syncs

### Setup Guide
- 📖 **API_SETUP_GUIDE.md** - Step-by-step instructions
  - Google Cloud Console project creation
  - Canvas API token generation
  - FocusDeck configuration
  - Troubleshooting section
  - Privacy & security notes

---

## Phase 4 Features ✅ COMPLETE - Study Session & Productivity Tracking

### Phase 4a: Study Session UI ✅

#### Study Session Window
- ✅ **Dark-themed timer UI** - Visual consistency with FocusDock aesthetic
- ✅ **Header with subject display** - Shows study topic (blue accent)
- ✅ **Large timer display** - Real-time elapsed time (HH:MM:SS) updating every 500ms
- ✅ **Progress tracking** - Visual bar showing session progress (0-60 minutes target)
- ✅ **Control buttons**
  - Play/Pause toggle (⏸/▶) - Pause timer without ending session
  - Break button - Increment break counter manually
  - End Session button - Finish and rate effectiveness
- ✅ **Session stats footer**
  - Focus rate percentage (active study time)
  - Break count (auto-incremented)
  - Session stats display

#### Pomodoro Integration
- ✅ **25-minute break reminder** - Alert banner appears at 25-minute mark
- ✅ **Break tracking** - Automatically counts breaks taken during session
- ✅ **Session pause** - Paused time doesn't count toward effective study time
- ✅ **Configurable reminder** - BreakReminderMinutes constant (customizable per session)

#### Effectiveness Rating System
- ✅ **Post-session rating dialog** - 1-5 star rating popup after session ends
- ✅ **Session metadata collection**
  - Session ID (GUID for unique identification)
  - Subject (study topic)
  - Total duration (in minutes)
  - Breaks taken (count)
  - Effectiveness rating (1-5 stars)
  - Optional notes field
- ✅ **Persistent session logging**
  - Saved to JSON via StudyPlanService.EndSession()
  - Includes timestamp and all session data
  - Queryable for history and analytics

#### Integration with Main Window
- ✅ **⏱ Study Session button** - New button added to dock UI
- ✅ **Study Plans menu** - Shows available study plans from earlier creation
- ✅ **Session list** - Browse and start sessions from existing plans
- ✅ **Quick start** - Manually enter subject for ad-hoc study sessions

### Phase 4b: Study Session History Dashboard ✅

#### Session History Window
- ✅ **Date range filtering** - Select "From" and "To" dates for custom ranges
- ✅ **Filter button** - Apply date range and refresh session list
- ✅ **Complete session list display**
  - Subject name with emoji indicator (📚)
  - Date/time completed
  - Duration in minutes
  - Effectiveness rating (1-5)
  - Breaks taken count
  - Organized by most recent first

#### Statistics Strip (Real-time Updates)
- ✅ **Total Sessions** - Count of all sessions in date range
- ✅ **Total Hours** - Sum of all session time (formatted as X.Xh)
- ✅ **Avg Effectiveness** - Average 1-5 rating across sessions
- ✅ **Total Breaks** - Sum of breaks across all sessions

#### Export Feature
- ✅ **CSV Export** - Download session data for external analysis
  - Columns: Subject, Date, Duration (min), Effectiveness, Breaks, Notes
  - Saved to Documents folder with timestamp
  - Full session history preserved

#### Integration
- ✅ **Launch from Main Window** - "View Session History" menu option
- ✅ **Accessible from context menus** - Quick access from dock buttons

### Phase 4c: Productivity Analytics Dashboard ✅

#### Summary Statistics
- ✅ **Total Study Time** - Aggregate hours studied in last 30 days
- ✅ **Avg Daily Hours** - Total hours / unique study days
- ✅ **Avg Effectiveness** - Average rating across all sessions
- ✅ **Most Studied Subject** - Subject with highest total minutes

#### Daily Study Pattern
- ✅ **Daily breakdown** - Lists each day's study hours (last 30 days)
- ✅ **Format**: "M/d: X.Xh (XXXm)" for easy scanning
- ✅ **Visual timeline** - Shows study habits and consistency

#### Subject Breakdown Analysis
- ✅ **Subject statistics** - Shows minutes, percentage, and session count per subject
- ✅ **Ranked display** - Sorted by most studied to least studied
- ✅ **Percentage calculation** - Helps visualize study focus areas
- ✅ **Session count** - Shows how many sessions per subject

#### Effectiveness Trend Tracking
- ✅ **Weekly effectiveness** - Average rating per week for trend detection
- ✅ **Trend visualization** - "Week 1: 4.2★ → Week 2: 4.5★" format
- ✅ **Performance insights** - Identify if effectiveness is improving
- ✅ **Fallback message** - Alerts if no ratings recorded yet

#### Session Statistics
- ✅ **Total sessions** - Count of all study sessions
- ✅ **Longest session** - Maximum session length in minutes
- ✅ **Avg session** - Mean session duration
- ✅ **Break patterns** - Total breaks and average breaks per session

#### Launch Points
- ✅ **From History Window** - "View Analytics" button opens dashboard
- ✅ **Modal dialog** - Opens as child of History window
- ✅ **Real-time updates** - Pulls current data from session logs

### Architecture & Implementation

#### New Files Created (Phase 4)
1. **StudySessionWindow.xaml** (140 lines)
   - XAML UI with dark theme matching dock
   - Timer display, buttons, progress bar
   - Session stats footer

2. **StudySessionWindow.xaml.cs** (260+ lines)
   - DispatcherTimer for real-time updates
   - Timer logic (play/pause/resume)
   - Break reminder at 25 minutes
   - Effectiveness rating dialog
   - Session persistence

3. **StudySessionHistoryWindow.xaml** (106 lines)
   - Date range pickers
   - Statistics strip
   - Session list display
   - Export and analytics buttons

4. **StudySessionHistoryWindow.xaml.cs** (130 lines)
   - Date filtering logic
   - Statistics calculations
   - CSV export functionality
   - Data binding for list display

5. **ProductivityAnalyticsWindow.xaml** (86 lines)
   - Analytics dashboard layout
   - Statistics display areas
   - Breakdown sections

6. **ProductivityAnalyticsWindow.xaml.cs** (138 lines)
   - Analytics calculations
   - Weekly trend computation
   - Subject breakdown analysis
   - Session statistics

#### Model Updates
- **StudySessionLog** enhanced with:
  - `BreaksTaken` field (tracks break count)
  - `Subject` property alias (for UI compatibility)
  - `DurationMinutes` property alias (calculated from MinutesSpent)
  - `SessionEndTime` property (readonly, from EndTime)

#### Service Updates
- **StudyPlanService.cs** added:
  - `GetSessionLogs()` - Returns all session logs for querying
  - Session persistence via EndSession() method

#### UI Integration
- **MainWindow.xaml** 
  - Added ⏱ Study Session button to dock
  - "View Session History" menu option
  
- **MainWindow.xaml.cs**
  - `ShowStudySessionMenu()` - Browse/start sessions
  - `StartStudySession(subject)` - Launch timer window

---

## Next Steps: Future Phases (Optional)

### Phase 5: Advanced Features (Planned)
- [ ] Study session voice notes recording
- [ ] AI-powered study recommendation engine
- [ ] Integration with Spotify focus playlists
- [ ] Custom break activities (stretching routines, etc.)
- [ ] Mobile app for session tracking on the go
- [ ] Cloud synchronization across devices
- [ ] Focus mode integration with social apps blocking

### Phase 6: Community & Social (Planned)
- [ ] Share study sessions with study groups
- [ ] Collaborative study planning
- [ ] Leaderboards and achievement badges
- [ ] Productivity insights sharing

---

## Stack & Architecture

- **.NET 8** with WPF (Windows Desktop SDK)
- **Clean separation**: System → Data → Core → App layers (4 projects)
- **Zero circular dependencies**
- **MVVM pattern** for data binding (UI)
- **Event-driven** service architecture
- **Win32 P/Invoke** for window management
- **JSON serialization** for all persistence
- **Async/await** for non-blocking operations

### Project Structure
```
src/FocusDock.System/     → Win32 interop + window tracking (NO deps)
src/FocusDock.Data/       → Models + JSON persistence (dep: System)
src/FocusDock.Core/       → Business logic services (dep: System, Data)
src/FocusDock.App/        → WPF UI + menus (dep: all)
```

---

## Quick Start

**Run from Command Line:**
```powershell
cd C:\Users\Caleb\Desktop\FocusDeck
dotnet run --project src/FocusDock.App/FocusDock.App.csproj
```

**Or from Visual Studio:**
1. Open `FocusDeck.sln`
2. Set `FocusDock.App` as startup project
3. Press F5

**Or build & run:**
```powershell
dotnet build
dotnet run --project src/FocusDock.App/FocusDock.App.csproj
```

---

## First Time Usage

1. **App launches** with dock at top (default)
2. **Hover to expand** dock to see all windows
3. **Click window** to pin it (green border = pinned)
4. **Click 📅 Calendar** → "Create Test Event" to add an event
5. **Click ✓ Todos** → "Add Task" to create a task
6. **Go to Workspace** → "Park Today" to save current state
7. **Close and relaunch** - it auto-restores! ✅

---

## User Guide: Phase 1 + 2

### Window Management
- **Hover dock**: Expands to show windows
- **Click window**: Toggle pin (green = pinned)
- **Right-click window** (coming): Window options
- **Double-click**: Bring window to focus

### Layout & Workspace
- **Layouts**: Apply 2-col, 3-col, or grid layout
- **Workspace**: Save and restore window arrangements
- **Park Today**: Quick-save for next session (saves to "Park Today" workspace)
- **Restore on launch**: App automatically restores last workspace

### Calendar & Assignments 📅
- **Click 📅 button**: See upcoming events and assignments
- **Create Test Event**: Manual event entry (for testing without API)
- **Canvas Assignments**: Shows due dates and course names
- **Auto-sync**: Every 15 minutes (once Canvas/Google Calendar API enabled)

### To-Do List & Study Planning ✓
- **Click ✓ button**: See active tasks and options
- **Add Task**: Quick-create with default due date (tomorrow)
- **View All Tasks**: Dashboard with stats and filters
- **Create Study Plan**: AI generates study sessions from assignments
- **Task stats**: "X/N completed • Y active • Z overdue"

### Automations & Reminders
- **Automations**: Set time-based rules (e.g., 9-5 apply "Focus" layout)
- **Reminders**: Get alerts for stale windows (20+ min inactive)
- **Dock**: Change monitor or edge (top/bottom/left/right)
- **Focus Mode**: Toggle to hide all windows except pinned ones

---

## Data Storage

All data automatically saved to:
```
%LOCALAPPDATA%\FocusDeck\
├── settings.json              (dock config)
├── presets.json               (layout presets)
├── workspaces.json            (saved workspaces)
├── pins.json                  (pinned windows)
├── automation.json            (automation rules)
├── todos.json                 (✓ NEW - tasks)
├── study_plans.json           (✓ NEW - study plans)
├── study_sessions.json        (✓ NEW - session history)
├── calendar_events.json       (📅 NEW - manual events)
├── canvas_assignments.json    (📅 NEW - assignments)
└── calendar_settings.json     (📅 NEW - API config)
```

**Backup:** Copy entire folder to backup all data

---

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
