# FocusDeck + Jarvis: Execution Roadmap (AI-First ✅ Checklists)

**Timezone:** America/Chicago  \
**Server:** ASP.NET Core  \
**UI:** Vite/React  \
**Clients:** WPF (.NET 9)  _(Android/Mobile roadmap is tracked separately in `docs/FocusDeck_Jarvis_Android_Roadmap.md`)_

---

## Status Snapshot

- [x] Phase 0.1: Legacy SPA route work is ready—Vite base `/` plus `BuildSpa` hook exist and old `wwwroot/app` assets were removed (placeholder `.gitkeep` holds the root while release builds copy `dist`).
- [x] Phase 0.2: `AutomationDbContext` owns the schema, migrations point to `InitialCanonicalSchema`, and there is no manual DDL in `Program.cs`.
- [x] Phase 0.3: Desktop and Web dev proxy both target `http://localhost:5000` and `.NET 9` where applicable. _(Android/Mobile targeting deferred; see Android roadmap.)_
- [x] Phase 0.4: CI produces a single `focusdeck-server-with-spa` artifact that stitches WebApp output and server builds into one deployable.
- [x] Phase 1: Foundations are ready—multi-tenant plumbing is wired (null tenant default, factory coverage, stubbed tenant membership for auth tests) so focus can shift to tenant-aware APIs/UI and the `/` SPA launch on Linux.
- [x] Phase 3.1: Jarvis "The Architect" (Proactive Automation Generation) - Retrieval Layer & Proposal Engine complete.
- [x] Phase 2.2/3.5: Automation Execution Engine (Local Runtime) - In Progress.

## Verifications

- [x] `npm run build` (WebApp) succeeds with the new QR/canvas types; Vite reports a large chunk warning but finishes.
- [x] `dotnet build FocusDeck.sln -c Release` (passes with the known warnings in LectureIntegrationTests, RemoteControlIntegrationTests, and AssetIntegrationTests).

## Execution order — Server/Web → Windows (Android in separate roadmap)

- **Step 1: Harden the Linux web server + Web UI.** Finish all Phase 0 + Phase 1 server and WebApp tasks first (schema ownership in EF, `/` SPA routing, multi‑tenant plumbing, `/v1/tenants/*` APIs, Jarvis APIs/UI, Linux URL cleanup, CI artifact) so the ASP.NET Core app and web client are rock‑solid and deployable on your Linux host.  
- **Step 2: Bring the Windows desktop client up to parity.** Once the backend is stable, wire WPF onboarding/PAKE/tenant context in `OnboardingWindow`, `KeyProvisioningService`, and the shell UX against the already‑solid server, keeping changes client‑side only.  
- **Android/Mobile:** Defer to `docs/FocusDeck_Jarvis_Android_Roadmap.md` and only start once the server + Windows client are in production and stable.

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

**Files**

- `src/FocusDeck.Desktop/App.xaml.cs`
- `src/FocusDeck.WebApp/vite.config.ts`

### 0.4 CI builds a single deployable

**Why:** Artifacts must include server and SPA.

- [ ] GitHub Actions: `actions/setup-node` + `npm ci` + `npm run build` (WebApp)
- [ ] Run `dotnet build -c Release` (server `BuildSpa` hook)
- [ ] Assert `src/FocusDeck.Server/wwwroot/index.html` exists (CI check)

**Files**

- `.github/workflows/build-server.yml`
- `src/FocusDeck.Server/FocusDeck.Server.csproj`

### Phase 0 platform breakdown (Server → Windows)

**Server (Linux / ASP.NET Core)**

- Clean up the legacy UI and SPA hosting by updating `src/FocusDeck.Server/Program.cs` and `src/FocusDeck.Server/wwwroot/**` so `/` serves the Vite-built SPA and `/v1/*`/SignalR/health endpoints stay API-only.  
- Move schema ownership into EF by ensuring `AutomationDbContext` and migrations under `src/FocusDeck.Persistence/**` fully describe the DB (no manual SQL in `Program.cs`).  
- Make CI build the full server + SPA artifact by wiring the `BuildSpa` target and GitHub Actions workflow so the published `/wwwroot` contains the compiled web assets.

**Windows Desktop (WPF client)**

- Point the desktop app at the canonical dev API URL by updating `src/FocusDeck.Desktop/App.xaml.cs` to use `http://localhost:5000`, matching the server and WebApp dev proxy.  
- No major desktop‑only features land in Phase 0; the key work is verifying the app can still boot and talk to the unified server after ports and schema are stabilized.

