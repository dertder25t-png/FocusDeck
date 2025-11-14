# FocusDeck + Jarvis: Execution Roadmap (AI-First ✅ Checklists)

**Timezone:** America/Chicago  \
**Server:** ASP.NET Core  \
**UI:** Vite/React  \
**Clients:** WPF (.NET 9), MAUI (.NET 9)

---

## Status Snapshot

- [x] Phase 0.1: Legacy SPA route work is ready—Vite base `/` plus `BuildSpa` hook exist and old `wwwroot/app` assets were removed (placeholder `.gitkeep` holds the root while release builds copy `dist`).
- [x] Phase 0.2: `AutomationDbContext` owns the schema, migrations point to `InitialCanonicalSchema`, and there is no manual DDL in `Program.cs`.
- [x] Phase 0.3: Desktop, Web dev proxy, and MAUI clients all target `http://localhost:5000` and `.NET 9` where applicable.
- [x] Phase 0.4: CI produces a single `focusdeck-server-with-spa` artifact that stitches WebApp output and server builds into one deployable.
- [ ] Phase 1: Foundations are ready—multi-tenant plumbing is wired (null tenant default, factory coverage, stubbed tenant membership for auth tests) so focus can shift to tenant-aware APIs/UI and the `/` SPA launch on Linux.

## Verifications

- [x] `npm run build` (WebApp) succeeds with the new QR/canvas types; Vite reports a large chunk warning but finishes.
- [x] `dotnet build FocusDeck.sln -c Release` (passes with the known warnings in LectureIntegrationTests, RemoteControlIntegrationTests, and AssetIntegrationTests).

## Phase 0 — Stabilize & Unify (Sprint 1–2)

**Goal:** One clean build that runs everywhere; no legacy UI; schema owned by EF; consistent ports.

### 0.1 Clean out legacy UI & route the SPA at /

**Why:** The current `wwwroot/app` + SPA base causes `http://localhost:5000/app/app/notes`. We want `/ →` SPA and a single history fallback.

- [x] Move SPA output to root: set Vite base: "/" (already configured); release `BuildSpa` copies `dist` → `src/FocusDeck.Server/wwwroot/`
- [x] Delete legacy files: `src/FocusDeck.Server/wwwroot/app/**`, any `index.html`, `app.js`, `styles.css` left from old UI (the `app` tree is gone and `.gitkeep` holds the root directory)
- [x] Server routing (`Program.cs`):

  ```csharp
  static bool ShouldServeSpa(HttpContext ctx, bool includeRoot)
  {
      var path = ctx.Request.Path.Value ?? string.Empty;
      var isRoot = string.IsNullOrEmpty(path) || string.Equals(path, "/", StringComparison.OrdinalIgnoreCase);

      if (ctx.Request.Path.StartsWithSegments("/v1") ||
          ctx.Request.Path.StartsWithSegments("/hubs") ||
          ctx.Request.Path.StartsWithSegments("/swagger") ||
          ctx.Request.Path.StartsWithSegments("/healthz") ||
          ctx.Request.Path.StartsWithSegments("/hangfire") ||
          path.StartsWith("/app", StringComparison.OrdinalIgnoreCase))
      {
          return false;
      }

      if (isRoot)
      {
          return includeRoot;
      }

      return !Path.HasExtension(path);
  }

  app.UseDefaultFiles();
  app.UseStaticFiles();

  app.MapWhen(ctx => ShouldServeSpa(ctx, includeRoot: false),
      spa => spa.Run(async c => await c.Response.SendFileAsync("wwwroot/index.html")));

  app.MapFallback("/app/{*path}", (HttpContext http, string? path) =>
  {
      var normalized = (path ?? string.Empty).Trim('/');
      return Results.Redirect(string.IsNullOrEmpty(normalized) ? "/" : $"/{normalized}", permanent: true);
  });
  ```

