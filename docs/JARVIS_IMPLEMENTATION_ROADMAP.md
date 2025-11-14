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

##  Jarvis API (Phase 3.1) – Server Slice

**Goal:** Introduce a stable, feature-gated Jarvis API surface without executing real workflows yet.

### Endpoints

- `GET /v1/jarvis/workflows`
  - Auth: `[Authorize]` – requires a valid JWT with tenant context.
  - Behavior (Phase 3.1): returns an empty list until `JarvisWorkflowRegistry` is wired to scan `bmad/jarvis/workflows/**`. In the current implementation, `ListWorkflowsAsync` scans `bmad/jarvis/workflows/**/workflow.yaml` for real workflow metadata and returns it.
- `POST /v1/jarvis/run-workflow`
  - Auth: `[Authorize]`.
  - Request: `JarvisRunRequestDto { workflowId, argumentsJson? }`.
  - Behavior (Phase 3.1): validates `workflowId`, logs the request, and returns a synthetic `runId` without enqueuing a real job.
- `GET /v1/jarvis/runs/{id}`
  - Auth: `[Authorize]`.
  - Behavior (Phase 3.1): returns a stub `JarvisRunStatusDto` with `Status = "Pending"` and a short summary explaining that the runner is not yet wired.

### Feature Flag

- All Jarvis endpoints are gated by `Features:Jarvis` (bool) in configuration.
  - When `Features:Jarvis = false`, each endpoint returns `404 Not Found` with `error = "Jarvis feature is disabled."`.
  - When `Features:Jarvis = true`, the endpoints behave as described above.

### Implementation Notes

- Controller: `src/FocusDeck.Server/Controllers/v1/JarvisController.cs`
  - Decorated with `[ApiController]`, `[ApiVersion("1.0")]`, `[Route("v{version:apiVersion}/jarvis")]`, `[Authorize]`.
  - Reads `Features:Jarvis` via `IConfiguration` to guard each action.
- Service: `src/FocusDeck.Server/Services/Jarvis/JarvisWorkflowRegistry.cs`
  - `IJarvisWorkflowRegistry.ListWorkflowsAsync()` – scans `bmad/jarvis/workflows/**` for `workflow.yaml` files and maps their `name`/`description` fields into `JarvisWorkflowSummaryDto`.
  - `EnqueueWorkflowAsync()` – creates a `JarvisWorkflowRun`, enqueues a `JarvisWorkflowJob`, and returns a real `RunId`.
  - `GetRunStatusAsync()` – returns a database-backed `JarvisRunStatusDto` with the latest status and summary.
- Contracts: `src/FocusDeck.Contracts/DTOs/JarvisDto.cs`
  - Stable DTOs for workflows, run requests, and run status, intended to remain compatible as Phase 3.2 adds Hangfire + SignalR and persistence.

---

##  Phase 3.2 – Run Persistence & Job Execution

**Goal:** Promote Jarvis from a pure stub to a minimal but real execution pipeline that can queue and track workflow runs.

### New Entity

- `JarvisWorkflowRun` (`src/FocusDeck.Domain/Entities/JarvisWorkflowRun.cs`)
  - Fields: `Id`, `WorkflowId`, `Status`, `RequestedAtUtc`, `StartedAtUtc`, `CompletedAtUtc`, `TenantId`, `RequestedByUserId`, `LogSummary`, `JobId`.
  - Implements `IMustHaveTenant` so `AutomationDbContext` stamps `TenantId` and query filters scope reads by tenant.
  - EF configuration: `JarvisWorkflowRunConfiguration` (`JarvisWorkflowRuns` table) with indexes on `TenantId`, `WorkflowId`, `Status`, `RequestedAtUtc`.

### Hangfire Job

- `JarvisWorkflowJob` (`src/FocusDeck.Server/Services/Jarvis/JarvisWorkflowJob.cs`)
  - Method: `ExecuteAsync(Guid runId, CancellationToken ct)`, invoked by Hangfire with `runId`.
  - Behavior (Phase 3.2):
    - Loads the `JarvisWorkflowRun` row.
    - Transitions `Status: Queued → Running → Succeeded` (or `Failed` on exception/cancellation).
    - Updates `StartedAtUtc` / `CompletedAtUtc` and writes a short `LogSummary` such as “Jarvis workflow executed successfully (stub).”
    - Does not yet call `bmad.ps1` or any external script; this will be added in Phase 3.3 (SignalR + real execution).

### Registry Wiring

- `JarvisWorkflowRegistry` now uses:
  - `AutomationDbContext` to persist `JarvisWorkflowRun`.
  - `IBackgroundJobClient` to enqueue `JarvisWorkflowJob`.
  - `IHttpContextAccessor` to capture `RequestedByUserId` from the current principal.
- `EnqueueWorkflowAsync`:
  - Validates `WorkflowId`.
  - Creates a `JarvisWorkflowRun` with `Status = "Queued"`, stamps timestamps and user.
  - Enqueues `JarvisWorkflowJob.ExecuteAsync(run.Id, ...)` via Hangfire and stores `JobId`.
  - Returns `JarvisRunResponseDto` with the new `RunId`.
- `GetRunStatusAsync`:
  - Reads the run from the database and maps canonical fields into `JarvisRunStatusDto { RunId, Status, LogSummary }`.

### API Flow (Server Only)