> Android/Mobile (MAUI client) setup for Phase 0 (ports, .NET 9, base URLs) is tracked separately in `docs/FocusDeck_Jarvis_Android_Roadmap.md`.

---

## Phase 1 — SaaS Foundation + Auth UI + URL Fixes (Sprint 3–4)

**Goal:** Multi-tenancy; PAKE login/registration/pairing UIs on Web/Desktop; SPA serves at `/` in Linux; Cloud/Tunnel ingress stable. _(Android/Mobile auth is deferred to the Android roadmap.)_

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

### 1.2 Authentication UI (sequenced: Web → Windows)

_Sequence: build and validate PAKE + tenant flows on Web first, then port those flows to the Windows desktop client. Android/Mobile follows later in the Android roadmap, reusing the same endpoints and contracts._

**Web (first)**

- [x] `/login`, `/register`, `/pair` (PAKE start/finish; store tokens; `ProtectedRoute`; see `AuthPakeController`, `src/FocusDeck.WebApp/src/lib/pake.ts`, `KeyProvisioningService`)
- [x] Login system validated end-to-end (fresh DB register + login + tenant claim confirmed)
- [x] Files:
  - `src/FocusDeck.WebApp/src/pages/LoginPage.tsx`
  - `src/FocusDeck.WebApp/src/pages/ProvisioningPage.tsx`
  - `src/FocusDeck.WebApp/src/pages/PairingPage.tsx`
  - `src/FocusDeck.WebApp/src/lib/pake.ts`
  - `src/FocusDeck.WebApp/src/hooks/useCurrentTenant.ts`
  - [x] Surface tenant context in the web shell via `/v1/tenants/current` so users can see the active workspace and jump to the Tenants page.
  - [x] Allow switching tenants directly from the Tenants page (`/v1/tenants/{id}/switch`) to refresh tokens and tenant context.

**Windows Desktop (second)**

- [x] Desktop: `OnboardingWindow` → `KeyProvisioningService` (PAKE flows + tenant refresh wired to `/v1/auth/pake`)
- [x] Desktop: `KeyProvisioningService` now exposes tenant context (`CurrentTenantDto`) and raises updates so the shell can show the current workspace after login.
- [x] Files:
  - `src/FocusDeck.Desktop/Views/OnboardingWindow.xaml(.cs)`
  - `src/FocusDeck.Desktop/Services/Auth/KeyProvisioningService.cs`

### 1.3 Linux web server URL cleanup (what you asked)

- [x] Remove old UI from `wwwroot/app/**` on server
- [x] Nginx/Cloudflare (if used) route `/` → Kestrel `:5000` (no subpath) (see `docs/CLOUDFLARE_DEPLOYMENT.md`)
- [x] Verify deep-links: `/notes`, `/lectures/123` load on refresh (`src/FocusDeck.Server/Program.cs` history fallback)
- [x] Fix SPA base to `/` (no `/app`), avoid `/app/app/...` paths (`src/FocusDeck.WebApp/vite.config.ts`)

**Files**

- `src/FocusDeck.Server/wwwroot/**`
- `src/FocusDeck.WebApp/vite.config.ts`
- Infra (nginx/cloudflared) config in your Linux host

### Phase 1 platform breakdown (Server → Windows)

**Server (backend + Linux web host)**

- Implement full multi‑tenant support on the backend by updating entities under `src/FocusDeck.Domain/Entities/*`, `AutomationDbContext`, and `TokenService` so every request carries an `app_tenant_id` claim and all `IMustHaveTenant` data is automatically stamped with `TenantId`.  
- Add tenant‑aware APIs like `/v1/tenants/current` and `/v1/tenants/{id}/switch` so all clients can discover and change the active workspace without custom hacks.  
- Fix Linux URL behavior by cleaning up `src/FocusDeck.Server/wwwroot/**`, setting the Vite base to `/` in `src/FocusDeck.WebApp/vite.config.ts`, and confirming the history fallback in `Program.cs` makes `/notes`, `/lectures/{id}`, etc. refresh correctly behind Nginx/Cloudflare.

**Windows Desktop (WPF client)**