- [x] Build hook (Server `.csproj`): restore `BuildSpa` target to run `npm ci && npm run build` in `src/FocusDeck.WebApp`, output → `wwwroot/` (the target already exists)
- [x] Verify: `http://localhost:5000/` loads SPA; deep-links like `/notes` refresh correctly, and legacy `/app/*` URLs now redirect to `/`.

**Files to touch**

- `src/FocusDeck.WebApp/vite.config.ts`
- `src/FocusDeck.Server/Program.cs`
- `src/FocusDeck.Server/FocusDeck.Server.csproj`
- `src/FocusDeck.Server/wwwroot/` (remove legacy; receive SPA)

### 0.2 One canonical EF schema (no manual SQL)

**Why:** Manual `CREATE TABLE IF NOT EXISTS` drifts from EF; breakage later.

- [x] Remove any manual schema DDL in `src/FocusDeck.Server/Program.cs` (no raw SQL remains)
- [x] Ensure `DbSets` exist in `AutomationDbContext` for auth/sync/refresh-tokens/etc.
- [x] Reset migrations (create a single clean `InitialCanonicalSchema`)
- [ ] Apply: `dotnet ef database update`
- [ ] Health check: server boots; basic endpoints respond 401/200 (not 500)

**Files**

- `src/FocusDeck.Persistence/AutomationDbContext.cs`
- `src/FocusDeck.Persistence/Migrations/**`
- `src/FocusDeck.Server/Program.cs`

### 0.3 Dev environment ports + clients

**Why:** Desktop was pointed to `:5239`. Standardize on `:5000`.

- [x] Desktop `App.xaml.cs` → base API `http://localhost:5000`
- [x] WebApp dev proxy → `5000`
- [x] MAUI targets .NET 9

**Files**

- `src/FocusDeck.Desktop/App.xaml.cs`
- `src/FocusDeck.WebApp/vite.config.ts`
- `src/FocusDeck.Mobile/FocusDeck.Mobile.csproj`

### 0.4 CI builds a single deployable

**Why:** Artifacts must include server and SPA.

- [ ] GitHub Actions: `actions/setup-node` + `npm ci` + `npm run build` (WebApp)
- [ ] Run `dotnet build -c Release` (server `BuildSpa` hook)
- [ ] Assert `src/FocusDeck.Server/wwwroot/index.html` exists (CI check)

**Files**

- `.github/workflows/build-server.yml`
- `src/FocusDeck.Server/FocusDeck.Server.csproj`

---

## Phase 1 — SaaS Foundation + Auth UI + URL Fixes (Sprint 3–4)

**Goal:** Multi-tenancy; PAKE login/registration/pairing UIs on Web/Desktop/Mobile; SPA serves at `/` in Linux; Cloud/Tunnel ingress stable.

### 1.1 Multi-Tenancy (backend)

- [x] Add entities: `Tenant`, `UserTenant`; add `TenantId` to `IMustHaveTenant` models
- [x] JWT: include `app_tenant_id` claim
- [x] Global query filter (reads `app_tenant_id`)
- [x] Stamp `TenantId` on writes in `SaveChangesAsync`
- [x] Audit every `IMustHaveTenant` entity (review EF configs/migrations) to prove `TenantId` is auto-set and logs capture tenant+user for tenant switches.
- [x] Tenant audit table now logs operations for every `IMustHaveTenant` entity when the context saves changes, so you can inspect writes by tenant.
- [x] Capture Linux `/` routing + audit (Nginx/Cloudflare configs, history fallback) with a doc snippet showing how to verify deep links (`src/FocusDeck.Server/Program.cs`, `docs/CLOUDFLARE_DEPLOYMENT.md`).
- [x] `/v1/tenants/current` summary + `/v1/tenants/{id}/switch` APIs so clients can refresh tenant context and request new tokens without re-login

**Files**

- `src/FocusDeck.Domain/Entities/*`
- `src/FocusDeck.Persistence/AutomationDbContext.cs`
- `src/FocusDeck.Server/Services/Auth/TokenService.cs`

### 1.2 Authentication UI (all clients)