- `POST /v1/jarvis/run-workflow`
  - Creates a `JarvisWorkflowRun` row and enqueues the Hangfire job.
  - Returns `runId` immediately.
- `GET /v1/jarvis/runs/{id}`
  - Reflects the current status from the database (`Queued`, `Running`, `Succeeded`, `Failed`) plus the last `LogSummary`.
  - Future phases will add richer logs and SignalR notifications to push updates to clients.

---

##  Phase 3.3 – SignalR + Web UI

**Goal:** Surface Jarvis run status live to the user using the existing SignalR hub and a minimal `/jarvis` page.

### SignalR Notifications

- Contract:
  - `JarvisRunUpdate` (shared) – `RunId`, `WorkflowId`, `Status`, `Summary`, `UpdatedAtUtc`.
  - Added to `INotificationClientContract` + `INotificationClient` as `Task JarvisRunUpdated(JarvisRunUpdate payload);`.
- Hub:
  - `NotificationsHub` already groups connections by user ID (`user:{userId}`).
  - `JarvisWorkflowJob` now calls `Clients.Group($"user:{RequestedByUserId}").JarvisRunUpdated(...)` after each status transition (`Running`, `Succeeded`, `Failed`).
  - Notifications are tenant-scoped via `JarvisWorkflowRun.TenantId` and only broadcast to the requesting user’s group.

### Web UI – `/jarvis` Page

- Route: `/jarvis` (protected via `ProtectedRoute` and the existing app shell).
- Feature flag:
  - The underlying API remains guarded by `Features:Jarvis`; the UI is designed to be shown only when the flag is enabled in the environment.
- Behavior:
  - Allows the user to trigger `POST /v1/jarvis/run-workflow` with a stub workflow id (e.g., `demo-focus`, `demo-notes`).
  - Subscribes to `JarvisRunUpdated` over the `NotificationsHub` connection; updates a simple in-memory list of runs:
    - Columns: `WorkflowId`, `Status`, `Last updated`, `Summary`.
  - If SignalR is unavailable, falls back to a lightweight polling loop that calls `GET /v1/jarvis/runs/{id}` a few times after enqueue.

### Files

- Server:
  - `src/FocusDeck.Shared/SignalR/Notifications/INotificationClientContract.cs` – `JarvisRunUpdate` + `JarvisRunUpdated`.
  - `src/FocusDeck.Server/Hubs/NotificationsHub.cs` – extended typed client interface.
  - `src/FocusDeck.Server/Services/Jarvis/JarvisWorkflowJob.cs` – emits `JarvisRunUpdated` for the requesting user’s group when status changes.
- WebApp:
  - `src/FocusDeck.WebApp/src/pages/JarvisPage.tsx` – minimal run launcher + status table.
  - `src/FocusDeck.WebApp/src/App.tsx` – imports `JarvisPage` and adds the `/jarvis` route + nav entry.

---

##  Jarvis Operations

**Goal:** Give ops/QA a concise checklist for monitoring and safely rolling out Jarvis.

### Metrics & Logs

- Metrics (meter `FocusDeck.Jarvis`):
  - `jarvis.runs.started` – count of runs enqueued, tagged by `tenant_id` and `workflow_id`.
  - `jarvis.runs.succeeded` / `jarvis.runs.failed` – run outcomes, tagged by `tenant_id`, `workflow_id`, and `status`.
  - `jarvis.runs.duration.seconds` – histogram of run duration from request to completion, tagged by `tenant_id`, `workflow_id`, and `status`.
- Logs:
  - `JarvisWorkflowJob` logs state transitions (`Queued → Running → Succeeded/Failed`) with `RunId`, `WorkflowId`, `TenantId`, `RequestedByUserId`, and an error message on failure.
  - SignalR delivery issues surface as warnings when `JarvisRunUpdated` cannot be sent to the requesting user’s group.

### Rate Limits & Concurrency

- Per-user concurrency guard:
  - `JarvisWorkflowRegistry` enforces a maximum of **3 active runs** per user (`Status` in `Queued` or `Running`).
  - When the limit is exceeded, `EnqueueWorkflowAsync` throws `JarvisRunLimitExceededException` and the API returns HTTP **429 Too Many Requests** with a user-friendly error.
- Operational guidance:
  - Watch for sustained 429s on `/v1/jarvis/run-workflow` as a signal that users or automations are over-queuing work.
  - If needed, adjust the limit in `JarvisWorkflowRegistry` once real workflows and capacity are better understood.

### Feature Flag & Canary Rollout

- Server flag:
  - `Features:Jarvis` (bool) controls all Jarvis APIs on the server.
  - When `Features:Jarvis = false`:
    - `JarvisController` returns **404 Not Found** with `error = "Jarvis feature is disabled."` for all `/v1/jarvis/*` endpoints.
  - When `Features:Jarvis = true`:
    - `/v1/jarvis/workflows`, `/v1/jarvis/run-workflow`, and `/v1/jarvis/runs/{id}` are enabled for authenticated, tenant-scoped callers.
- Web UI:
  - The `/jarvis` page probes `/v1/jarvis/workflows` on load.
    - 404 → shows a clear “Jarvis is not enabled for this environment” banner and disables workflow buttons.
    - 200 → lists discovered workflows and allows runs to be triggered.
- Canary strategy:
  - Enable `Features:Jarvis` only in canary environments or appsettings overrides used by canary tenants.
  - Keep the flag **off** in general production until metrics and error rates look healthy for canaries.

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