- Wire the onboarding flow in `src/FocusDeck.Desktop/Views/OnboardingWindow.xaml(.cs)` and `Services/Auth/KeyProvisioningService.cs` to call the PAKE endpoints (`/v1/auth/pake`) and store the returned tenant‑scoped tokens.  
- Surface the current tenant (name/slug) and auth status in the shell window so users can see which workspace they are in after login.  
- Ensure desktop UX basics are in place (auth wizard, main shell nav, connection/JWT/tenant status bar) so Jarvis and multi‑tenant features make sense to users on Windows.

> Android/Mobile (MAUI client) platform work for Phase 1 is tracked in `docs/FocusDeck_Jarvis_Android_Roadmap.md`.

### 1.4 UX pass (first cut)

- [x] Desktop: onboarding flow (auth), main shell nav, status bar for connection/JWT/tenant (`ShellWindow`, `OnboardingWindow`)
- [x] Web: clean top-nav, Notes/Lectures/Courses list pages wired; empty states (`AppLayout`, page components)

### Phase 1 execution focus

Use this mini-plan to steer Sprint 3–4 work now that Phase 0 plumbing is stable.

1. **Multi-tenant infrastructure**  
   - Add `TenantId` to every `IMustHaveTenant` entity and persist it on `SaveChangesAsync` so writes are annotated automatically (`AutomationDbContext` + `domain entities`).  
   - Inject `app_tenant_id` into JWTs while also reading it in the global query filter so every API call scopes data (`TokenService`, `AutomationDbContext`).  
   - Build tenant bootstrapping stories in `TenantMembershipService` so onboarding/login tracks a `Tenant`/`UserTenant` pair for every user or device.

2. **Authentication surfaces**  
   - Wire `/login`, `/register`, `/pair` in the WebApp with PAKE flows and a `ProtectedRoute` wrapper; share the `pake.ts` logic so the Windows client (and later Android) can reuse it.  
   - Update Desktop `OnboardingWindow` + `KeyProvisioningService` to hit the new endpoints and store tenant-scoped tokens.  
   - Surface tenant information (name, slug) in the UI status bars/sidebars once authentication succeeds.

3. **Linux SPA deployment readiness**  
   - Confirm Linux/Nginx routes `/` → Kestrel `:5000`, remove `/app` assets, and verify deep-link refreshes using the existing history fallback.  
   - Document the Linux host configuration (Nginx, Cloudflare rules, ports) in an infra note so the team can reproduce the `/` behavior.

---

## Phase 1.5 — Jarvis Contextual Learning Loop (Sprint 4.5)

**Goal:** Elevate Jarvis from scripted executor to adaptive assistant that learns from context while preserving privacy and respecting device resources.

### 0. Privacy & User Controls (Pre-flight)

- [x] Build `PrivacyService` with anonymization tiers (`Low | Medium | High`) and ensure every capture hook consults `PrivacyService.IsEnabled(contextType)`.
- [ ] Implement consent dashboard (Web + Desktop) to toggle capture types (e.g., `ActiveWindowTitle`, `TypingVelocity`, `MouseEntropy`, `PhysicalLocation`) and provide live preview, delete, export, and disable controls.
- [x] Gate all snapshot and feedback pipelines behind verified consent; no contextual data leaves a device until privacy checks pass.

### 1. Context Snapshot Infrastructure

> **Implementation Guide:** See [`docs/CONTEXT_SNAPSHOT_PIPELINE.md`](docs/CONTEXT_SNAPSHOT_PIPELINE.md) for the full design. The following steps will complete the skeleton system:

- [ ] **Implement the `EfContextSnapshotRepository`:**
    - Wire up the `AddAsync`, `GetByIdAsync`, and `GetLatestForUserAsync` methods to use the `AutomationDbContext`.
    - Add a `DbSet<ContextSnapshot>` to the `AutomationDbContext`.
    - Create a new EF Core migration to add the `ContextSnapshots` table.
- [ ] **Implement the `ContextSnapshotService`:**
    - Inject all `IContextSnapshotSource` implementations into the service.
    - Implement the `CaptureNowAsync` method to:
        - Call all sources to get context slices.
        - Merge the slices in order of priority.
        - Save the final snapshot to the repository.
        - Enqueue a background job for vectorization.
- [ ] **Implement the `ContextController`:**
    - Inject the `IContextSnapshotService` into the controller.
    - Wire up the controller actions to call the service.