**Web**

- [x] `/login`, `/register`, `/pair` (PAKE start/finish; store tokens; `ProtectedRoute`; see `AuthPakeController`, `src/FocusDeck.WebApp/src/lib/pake.ts`, `KeyProvisioningService`)
- [x] Login system validated end-to-end (fresh DB register + login + tenant claim confirmed)
- [ ] Files:
  - `src/FocusDeck.WebApp/src/pages/LoginPage.tsx`
  - `src/FocusDeck.WebApp/src/pages/ProvisioningPage.tsx`
  - `src/FocusDeck.WebApp/src/pages/PairingPage.tsx`
  - `src/FocusDeck.WebApp/src/lib/pake.ts`
  - `src/FocusDeck.WebApp/src/hooks/useCurrentTenant.ts`
  - [x] Surface tenant context in the web shell via `/v1/tenants/current` so users can see the active workspace and jump to the Tenants page.
  - [x] Allow switching tenants directly from the Tenants page (`/v1/tenants/{id}/switch`) to refresh tokens and tenant context.

**Desktop/Mobile**

- [x] Desktop: `OnboardingWindow` → `KeyProvisioningService` (PAKE flows + tenant refresh wired to `/v1/auth/pake`)
- [x] Desktop: `KeyProvisioningService` now exposes tenant context (`CurrentTenantDto`) and raises updates so the shell can show the current workspace after login.
- [x] Mobile: provisioning page now subscribes to tenant summary updates exposed by `IMobileAuthService`, so the active tenant name/slug appears on-device after login.
- [x] Mobile: Provisioning + QR pairing (claim code → tokens) with `MobilePakeAuthService` + `ProvisioningPage`
- [ ] Files:
  - `src/FocusDeck.Desktop/Views/OnboardingWindow.xaml(.cs)`
  - `src/FocusDeck.Desktop/Services/Auth/KeyProvisioningService.cs`
  - `src/FocusDeck.Mobile/.../ProvisioningViewModel.cs`

### 1.3 Linux web server URL cleanup (what you asked)

- [x] Remove old UI from `wwwroot/app/**` on server
- [x] Nginx/Cloudflare (if used) route `/` → Kestrel `:5000` (no subpath) (see `docs/CLOUDFLARE_DEPLOYMENT.md`)
- [x] Verify deep-links: `/notes`, `/lectures/123` load on refresh (`src/FocusDeck.Server/Program.cs` history fallback)
- [x] Fix SPA base to `/` (no `/app`), avoid `/app/app/...` paths (`src/FocusDeck.WebApp/vite.config.ts`)

**Files**

- `src/FocusDeck.Server/wwwroot/**`
- `src/FocusDeck.WebApp/vite.config.ts`
- Infra (nginx/cloudflared) config in your Linux host

### 1.4 UX pass (first cut)

- [x] Desktop: onboarding flow (auth), main shell nav, status bar for connection/JWT/tenant (`ShellWindow`, `OnboardingWindow`)
- [x] Web: clean top-nav, Notes/Lectures/Courses list pages wired; empty states (`AppLayout`, page components)
- [x] Mobile: login & "quick actions" (Start Note, Pair Device) (`CommandDeckPage`, provisioning/pairing screens)

### Phase 1 execution focus

Use this mini-plan to steer Sprint 3–4 work now that Phase 0 plumbing is stable.

1. **Multi-tenant infrastructure**  
   - Add `TenantId` to every `IMustHaveTenant` entity and persist it on `SaveChangesAsync` so writes are annotated automatically (`AutomationDbContext` + `domain entities`).  
   - Inject `app_tenant_id` into JWTs while also reading it in the global query filter so every API call scopes data (`TokenService`, `AutomationDbContext`).  
   - Build tenant bootstrapping stories in `TenantMembershipService` so onboarding/login tracks a `Tenant`/`UserTenant` pair for every user or device.

