# Phase 4: Study Session UI & Real-Time Tracking

**Goal:** Build the real-time study session interface with timer, tracking, and effectiveness scoring.

---

## Phase 4 Roadmap

### 1. Study Session Window (New UI)
- ✅ Modal dialog showing active study session
- ✅ Large, clear timer display
- ✅ Subject/task being studied
- ✅ Play/Pause/Stop controls
- ✅ Break recommendations (Pomodoro)
- ✅ Visual progress indicator

### 2. Study Session Service (Enhanced)
- ✅ Track elapsed time precisely
- ✅ Manage break intervals
- ✅ Auto-pause detection (idle)
- ✅ Session persistence
- ✅ History tracking

### 3. Effectiveness Tracking
- ✅ End-of-session effectiveness rating (1-5 stars)
- ✅ Store effectiveness scores
- ✅ Calculate productivity metrics
- ✅ Suggest optimal study times

### 4. Study History Dashboard
- ✅ View past sessions
- ✅ Filter by date/subject
- ✅ Productivity trends
- ✅ Total study hours

### 5. Integration Points
- ✅ Start session from task menu
- ✅ Auto-start before due dates (configurable)
- ✅ Sync with calendar/assignments
- ✅ Show session stats in dock

---

## Implementation Plan

### Phase 4a: Study Session Window (This Session)
1. Create `StudySessionWindow.xaml` (dark UI with timer)
2. Create `StudySessionWindow.xaml.cs` (timer logic)
3. Update `StudyPlanService` (session management)
4. Add "Start Session" button to UI
5. Test with manual session

### Phase 4b: Effectiveness Rating (Next Session)
1. Create effectiveness rating dialog
2. Store ratings in StudySessionLog
3. Calculate productivity statistics
4. Display in dock menus

### Phase 4c: History & Analytics (If Time)
1. Build session history viewer
2. Add trend analysis
3. Suggest optimal study times
4. Export session data

---

## Let's Start!

Ready to build the Study Session UI?