- [ ] **Implement the Snapshot Sources:**
    - Replace the fake data in the snapshot sources with real data from the corresponding APIs (Google Calendar, Canvas, Spotify, etc.).
- [ ] **Connect to the System:**
    - Register the new services and repositories in the dependency injection container in `src/FocusDeck.Server/Startup.cs`.
    - Add the new `DbContext` changes to the `AutomationDbContext`.

### 2. On-Device Feature Engineering & Enhanced Depth

- [ ] Extend clients and bridges (Desktop system tray monitor, VS Code extension, Browser Bridge) to enrich snapshots with behavioral metrics (`TypingVelocity`, `MouseEntropy`, `ContextSwitchCount`) and environment signals (`DevicePosture`, `AudioContext`, `PhysicalLocation`).
- [ ] Implement local aggregation so raw keystroke/mouse data stays on device; send `FeatureSummary` payloads with optional `ApplicationStateDetails` JSON blob.
- [ ] Ensure capture SDK respects device power profiles—offer adaptive sampling rates to minimize CPU/battery on laptops while scaling up on high-performance desktops.

### 3. Real-Time Vectorization & Storage (skeleton complete)

> **Implementation Guide:** See [`docs/Vectorization-Implementation-Notes.md`](docs/Vectorization-Implementation-Notes.md) for details on setting up the vector DB and completing the job logic.

- [x] Queue `VectorizeSnapshotJob` whenever a snapshot is persisted (and on feedback updates) to compute embeddings (`all-MiniLM-L6-v2` or ML.NET equivalent) and update indexes within seconds. _(Skeleton implemented)_
- [ ] Deploy pgvector (preferred) or Qdrant alongside PostgreSQL and create `ContextVectors` tables for behavioral, temporal, and project embeddings. _(DB work remaining)_
- [ ] Track queue throughput/lag metrics and schedule cleanup jobs for expired snapshots.

### 4. Suggestion APIs & Explainability (skeleton complete)

> **Implementation Guide:** See [`docs/SuggestionAPI-Implementation-Notes.md`](docs/SuggestionAPI-Implementation-Notes.md) for details on completing the implementation.

- [x] Implement `/v1/jarvis/suggest` with a rule-based MVP, then upgrade to vector-driven retrieval via `VectorSearchService`. _(Skeleton implemented)_
- [ ] Integrate MCP Gateway tool (`jarvis.analyze_context`) to allow LLM reasoning over layered context.
- [x] Return `{ action, parameters, confidence, evidence[] }` payloads and surface "Why?" UI that fetches referenced snapshot summaries. _(Skeleton implemented)_

### 5. Feedback & Reinforcement Loop (skeleton complete)

> **Implementation Guide:** See [`docs/FeedbackLoop-Implementation-Notes.md`](docs/FeedbackLoop-Implementation-Notes.md) for details on completing the implementation.

- [x] Add `/v1/jarvis/feedback { snapshotId, reward }` API and `Jarvis.FeedbackSignals` storage for explicit (👍/👎) and implicit (completion rate, dwell time) rewards. _(Skeleton implemented)_
- [x] Implement `ImplicitFeedbackMonitor` to infer rewards and trigger snapshot re-vectorization with decayed weighting (recent signals ×10). _(Skeleton implemented)_
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

### 2.2 Native Automation Engine (YAML + Local Execution)
**Goal:** A Home Assistant-style automation engine where behaviors are defined in YAML and executed locally. This maximizes token efficiency (AI writes the YAML once; engine runs it forever) and privacy.

- [ ] **Automation Core:**
    - `AutomationEngine`: The background service that listens for triggers and executes actions.
    - `YamlLoader`: Parses YAML definitions into executable `Automation` objects.
    - `TriggerSystem`: Event bus for `TimeTrigger`, `StateChangeTrigger` (e.g., "Focus Mode ON"), `EventTrigger` (e.g., "Calendar Event Started").
    - `ActionSystem`: Registry of executable actions (e.g., `Obsidian.AppendNote`, `Spotify.Play`, `Windows.OpenApp`).
- [ ] **Integration Registry:**
    - System for users to add/configure integrations (Google, Spotify, Obsidian, etc.) which expose Triggers and Actions.
- [ ] **Web UI (Automation Center):**
    - Dashboard showing active automations.
    - **YAML Editor:** Monaco-based editor for raw YAML editing.
    - **Visual Builder:** Form-based UI for editing triggers/actions (generates YAML).
    - **Run History:** Logs of when automations ran and their output.