2. **Authentication surfaces**  
   - Wire `/login`, `/register`, `/pair` in the WebApp with PAKE flows and a `ProtectedRoute` wrapper; share the `pake.ts` logic between desktop/mobile to minimize duplication.  
   - Update Desktop `OnboardingWindow` + `KeyProvisioningService` and Mobile provisioning view model to hit the new endpoints and store tenant-scoped tokens.  
   - Surface tenant information (name, slug) in the UI status bars/sidebars once authentication succeeds.

3. **Linux SPA deployment readiness**  
   - Confirm Linux/Nginx routes `/` → Kestrel `:5000`, remove `/app` assets, and verify deep-link refreshes using the existing history fallback.  
   - Document the Linux host configuration (Nginx, Cloudflare rules, ports) in an infra note so the team can reproduce the `/` behavior.

---

## Phase 1.5 — Jarvis Contextual Learning Loop (Sprint 4.5)

**Goal:** Elevate Jarvis from scripted executor to adaptive assistant that learns from context while preserving privacy and respecting device resources.

### 0. Privacy & User Controls (Pre-flight)

- [ ] Build `PrivacyService` with anonymization tiers (`Low | Medium | High`) and ensure every capture hook consults `PrivacyService.IsEnabled(contextType)`.
- [ ] Implement consent dashboard (Web + Desktop) to toggle capture types (e.g., `ActiveWindowTitle`, `TypingVelocity`, `MouseEntropy`, `PhysicalLocation`) and provide live preview, delete, export, and disable controls.
- [ ] Gate all snapshot and feedback pipelines behind verified consent; no contextual data leaves a device until privacy checks pass.

### 1. Context Snapshot Infrastructure

- [ ] Add `ContextSnapshot` entity (`Id`, `UserId`, `TenantId`, `EventType`, `Timestamp`, `ActiveApplication`, `ActiveWindowTitle`, `CalendarEventId`, `CourseContext`, `MachineState`).
- [ ] Introduce client capture hooks for events (`NoteStarted`, `NoteStopped`, `WorkflowStarted`, `WorkflowCompleted`, `FocusModeEntered`, `FocusModeExited`, `AppFocused`, `BrowserTabActive`, `CalendarEventStarted`).
- [ ] Clients publish snapshot events to an in-memory/redis channel; create `SnapshotIngestService` (`IHostedService`) to batch ingest into `Jarvis.ContextSnapshots` every 30 s and expose `/v1/jarvis/snapshots` for admin/debug.
- [ ] Add `ContextAggregator` (Rx.NET) to merge events in 30 s windows before persistence.

### 2. On-Device Feature Engineering & Enhanced Depth

- [ ] Extend clients and bridges (Desktop system tray monitor, VS Code extension, Browser Bridge) to enrich snapshots with behavioral metrics (`TypingVelocity`, `MouseEntropy`, `ContextSwitchCount`) and environment signals (`DevicePosture`, `AudioContext`, `PhysicalLocation`).
- [ ] Implement local aggregation so raw keystroke/mouse data stays on device; send `FeatureSummary` payloads with optional `ApplicationStateDetails` JSON blob.
- [ ] Ensure capture SDK respects device power profiles—offer adaptive sampling rates to minimize CPU/battery on laptops while scaling up on high-performance desktops.

### 3. Real-Time Vectorization & Storage

- [ ] Deploy pgvector (preferred) or Qdrant alongside PostgreSQL and create `ContextVectors` tables for behavioral, temporal, and project embeddings.
- [ ] Queue `VectorizeSnapshotJob` whenever a snapshot is persisted (and on feedback updates) to compute embeddings (`all-MiniLM-L6-v2` or ML.NET equivalent) and update indexes within seconds.
- [ ] Track queue throughput/lag metrics and schedule cleanup jobs for expired snapshots.

### 4. Suggestion APIs & Explainability

