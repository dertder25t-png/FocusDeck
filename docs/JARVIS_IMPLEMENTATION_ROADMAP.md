#  FocusDeck JARVIS Implementation Roadmap

**Status:** Phase 7 - JARVIS AI Companion  
**Version:** 1.0  
**Created:** November 5, 2025  
**Owner:** Caleb Carrillo-Miranda  

---

##  Executive Vision

Transform FocusDeck from a productivity tracker into **JARVIS for Academics**  an autonomous AI companion that:

- **Watches without pestering** - Multi-sensor awareness (keyboard, mouse, window tracking, phone motion)
- **Automates everything possible** - Zero manual input for routine tasks (homework drafts, flashcard generation, content creation)
- **Only interrupts when it matters** - Context-aware notifications with intelligent filtering
- **Learns who you are** - Personalization, spiritual health tracking, burnout prevention
- **Never lets you burn out** - Detects unsustainability, enforces breaks, balances metrics

**Core Thesis:** AI does the work FOR you. You only make decisions when human judgment is required.

---

##  Implementation Phases (6-Month Plan)

### Phase 1: Foundation - Multi-Sensor Context Detection (Weeks 1-4)

**Goal:** Build the observability layer so JARVIS sees what students are doing

#### 1.1 Activity Detection Service
- [ ] Window/application tracking (Desktop WPF & Linux)
- [ ] Keyboard/mouse activity monitoring
- [ ] Mobile device motion detection (accelerometer, gyroscope)
- [ ] Focus state detection (when user is actively working vs. idle)

**Deliverables:**
- IActivityDetectionService interface (FocusDeck.Services)
- Windows implementation (WinEventHook for window tracking)
- Linux implementation (wmctrl/xdotool commands)
- Mobile implementation (MAUI device motion sensors)

**Tests:**
- [ ] Activity detection accuracy (>95%)
- [ ] Minimal performance impact (<5% CPU overhead)
- [ ] Cross-platform consistency

#### 1.2 Context Aggregation
- [ ] Merge multi-sensor data into unified context state
- [ ] Canvas/assignment detection (polling Canvas API for upcoming due dates)
- [ ] Note proximity detection (what notes are open/relevant)

**Deliverables:**
- IContextAggregationService - Real-time student context
- SignalR broadcast of context changes to all devices
- Database entity: StudentContext (timestamp, app, focus_level, canvas_assignments)

**Tests:**
- [ ] Aggregation latency <100ms
- [ ] All platforms report consistent state

---

### Phase 2: Burnout Detection & Prevention (Weeks 5-8)

**Goal:** Protect student wellness by detecting and preventing burnout

#### 2.1 Pattern Recognition
- [ ] Track work session length and break frequency
- [ ] Monitor sleep data integration (if available)
- [ ] Measure output quality trends (declining quality = fatigue signal)
- [ ] Analyze work intensity variability

**Deliverables:**
- IBurnoutAnalysisService - Weekly pattern analysis
- Database schema: StudentWellnessMetrics (hours_worked, break_frequency, quality_score, sleep_hours)
- Algorithm: Detect unsustainable patterns (3+ consecutive 12hr days, break frequency drops >50%)

#### 2.2 Burnout Prevention Actions
- [ ] Smart break enforcement (app blocks work at configured time)
- [ ] Intelligent downtime suggestions (outdoor break, sleep, etc.)
- [ ] Weekly wellness report
- [ ] User override handling (graduated enforcement if ignored)

**Deliverables:**
- IBurnoutPreventionService - Reactive intervention
- Background job: BurnoutCheckJob (runs every 2 hours)
- SignalR notification: Burnout alert with smart recommendations
- UI: Wellness dashboard + enforcement controls

---

### Phase 3: Intelligent Notification Management (Weeks 9-12)

**Goal:** Act as a smart bouncer  only important notifications get through

#### 3.1 Notification Filtering Engine
- [ ] Priority scoring system (urgency  importance  context)
- [ ] Whitelist/blacklist per contact and app
- [ ] Do Not Disturb during focus sessions
- [ ] Batch notifications for low-priority items

**Deliverables:**
- INotificationFilterService - Priority-based routing
- Database entity: NotificationPreference (contact/app, priority, batch_settings)
- Filter rules engine (priority algorithm)

#### 3.2 Notification Customization UI
- [ ] Contact priority management
- [ ] App whitelist configuration
- [ ] Custom filter rules
- [ ] Quiet hours scheduling

**Deliverables:**
- Web UI: Notification preferences page
- Mobile UI: Quick settings for focus mode
- Desktop UI: Tray icon for quick access

---

### Phase 4: Autonomous Content Generation (Weeks 13-20)

**Goal:** AI drafts homework, essays, flashcards  student reviews and approves

#### 4.1 Canvas Integration Enhancement
- [ ] Real-time assignment detection
- [ ] Syllabus parsing (extract key topics)
- [ ] Professor emphasis detection (Canvas announcements)
- [ ] Due date tracking and alerts

#### 4.2 Flashcard Auto-Generation
- [ ] Extract key concepts from student notes
- [ ] AI generates flashcards in student's style
- [ ] Automatic MLA/APA citation
- [ ] Mastery tracking per card

**Deliverables:**
- IFlashcardGenerationService - AI-powered card creation
- Integration with notes: Scan for key terms, definitions, concepts
- Notification: 45 flashcards ready for [Canvas Assignment]