**Files**
- `src/FocusDeck.Server/Services/Automation/**`
- `src/FocusDeck.Domain/Entities/Automation.cs`
- `src/FocusDeck.WebApp/src/pages/Automations/**`

### 2.3 Core Integrations (Email, Code, Files)
**Goal:** Give the Automation Engine access to external tools via "Integrations".

- **Email (Gmail / Outlook)** — Triggers: `OnEmailReceived`; Actions: `CreateTask`.
- **GitHub / GitLab** — Triggers: `OnPrAssigned`; Actions: `OpenBrowser`.
- **Google Drive / OneDrive** — Actions: `SaveFile`, `ListFiles`.

---

## Phase 3 — Jarvis "The Architect" (Sprint 6–7)

**Goal:** Jarvis acts as the *creator* of automations, not just the runner. It analyzes context and writes YAML automations for the engine to execute.

### 3.1 Jarvis Modes
- **Manual Mode:** User writes YAML manually. Jarvis is passive.
- **Review Mode (Default):** Jarvis suggests automations (YAML); User must approve/edit them before they become active.
- **Auto Mode:** Jarvis creates and enables automations automatically based on confidence thresholds.

### 3.2 The "Architect" Loop
- [ ] **Context Review Job:** Scheduled job (user-defined interval) where Jarvis analyzes `ContextSnapshots` and `ActivitySignals`.
- [ ] **Automation Generator:** LLM prompt pipeline that outputs valid YAML automations based on the user's habits (e.g., "I see you always open VS Code and Spotify at 9 AM; here is an automation to do that").
- [ ] **Suggestion UI:** Interface for the user to review, diff, and accept Jarvis-generated automations.

### 3.3 Jarvis API
- `POST /v1/jarvis/architect/analyze` -> Triggers an ad-hoc review.
- `POST /v1/jarvis/architect/generate` -> Generates YAML for a specific intent.

### Phase 3.5: Foundation - Hybrid Academic Writing Engine (Weeks 13-16)

**Goal:** Build the "Body" for Jarvis to inhabit. Create a professional-grade writing environment that supports complex academic work natively (Zero-Token) so Jarvis has a structured target for generation later.

#### 3.5.1 Dual-Mode Editor Architecture
- [ ] **Architecture Fork:** Split `Note` entity into `QuickNote` (Markdown) and `AcademicPaper` (Structured Document).
- [ ] **Paper Mode UI:** Implement WPF `FlowDocument` viewer for true pagination, headers, and footers (Google Docs style).
- [ ] **Smart Mode Switcher:** Toggle context: "Speed Mode" (Notes) vs. "Format Mode" (Paper).

#### 3.5.2 Native Citation Engine (Deterministic/No-LLM)
- [ ] **Structured Source Database:** Create `AcademicSource` entity to store metadata (Author, Year, Publisher) separate from text.
- [ ] **The "Cite-o-matic" Logic:** C# service to generate APA/MLA/Chicago strings deterministically (No AI tokens used).
- [ ] **"Find & Cite" Tool:** Paste a quote $\to$ Engine finds text position $\to$ Injects citation ID $\to$ Auto-updates Bibliography footer.
- [ ] **Hot-Swap Styles:** Change paper from APA to MLA instantly by re-rendering the footer (not rewriting text).

**Deliverables:**
- `CitationEngine` (Core logic for formatting)
- `PaperEditorControl` (WPF RichText implementation)
- `SourceManagerDialog` (UI for adding books/links)

**Tests:**
- [ ] Bibliography generates correctly for 50+ sources in < 100ms.
- [ ] Switching Citation Style updates all footnotes without breaking text.

------

## Phase 4 — Auto-Tag Notes to Class (GCal) + Calendar Planner (Sprint 8)

**Goal:** When you start writing/recording, the note auto-attaches to the current class from Google Calendar.

### Activity Signals – Burnout & context telemetry

- Ingest UI/agent telemetry via `POST /v1/activity/signals` (v1 API, `[Authorize]`).
  Each payload carries `SignalType`, `SignalValue`, `SourceApp`, optional `MetadataJson`, and `CapturedAtUtc`.