- [ ] Implement `/v1/jarvis/suggest` with a rule-based MVP, then upgrade to vector-driven retrieval via `VectorSearchService`.
- [ ] Integrate MCP Gateway tool (`jarvis.analyze_context`) to allow LLM reasoning over layered context.
- [ ] Return `{ action, parameters, confidence, evidence[] }` payloads and surface "Why?" UI that fetches referenced snapshot summaries.

### 5. Feedback & Reinforcement Loop

- [ ] Add `/v1/jarvis/feedback { snapshotId, reward }` API and `Jarvis.FeedbackSignals` storage for explicit (👍/👎) and implicit (completion rate, dwell time) rewards.
- [ ] Implement `ImplicitFeedbackMonitor` to infer rewards and trigger snapshot re-vectorization with decayed weighting (recent signals ×10).
- [ ] Introduce Thompson Sampling (or similar) bandit policy to adapt recommendations based on cumulative rewards.

### 6. Layered Context Builder & Prompt Templates

- [ ] Build `LayeredContextService` to compose Immediate → Session → Project → Seasonal context layers and register prompt templates with the MCP Gateway.
- [ ] Generate dynamic few-shot examples via `ExampleGenerator` (top three similar events) and enforce confidence thresholds (`<0.7 → insufficient_context`).

### 7. Performance & Resource Adaptation

- [ ] Provide Jarvis runtime with real-time device capability profiles (CPU class, battery level, thermal state) and adapt workflow selection, concurrency, and automation intensity accordingly.
- [ ] Expose settings allowing users to set "Eco", "Balanced", or "Performance" modes, ensuring laptops remain power efficient while desktops can run heavier automations.

### 8. Testing, Validation & Metrics

- [ ] Simulate ≥100 contexts across personas (e.g., "CS Student", "Designer", "Gamer") to benchmark precision/recall against Jarvis Classic.
- [ ] Implement adversarial testing to catch false positives and monitor dashboards for precision, recall, reward curves, and vectorization lag.
- [ ] Store successful workflow runs as training signals and ensure MCP audit logs correlate tool calls with context evidence.

### Deliverables

- ✅ Privacy & consent dashboard live before capture hooks
- ✅ Decoupled snapshot ingestion with on-device feature summaries
- ✅ Real-time vector index + embedding pipeline with explainable suggestions
- ✅ `/v1/jarvis/suggest` + `/v1/jarvis/feedback` APIs wired to MCP Gateway
- ✅ Performance-aware Jarvis runtime configuration (Eco/Balanced/Performance)
- ✅ Learning metrics and validation dashboards (precision/recall/reward)

---

## Phase 2 — Observability + MCP Server/Gateway (Sprint 5)

**Goal:** Production-grade telemetry and the MCP tool plane so LLMs can safely call your app’s tools.

### 2.1 Observability

- [ ] OpenTelemetry traces → OTLP exporter (collector/Tempo/Jaeger)
- [ ] Metrics → Prometheus (`/metrics`)
- [ ] Serilog → OTLP/Seq sink
- [ ] Minimal dashboards (RPS, 4xx/5xx, job failures)

**Files**

- `src/FocusDeck.Server/Program.cs`
- `src/FocusDeck.Server/appsettings*.json`
- `src/FocusDeck.Server/FocusDeck.Server.csproj`

### 2.2 MCP Server + Gateway (your definition: LLMs can grab resources/APIs as tools)

**Concept:** Expose capabilities (e.g., “list lectures”, “create note”, “queue Jarvis workflow”) behind a Model Context Protocol server. A gateway brokers between multiple MCP servers (FocusDeck, Google, Canvas, Home Assistant), giving Copilot/GPT a single toolbelt.

- [ ] Define tool contracts (`/mcp/tools/*.json`) for: `notes.create`, `notes.find`, `lectures.uploadAudio`, `jarvis.runWorkflow`, `calendar.getNowClass`, `projects.saveState`, `browserBridge.openTabs` (proxy), etc.
- [ ] Implement MCP server in FocusDeck (WebSocket/HTTP) that exposes these tools with auth (tenant-scoped)
- [ ] Docker MCP Gateway (off-box) that aggregates: `focusdeck-mcp`, `google-mcp`, `canvas-mcp`, `homeassistant-mcp`
- [ ] Client configs (Copilot/Codex/Gemini): point to gateway; pass user identity/tenant in headers