#### 4.3 Essay Draft Generation (MVP)
- [ ] Parse assignment prompt
- [ ] Analyze student's past essay writing style
- [ ] Generate outline with thesis, 3 body paragraph topics, conclusion
- [ ] Gather relevant quotes/sources from student notes
- [ ] Mark sections needing student input

**Deliverables:**
- IEssayDraftService - Structured draft creation
- Template system for essay structure
- Draft-for-review workflow

#### 4.4 Study Guide Generation
- [ ] Automatic summary from course materials
- [ ] Key concept extraction
- [ ] Practice problem generation
- [ ] Links to relevant notes

---

### Phase 5: Context-Aware Workspace Automation (Weeks 21-24)

**Goal:** JARVIS auto-prepares workspace based on detected context

#### 5.1 Workspace Preparation
- [ ] Detect assignment detected  Open desktop prep
- [ ] Auto-organize: Canvas assignment, relevant notes, reference materials
- [ ] Create study session playlist/focus music
- [ ] Block distracting apps proactively

#### 5.2 Focus Mode Enforcement
- [ ] Disable notifications (except critical)
- [ ] Block Reddit, Discord, YouTube during focus sessions
- [ ] Show focus timer with progress
- [ ] Streak tracking (3 consecutive undistracted sessions)

**Deliverables:**
- Enhanced FocusMode service
- DNS/host file blocking (localhost routing to blocking page)
- UI: Focus timer on desktop, mobile

---

### Phase 6: Balanced Productivity Metrics (Weeks 25-26)

**Goal:** Track BOTH output AND protection (focus time, breaks, wellness)

#### 6.1 Metrics Dashboard
- [ ] Weekly report: Tasks completed, hours worked, focus quality
- [ ] Protection metrics: Deep focus sessions, distractions blocked, intentional breaks
- [ ] Wellness metrics: Sleep consistency, burnout score, balance index
- [ ] Trend analysis: Week-over-week comparisons

#### 6.2 Personalization Learning
- [ ] Track which features user engages with most
- [ ] Learn optimal focus window length
- [ ] Adapt notification filtering based on behavior
- [ ] Adjust burnout threshold per student

---

##  Architecture Overview

### New Service Interfaces

- IActivityDetectionService - Cross-platform activity monitoring
- IBurnoutAnalysisService - Pattern recognition and wellness analysis
- INotificationFilterService - Smart notification routing
- IFlashcardGenerationService - AI-powered flashcard creation
- IEssayDraftService - Essay outline and structure generation
- IWorkspacePreparationService - Desktop orchestration
- IProductivityMetricsService - Comprehensive metrics tracking
- IPersonalizationService - User learning and adaptation

### New Database Entities

- StudentContext - Current activity state
- StudentWellnessMetrics - Wellness tracking
- AssignmentContext - Assignment details from Canvas
- GeneratedFlashcard - AI-generated study cards
- EssayDraft - Auto-generated essay outlines
- FocusSessionMetrics - Focus session tracking
- StudentPreferences - Personalization settings

### Background Jobs

- BurnoutCheckJob - Every 2 hours
- CanvasPollingJob - Every 15 mins
- ContentGenerationJob - On-demand
- MetricsCalculationJob - Daily at 6pm

---

##  Deliverables Summary

| Phase | Feature | Status |
|-------|---------|--------|
| 1 | Activity Detection (Win + Linux + Mobile) |  |
| 1 | Context Aggregation Service |  |
| 2 | Burnout Analysis & Prevention |  |
| 2 | Wellness Metrics Dashboard |  |
| 3 | Notification Filter Engine |  |
| 3 | Notification Preferences UI |  |
| 4 | Flashcard Auto-Generation |  |
| 4 | Essay Draft Generation |  |
| 4 | Study Guide Generator |  |
| 5 | Workspace Preparation Service |  |
| 5 | Enhanced Focus Mode |  |
| 6 | Productivity Metrics Dashboard |  |
| 6 | Personalization Learning Engine |  |

---

##  Success Metrics (By End of Phase 6)

-  90%+ activity detection accuracy
-  <10% burnout alert false positives
-  80%+ user satisfaction (NPS >50)
-  40% reduction in student study prep time
-  30% improvement in focus session quality
-  0 accidental auto-submissions (100% draft review)

---

##  Timeline

- Weeks 1-4: Activity Detection Foundation
- Weeks 5-8: Burnout Prevention
- Weeks 9-12: Notification Management
- Weeks 13-20: Content Generation (Flashcards + Essays)
- Weeks 21-24: Workspace Automation
- Weeks 25-26: Metrics & Polish

**Target Ship Date:** ~Mid-January 2026

---

##  Technical Risks & Mitigation

| Risk | Mitigation |
|------|-----------|
| Canvas API rate limits | Queue system + caching |
| Cross-platform activity detection | Heavy testing per platform |
| AI draft quality issues | Hybrid human review + training |
| Burnout detection false alerts | Conservative thresholds + tuning |
| Performance impact | Offload to background jobs + Redis |

---

##  Priority Ranking

### Must-Have (MVP)
1. Activity Detection (Foundation)
2. Burnout Prevention (Safety)
3. Notification Filtering (UX)
4. Flashcard Generation (High ROI)

### Should-Have (Phase 2)
5. Essay Drafts
6. Workspace Prep
7. Metrics Dashboard

### Nice-to-Have (Future)
8. Personalization Learning
9. Study Guide Generation
10. Advanced AI features

---

**Next Step:** Create BMAD stories for Phase 1 and kick off Activity Detection service development.