- Persists the new `ActivitySignal` entity (`Id`, `TenantId`, `UserId`, `SignalType`, `SignalValue`, `SourceApp`, `MetadataJson`, `CapturedAtUtc`), indexed by `TenantId` + `CapturedAtUtc` so we can efficiently trend per-tenant/time window.
- `/jarvis` exposes an “Emit sample activity signals” button (guarded by `Features:Jarvis`) that posts fake `TypingBurst`/`ActiveWindow` signals to the ingestion API, helping QA verify the pipeline without production sensors.
- This telemetry stream seeds the upcoming burnout/autotagging work so we can detect typing bursts, active windows, and other context signals before hooking real clients.
- [x] Burnout analysis now persists `StudentWellnessMetrics` (hours_worked, break_frequency, quality_score, sleep_hours, `IsUnsustainable`) and `BurnoutCheckJob` runs every 2 hours via Hangfire to flag 3+ consecutive 12-hour days or >50% break-frequency drops.

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

#### 4.3 The Lecture Scribe (Zero-Input Audio Pipeline)
**Goal:** Never miss a detail in class, even if you forget to open the app.
- [ ] **Geofence Triggers:** Service detects "On Campus" location + "Class Time" (Calendar).
- [ ] **Passive Audio Sentinel:** Low-power listening for dominant voice frequencies (Professor speaking) when in class context.
- [ ] **Auto-Record & Transcribe:** Automatically starts local audio recording and processes via local Whisper model (no cloud costs).
- [ ] **Smart Synthesis:** Converts transcript into a structured `Note` with bullet points, "Key Terms," and "Homework Mentions."