**Files**

- `tools/mcp/focusdeck/*.ts|cs` (server handlers)
- `tools/mcp/gateway/docker-compose.yml`
- `docs/MCP_TOOLS.md` (contract examples, auth rules)

---

## Phase 3 — Jarvis (API + Runner + SignalR Bus) (Sprint 6–7)

**Goal:** End-to-end Jarvis slice, feature-gated, tenant-aware.

### 3.1 Jarvis API & registry

- [x] `GET /v1/jarvis/workflows` → scan `bmad/jarvis/workflows/**`
- [x] `POST /v1/jarvis/run-workflow` → enqueue Hangfire job, return `runId`
- [x] `GET /v1/jarvis/runs/{id}` → status/logs

**Files**

- `src/FocusDeck.Server/Controllers/v1/JarvisController.cs`
- `src/FocusDeck.Server/Services/Jarvis/JarvisWorkflowRegistry.cs`
- `src/FocusDeck.Contracts/DTOs/JarvisDto.cs`

### 3.2 Hangfire job runner + outputs → SignalR actions

- [x] Job `JarvisWorkflowJob` runs Jarvis workflows via a Hangfire job and updates status/logs (Phase 3.2 stub – external script invocation can be extended in later phases).
- [x] Persist `JarvisWorkflowRun` entity; update status/logs
- [x] Parse workflow outputs → dispatch via `NotificationsHub` (Phase 3.2 uses `JarvisRunUpdated` notifications; richer remote actions can follow in later phases).
- [x] Clients (Desktop/Mobile) implement the listener and perform actions (show toast, start/pause, open URL, etc.)

**Files**

- `src/FocusDeck.Server/Jobs/JobImplementations.cs`
- `src/FocusDeck.Domain/Entities/JarvisWorkflowRun.cs`
- `src/FocusDeck.Shared/SignalR/Notifications/INotificationClientContract.cs`
- Clients’ SignalR services

### 3.3 Jarvis UI (Web)

- [x] `/jarvis` page: list workflows, run, see run status/logs
- [x] Feature flag `"Features:Jarvis"`: false by default; enable canary

---

## Phase 4 — Auto-Tag Notes to Class (GCal) + Calendar Planner (Sprint 8)

**Goal:** When you start writing/recording, the note auto-attaches to the current class from Google Calendar.

### Activity Signals – Burnout & context telemetry

- Ingest UI/agent telemetry via `POST /v1/activity/signals` (v1 API, `[Authorize]`).
  Each payload carries `SignalType`, `SignalValue`, `SourceApp`, optional `MetadataJson`, and `CapturedAtUtc`.
- Persists the new `ActivitySignal` entity (`Id`, `TenantId`, `UserId`, `SignalType`, `SignalValue`, `SourceApp`, `MetadataJson`, `CapturedAtUtc`), indexed by `TenantId` + `CapturedAtUtc` so we can efficiently trend per-tenant/time window.
- `/jarvis` exposes an “Emit sample activity signals” button (guarded by `Features:Jarvis`) that posts fake `TypingBurst`/`ActiveWindow` signals to the ingestion API, helping QA verify the pipeline without production sensors.
- This telemetry stream seeds the upcoming burnout/autotagging work so we can detect typing bursts, active windows, and other context signals before hooking real clients.

### 4.1 Google OAuth + incremental sync (+ push optional)

- [ ] Scopes: `userinfo.email`, `calendar.readonly` (offline access → refresh token)
- [ ] Entities: `CalendarSource`, `EventCache`, `CourseIndex`
- [ ] Job: `CalendarWarmSyncJob` (every 30m) next 14 days (incremental with `syncToken`)
- [ ] Optional: watch + webhook for near-real-time

### 4.2 Resolver logic (server)

- [ ] Window `now − 15m … now + 10m` in user TZ
- [ ] Rank: ongoing > imminent > recent; score by course code/keywords/recurrence/primary calendar
- [ ] Attach: set `note.courseId`, `note.eventId`, title `"{Course} – {Topic} – {Date Time}"`
- [ ] Fallbacks: “Unassigned” + top-3 picker from time-pattern

**Files**

- `src/FocusDeck.Server/Controllers/v1/NotesController.cs` (e.g., `/v1/notes/start`)
- `src/FocusDeck.Server/Services/Calendar/CalendarResolver.cs`
- `src/FocusDeck.Domain/Entities/*` (`CalendarSource`, `EventCache`, `NoteSession`, `CourseIndex`)

---

## Phase 5 — Device Agent + Job Bundles (Sprint 9–10)

**Goal:** Server plans; device acts. Prepare class layouts/jobs on server; execute instantly when laptop/phone connects.

### 5.1 Job schema & agent skeleton (Windows, later Android)

- [ ] JSON job bundle (idempotent steps, `expires_at`, safety preview)
- [ ] Windows agent: service + WebSocket client; capabilities advertised to server
- [ ] Skills: `arrange_layout`, `open_url`, `open_notes_page`, `arm_audio_recording`, `show_pill`
- [ ] Server endpoint: queue jobs to device; status graph (queued → dispatched → done/failed)

**Files**

- `agents/windows/**`
- `src/FocusDeck.Server/Controllers/v1/AgentController.cs`
- `src/FocusDeck.Server/Services/Jobs/**`

---

## Phase 6 — Browser Bridge + Memory Vault + Project Memory (Sprint 11–12)

**Goal:** Capture dev research, AI chats, and tab context into a personal knowledge notebook and recoverable states.

### 6.1 Browser Bridge (extension + bridge API)

- [ ] Capture open tabs, detect AI chat pages, long code blocks
- [ ] Commands: open tabs set, close aged, screenshot page, send content to FocusDeck
- [ ] Link captured items to active project/code path

### 6.2 AI Memory Vault + Project Timeline

- [ ] Store meaningful AI chats (title, tags, gist, links)
- [ ] Auto-summarize research queries into “Developer Knowledge Notebook”
- [ ] Show “last next step” reminder when you revisit code area

**Files**

- `extensions/browser/**`
- `src/FocusDeck.Server/Controllers/v1/MemoryController.cs`
- `src/FocusDeck.Domain/Entities/KnowledgeItem.cs`

---

## Phase 7 — “Mental Save State” + Adaptive Layout Intelligence (Sprint 13–14)

**Goal:** One hotkey to save/restore whole-OS working sets; auto-apply preferred layouts per activity.

- [ ] Save snapshot: windows, tabs, current notes, audio timestamp, current task
- [ ] Restore snapshot; nightly “Park Today” + morning “Restore Suggestion”
- [ ] Learn per-course/per-project layout; apply automatically with soft preview & Undo

**Files**

- `agents/windows/skills/**`
- `src/FocusDeck.Server/Services/Snapshots/**`
- `src/FocusDeck.WebApp/src/pages/Workspaces/**`

---

## Per-Phase “HOW TO BUILD” cards (for Copilot/Codex)

> Drop these mini-cards into issues. They’re terse on purpose so LLMs act directly.

### Card: Fix SPA at / (Phase 0.1)

**Intent:** Serve Vite app from `/`, remove `/app` prefix duplication.  
**Touch:** `vite.config.ts`, `Program.cs`, `FocusDeck.Server.csproj`, `wwwroot/**`

**Steps:**

1. Vite base: "/". 2) Build to dist, copy → `wwwroot/`.
2. Enable `UseDefaultFiles`/`UseStaticFiles`. 4) Add history fallback.
3. Delete legacy `wwwroot/app/**`.

**Done-when:** `/notes` refresh works; no `/app/app`.  
**Risks:** Nginx/Cloudflare misroutes → ensure root path.  
**Tests:** Curl `/`, `/notes` (200 OK, returns SPA HTML).