**Deliverables:**
- `IGeoLocationService` (Platform-specific geofencing)
- `IAudioSentinelService` (Privacy-focused voice activity detection)
- `LocalWhisperIntegration` (C# bindings for `whisper.cpp` to run on GPU)

## Phase 5 — Device Agent + Job Bundles (Sprint 9–10)

**Goal:** Server plans; device acts. Prepare class layouts/jobs on server; execute instantly when laptop/phone connects.

### 5.1 Job schema & agent skeleton (Windows)

- [ ] JSON job bundle (idempotent steps, `expires_at`, safety preview)
- [ ] Windows agent: service + WebSocket client; capabilities advertised to server
- [ ] Skills: `arrange_layout`, `open_url`, `open_notes_page`, `arm_audio_recording`, `show_pill`
- [ ] Server endpoint: queue jobs to device; status graph (queued → dispatched → done/failed)

**Files**

- `agents/windows/**`
- `src/FocusDeck.Server/Controllers/v1/AgentController.cs`
- `src/FocusDeck.Server/Services/Jobs/**`

#### 5.3 The Tab Shepherd (Browser Clutter Killer)
**Goal:** Eliminate "Tab Hoarding" anxiety by treating browser tabs as transient context, not permanent storage.
- [ ] **Browser Extension:** Build Chrome/Edge extension to communicate active tabs to FocusDeck Desktop via SignalR.
- [ ] **Context Binding:** "Bind" a set of tabs to a specific Project or FocusDeck Task.
- [ ] **Auto-Fold:** If a tab group hasn't been touched in 2 hours, auto-close it and save it as a "Session Bundle" in the project history.
- [ ] **Instant Restore:** When opening the project again, one click restores the exact browser state (scroll position + open tabs).

**Deliverables:**
- `FocusDeck.BrowserExt` (Manifest V3 extension)
- `IBrowserContextService` (Manage tab groups/persistence)

#### 5.4 The Ambient Horizon (Subconscious Deadline Awareness)
**Goal:** Replace stressful notification spam with subtle, peripheral awareness of time.
- [ ] **Visual Urgency Engine:** Tints UI accents (Dock border, wallpaper glow) based on `TimeRemaining / WorkRemaining`.
    - *Cool Blue:* Safe (> 3 days).
    - *Warm Orange:* Warning (< 24 hours).
    - *Critical Red:* Immediate (< 4 hours).
- [ ] **The "Morning Rundown" Protocol:**
    - Optional "Morning Meeting" mode where Jarvis greets you with a visual summary of the day's "Horizon" colors.
    - User choice: "Show me the raw list" vs. "Just give me the vibe (colors)."
- [ ] **Granular Customization:**
    - **Toggle:** Master On/Off switch for ambient effects.
    - **UI Mode:** Choose between "Subtle Glow," "Text Ticker," or "Standard Notifications."
    - **Palette Editor:** User defines what "Urgent" looks like (e.g., maybe Purple instead of Red).

**Deliverables:**
- `IAmbientDisplayService` (Controls UI theming/overlay)
- `MorningBriefingView` (The "Morning Meeting" UI)

## Phase 6 — Browser Bridge + Memory Vault + Project Memory (Sprint 11–12) custom browswer exstentions for zen browswer/firefox

**Goal:** Capture dev research, AI chats, and tab context into a personal knowledge notebook and recoverable states.

### 6.1 Browser Bridge (extension + bridge API)

- [ ] Capture open tabs, detect AI chat pages, long code blocks
- [ ] Commands: open tabs set, close aged, screenshot page, send content to FocusDeck
- [ ] Link captured items to active project/code path

### 6.2 AI Memory Vault + Project Timeline

- [ ] Store meaningful AI chats (title, tags, gist, links)
- [ ] Auto-summarize research queries into “Developer Knowledge Notebook”
- [ ] Show “last next step” reminder when you revisit code area
Browser extension (Firefox/Zen)

Use this for:

Capture:

List open tabs and their URLs/titles

Content script to scrape:

AI chats (ChatGPT, Claude, Gemini, etc.)

Large <pre><code> blocks

Article body text for research

Take screenshots of current page if needed

Commands (keyboard or button):

“Send all AI chats on this page to FocusDeck”

“Send this tab/page to FocusDeck → active project”

“Close old tabs from this domain/workspace”

“Save this as ‘research session’ for project X”

Bridge:

POST to https://your-focusdeck-server/v1/browser/events

Includes:

userId

projectId or repoSlug

tab URL, title

scraped content

“kind” (ai_chat, code_snippet, research_article, etc.)

FocusDeck.Server (Browser Bridge + Memory Vault)

Server does the thinking and storage:

Browser Bridge API

/v1/browser/tabs/snapshot

/v1/browser/page/capture

/v1/browser/ai-chat/save

All tied to an authenticated user + project

AI Memory Vault

Store:

page content

minimal AI chat logs (title, gist, tags, links)

Run jobs to:

Summarize into “Developer Knowledge Notebook”

Extract TODOs / “next step” per project

Project Timeline

Timeline entries like:

“You read 4 pages about X at 3:20 PM”

“You asked AI about refactoring Y”

When you revisit a repo/branch/task:

“Last next step here was: refactor FooService into modules A/B.”

Windows app / Web UI

These are just views and controls over what the extension + server are doing:

Show:

Knowledge notebook per project

AI conversations attached to tasks/repos

Tab snapshots from previous sessions (“restore this work session”)

Let the user:

Search their research

Jump from a notebook entry → open repo / note / code area

Configure rules (“auto-save all ChatGPT convos tagged ‘FocusDeck’”)


---

## Phase 7 — “Mental Save State” + Adaptive Layout Intelligence (Sprint 13–14) windows app 

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
- **P1 Gate:** Multi-tenant reads/writes; Web/Desktop can login/register/pair; Linux deploy serves SPA at `/`.
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
- [ ] `[OBS] OTLP+Prom+Serilog production pipeline`
- [ ] `[MCP] FocusDeck MCP server + gateway`
- [ ] `[JARVIS] Contextual learning loop (privacy, snapshots, suggest API)`
- [ ] `[JARVIS] API + Hangfire runner + SignalR dispatch`
- [ ] `[JARVIS] Smart Start Note workflow (current class)`
- [ ] `[JARVIS] Summarize + quiz note workflow`
- [ ] `[JARVIS] Extract tasks/deadlines from note`
- [ ] `[JARVIS] Next Session Prep (dashboard suggestions)`
- [ ] `[JARVIS] Automation Center UI (manage & create automations)`
- [ ] `[CAL] Google Calendar warm sync + resolver`
- [ ] `[AGENT] Windows agent skeleton + skills`
- [ ] `[BRIDGE] Browser extension + memory vault`
- [ ] `[WORKSPACE] Mental save state + adaptive layouts`

---

## Final UI/UX Note (what to design at the end of P1)

- **Desktop:** Onboarding/login, left nav (Notes/Lectures/Courses), status bar, “Start Note” primary CTA.
- **Web (Linux server):** Clean top bar, Notes list with course chips, `/jarvis` gated page, deep-link routing tested at root.

---

With this roadmap committed, the next action item is to begin **Phase 1** execution once Phase 0 stabilization tasks are delivered and validated.