### Card: MCP Server + Gateway (Phase 2.2)

**Intent:** Give LLMs controlled tools into FocusDeck + external APIs.  
**Touch:** `tools/mcp/**`, `docs/MCP_TOOLS.md`

**Steps:**

1. Define JSON tool specs for notes/lectures/jarvis/calendar.
2. Implement MCP server (authZ by tenant).
3. Spin gateway (Docker) aggregating multiple MCP servers.
4. Configure Copilot/GPT to point at gateway with user headers.

**Done-when:** `mcp:list_tools` shows FocusDeck tools; `notes.create` works.  
**Risks:** tool auth; rate limits.  
**Tests:** E2E: run `jarvis.runWorkflow` via MCP; see job queued.

### Card: Auto-tag notes from Calendar (Phase 4)

**Intent:** Attach new notes/recordings to the right class automatically.  
**Touch:** `CalendarResolver.cs`, `NotesController.cs`, entities.

**Steps:**

1. OAuth + refresh token storage. 2) Warm sync event cache (14d).
2. Resolver: window `now−15m..now+10m`; score & choose event.
3. `/v1/notes/start` calls resolver; updates title/tags.

**Done-when:** Starting a note during class auto-labels it.  
**Risks:** DST, overlapping events.  
**Tests:** Simulate 4 classes/day, 100 starts; ≥95% correct.

---

## Acceptance Gates (what “done” really means)

- **P0 Gate:** `dotnet build -c Release` produces server + SPA at root; desktop connects to `:5000`; EF canonical migration applies cleanly.
- **P1 Gate:** Multi-tenant reads/writes; Web/Desktop/Mobile can login/register/pair; Linux deploy serves SPA at `/`.
- **P1.5 Gate:** Privacy dashboard enabled; contextual snapshots captured with consent; `/v1/jarvis/suggest` returns explainable, resource-aware recommendations with feedback loop online.
- **P2 Gate:** OTLP traces + Prom metrics + central logs visible; MCP tools usable from Copilot/GPT.
- **P3 Gate:** `/v1/jarvis/*` live; job runs BMAD; clients receive SignalR actions.
- **P4 Gate:** Notes start auto-attach to class with ≥95% accuracy in tests.
- **P5–P7 Gates:** Device agent executes bundles on connect; Memory Vault & Save States usable.

---

## “Quick-add” Issue Titles (copy these)

- [ ] `[WEB] Serve SPA at root (fix /app duplication)`
- [ ] `[SERVER] History fallback for SPA routes`
- [ ] `[SERVER] Canonical EF migration; remove manual DDL`
- [ ] `[DESKTOP] BaseAddress = http://localhost:5000`
- [ ] `[MOBILE] Target .NET 9`
- [ ] `[OBS] OTLP+Prom+Serilog production pipeline`
- [ ] `[MCP] FocusDeck MCP server + gateway`
- [ ] `[JARVIS] Contextual learning loop (privacy, snapshots, suggest API)`
- [ ] `[JARVIS] API + Hangfire runner + SignalR dispatch`
- [ ] `[CAL] Google Calendar warm sync + resolver`
- [ ] `[AGENT] Windows agent skeleton + skills`
- [ ] `[BRIDGE] Browser extension + memory vault`
- [ ] `[WORKSPACE] Mental save state + adaptive layouts`

---

## Final UI/UX Note (what to design at the end of P1)

- **Desktop:** Onboarding/login, left nav (Notes/Lectures/Courses), status bar, “Start Note” primary CTA.
- **Web (Linux server):** Clean top bar, Notes list with course chips, `/jarvis` gated page, deep-link routing tested at root.
- **Android:** Login + Quick Actions (“Start Note”, “Pair Device”), minimal list screens to verify auth + API.

---

With this roadmap committed, the next action item is to begin **Phase 1** execution once Phase 0 stabilization tasks are delivered and validated.
