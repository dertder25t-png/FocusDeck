# ROADMAP.md

## FocusDeck Feature Roadmap

This document outlines the feature development roadmap for FocusDeck, organized into phases with clear deliverables and current implementation status.

---

## Implementation Status Legend

- ✅ **Fully Implemented** - Feature is complete and tested
- 🚧 **In Progress** - Feature is partially implemented
- 📋 **Planned** - Feature is designed but not yet implemented
- 🔄 **Needs Update** - Feature exists but needs refactoring/updates

---

## Shared Production Foundations

### Server Infrastructure ✅ **COMPLETE**

**Status:** Fully implemented and production-ready

- ✅ **Serilog Logging**
  - Structured logging with correlation IDs
  - Request/response logging
  - Console and file outputs
  - Machine name and thread ID enrichment

- ✅ **Hangfire Background Jobs**
  - PostgreSQL storage backend
  - Job dashboard at `/hangfire`
  - 5 concurrent workers
  - Automatic retry policies

- ✅ **SignalR Real-Time Communication**
  - NotificationsHub at `/hubs/notifications`
  - WebSocket support
  - Event broadcasting for:
    - Lecture processing updates
    - Focus session changes
    - Remote control actions

- ✅ **JWT Authentication**
  - Access tokens (60 min expiration)
  - Refresh tokens (7 day expiration)
  - Token rotation with replay attack detection
  - Client fingerprinting

- ✅ **Google OAuth Support** 🆕
  - `POST /v1/auth/google` endpoint
  - ID token verification
  - Automatic user provisioning
  - Profile data integration

- ✅ **API Versioning**
  - All endpoints under `/v1/*`
  - Swagger documentation groups
  - Version negotiation support

- ✅ **Health Checks**
  - `GET /v1/system/health` endpoint
  - Database connectivity check
  - Filesystem write check
  - Detailed metrics (duration, status)

- ✅ **OpenTelemetry Tracing**
  - HTTP request instrumentation
  - Database query instrumentation
  - SignalR connection tracing

### Desktop (WPF) Foundations 📋 **PLANNED**

**Status:** Not implemented (Windows-only, requires Visual Studio)

**Planned Features:**
- 📋 **Design Tokens**
  - `Colors.xaml` - Color palette
  - `Typography.xaml` - Font system
  - `Spacing.xaml` - Layout grid

- 📋 **Theme System**
  - Light theme
  - Dark theme
  - System theme detection

- 📋 **Snackbar Service**
  - Toast notifications
  - Action buttons
  - Auto-dismiss

- 📋 **Command Palette (Ctrl+K)**
  - Quick actions
  - Search commands
  - Keyboard shortcuts

- 📋 **3-Pane Shell**
  - Navigation sidebar
  - Main content area
  - Info/details panel

### Mobile (MAUI) Foundations 🚧 **IN PROGRESS**

**Status:** Core services implemented, UI components planned

- ✅ **Device Pairing Service** 🆕
  - Pairing code generation
  - Desktop-mobile linking
  - Pairing verification
  - Unpair functionality

- ✅ **WebSocket Client Service**
  - SignalR connection management
  - Automatic reconnection
  - Message send/receive
  - State change events

- ✅ **Heartbeat Service**
  - Configurable interval (default 30s)
  - Background timer
  - Start/stop controls
  - Event-based notifications

- 📋 **Heartbeat Payload Integration**
  - Device activity tracking
  - Screen state reporting
  - Focus session sync

---

## Day 1: Asset Pipeline ✅ **COMPLETE**

**Status:** Server-side fully implemented, desktop client planned

### Server Components ✅

- ✅ **Asset Entity**
  - ID, filename, content type
  - Size, storage path
  - Upload timestamp and user
  - Metadata dictionary

- ✅ **Upload Endpoint**
  - `POST /v1/uploads/asset`
  - Multipart form data
  - 5MB size limit enforcement
  - Content type validation
  - Streaming upload

- ✅ **Download Endpoint**
  - `GET /v1/assets/{id}`
  - Authorized access only
  - Range request support
  - Streaming delivery

- ✅ **Delete Endpoint**
  - `DELETE /v1/assets/{id}`
  - Cascading deletion
  - Storage cleanup

- ✅ **File Storage**
  - Organized by date: `/data/assets/{yyyy}/{MM}/{id.ext}`
  - Automatic directory creation
  - Local filesystem storage
  - Extensible storage interface

- ✅ **Integration Tests**
  - Upload/download round-trip
  - Size limit enforcement
  - Content type verification
  - Error handling

### Desktop Client 📋

- 📋 **IAssetClient Interface**
  - UploadAsync with progress
  - DownloadAsync with streaming
  - DeleteAsync

- 📋 **Progress UI**
  - Upload progress bar
  - Cancel operation
  - Error handling

---

## Day 2: Lecture Entities & Recording ✅ **COMPLETE (Server)**

**Status:** Server-side fully implemented, desktop UI planned

### Server Components ✅

- ✅ **Course Entity**
  - Name, code, instructor
  - Description
  - Lecture collection

- ✅ **Lecture Entity**
  - Title, description
  - Recorded date
  - Audio asset reference
  - Processing status enum
  - Transcription text
  - Summary text
  - Generated note reference
  - Duration

- ✅ **Endpoints**
  - `POST /v1/lectures` - Create lecture
  - `POST /v1/lectures/{id}/audio` - Upload audio (50MB limit)
  - `GET /v1/lectures/{id}` - Get lecture details
  - `GET /v1/lectures/course/{courseId}` - List course lectures
  - `POST /v1/lectures/{id}/process` - Start transcription/summarization

- ✅ **TranscribeLectureJob Stub**
  - Job interface defined
  - Whisper adapter interface
  - Audio file validation
  - Status updates

- ✅ **SignalR Events Reserved**
  - `Lecture:Transcribed`
  - `Lecture:Summarized`
  - `Lecture:NoteReady`

### Desktop Client 📋

- 📋 **WASAPI Audio Recorder**
  - Start/Pause/Stop controls
  - Level meter visualization
  - Elapsed time display
  - WAV file output

- 📋 **Recording Workflow**
  - On stop → Create lecture
  - Upload WAV file
  - Trigger processing
  - Show pending state card

---

## Day 3: AI Transcription & Summarization 🚧 **IN PROGRESS**

**Status:** Job infrastructure exists, AI integration pending

### Server Components 🚧

- ✅ **TranscribeLectureJob**
  - Job defined with interface
  - Hangfire integration
  - 📋 Whisper.cpp adapter (stub)
  - 📋 Audio preprocessing
  - 📋 Save transcription text

- ✅ **SummarizeLectureJob**
  - Job defined with interface
  - LLM provider interface
  - 📋 Generate summary (≤250 words)
  - 📋 Extract 5 key bullet points
  - 📋 Save summary

- ✅ **Job Chaining**
  - Transcribe → Summarize continuation
  - `POST /v1/lectures/{id}/process` endpoint
  - Automatic job enqueueing

- ✅ **SignalR Notifications**
  - Progress updates
  - Completion events
  - Error notifications

### Desktop Client 📋

- 📋 **Progress Chips**
  - Real-time status updates
  - Visual indicators
  - Click to view details

- 📋 **Lecture Details Pane**
  - Auto-open when complete
  - Show transcription
  - Show summary

### Integration Requirements 📋

- 📋 **Whisper Integration**
  - Install whisper.cpp locally
  - Configure model path
  - Handle audio formats

- 📋 **LLM Integration**
  - OpenAI API
  - Anthropic Claude API
  - Local LLM option (Ollama)

---

## Day 4: Note Generation & Review Plans 🚧 **IN PROGRESS**

**Status:** Entities exist, job logic planned

### Server Components 🚧

- ✅ **ReviewPlan Entity**
  - ID, lecture reference
  - Start date, test date
  - Review sessions (jsonb)
  - Status tracking

- ✅ **ReviewSession Entity**
  - Scheduled date
  - Status (pending/completed)
  - Completion date
  - Performance rating

- 🚧 **GenerateLectureNoteJob**
  - Job interface defined
  - 📋 Generate note sections:
    - Key Points
    - Definitions
    - Likely Test Questions
    - References
  - 📋 Link to lecture
  - 📋 Emit `Lecture:NoteReady` event

- ✅ **Review Plan Endpoint**
  - `POST /v1/review-plans`
  - Input: lectureId, lastTestDate, nextTestDate
  - Spaced repetition algorithm (D0, D+2, D+7, D+14)
  - Generate review sessions

### Desktop Client 📋

- 📋 **Calendar Picker**
  - Side-rail component
  - Select test dates
  - Visual indicators

- 📋 **Create Review Plan Button**
  - One-click workflow
  - Default spacing
  - Manual override

- 📋 **Sessions List View**
  - Upcoming sessions
  - Completed sessions
  - Performance tracking

---

## Day 5: Assessment Resolver 📋 **PLANNED**

**Status:** Architecture designed, implementation pending

### Server Components 📋

- 📋 **UpcomingAssessmentResolver Service**
  - Google Calendar integration
  - Canvas LMS integration
  - Keyword detection (Exam, Test, Quiz, Midterm, Final)
  - Nearest event selection per course

- 📋 **External Integrations**
  - Google Calendar API client
  - Canvas API client
  - Token management

- 📋 **Endpoint**
  - `GET /v1/courses/{id}/assessments/next`
  - Response: date, title, type, source

### Desktop Client 📋

- 📋 **Lecture Side Panel**
  - Show inferred next test date
  - Edit/override button
  - Auto-populate from resolver

### Tests 📋

- 📋 Keyword detection accuracy
- 📋 Earliest selection logic
- 📋 Override behavior
- 📋 Multiple source handling

---

## Day 6: Focus Session Model 🚧 **IN PROGRESS**

**Status:** Entities exist, policies need implementation

### Server Components 🚧

- ✅ **FocusSession Entity**
  - ID, start/end timestamps
  - Mode (strict/soft)
  - Course reference
  - Policy (jsonb)
  - Distraction events (jsonb[])

- ✅ **DeviceLink Entity**
  - ID, device type
  - Last seen timestamp
  - Capabilities (jsonb)

- ✅ **Endpoints**
  - `POST /v1/focus/sessions` - Start session
  - `POST /v1/focus/sessions/{id}/stop` - End session
  - `POST /v1/focus/sessions/{id}/distraction` - Log distraction

- ✅ **SignalR Events**
  - `Focus:Started`
  - `Focus:Ended`
  - `Focus:Distraction`

### Desktop Client 📋

- 📋 **Focus Control Widget**
  - Start/stop button
  - Strict/soft mode selector
  - Timer display
  - Session persistence

- 📋 **Policy Configuration**
  - Block websites
  - Lock desktop
  - Mute notifications
  - Time limits

---

## Day 7: Mobile Focus Integration 📋 **PLANNED**

**Status:** Heartbeat service exists, integration planned

### Mobile Components 📋

- ✅ **HeartbeatService** (Base)
- 📋 **Enhanced Heartbeat Payload**
  - Device ID
  - Screen on/off state
  - User interaction detection
  - Timestamp
  - POST `/v1/devices/heartbeat` every 10s

### Server Components 📋

- 📋 **Device Activity Tracking**
  - "Active in last 15s" flag
  - Interaction timestamps
  - Screen state history

- 📋 **Distraction Detection**
  - Check strict session + phone active
  - Emit `Focus:Distraction` event
  - Log distraction details

### Desktop Client 📋

- 📋 **Lock-In Overlay**
  - Full-screen modal
  - Pause timer
  - Keyboard confirm to resume
  - Distraction reason logging

### Tests 📋

- 📋 Heartbeat throttling
- 📋 Strict vs soft mode logic
- 📋 Overlay activation flow
- 📋 Timer persistence

---

## Day 8: Focus Analytics 📋 **PLANNED**

**Status:** Data collection ready, analytics views planned

### Server Components 📋

- 📋 **Analytics Endpoint**
  - `GET /v1/analytics/focus`
  - Query parameters: startDate, endDate, courseId
  - Response:
    - Total time focused
    - Session count
    - Average duration
    - Distractions per hour
    - Current streak (days)

- 📋 **Data Aggregation**
  - Rolling 28-day window
  - Efficient caching
  - Daily/weekly rollups

### Desktop Client 📋

- 📋 **Charts Component**
  - Time series graph
  - Session duration histogram
  - Distraction breakdown
  - Streak calendar

### Mobile Client 📋

- 📋 **Mini Dashboard**
  - Today's focus time
  - Current streak
  - Quick stats

---

## Day 9: AI-Verified Notes Model 📋 **PLANNED**

**Status:** Architecture designed, implementation pending

### Server Components 📋

- 📋 **NoteSuggestion Entity**
  - ID, note reference
  - Type (MissingPoint/Definition/Reference)
  - Content (markdown)
  - Source (timestamp/section)
  - Confidence score
  - Created/accepted timestamps

- 📋 **Endpoints**
  - `POST /v1/notes/{id}/verify` - Enqueue verification job
  - `GET /v1/notes/{id}/suggestions` - List suggestions
  - `POST /v1/notes/suggestions/{id}/accept` - Accept suggestion

### Desktop Client 📋

- 📋 **Verify Button**
  - Enqueue verification
  - Show progress

- 📋 **Suggestions Panel**
  - Right-rail list
  - Preview card
  - Accept/reject buttons

- 📋 **AI Additions Section**
  - Separate markdown section
  - Never mutates user text
  - Clear attribution

### Guardrails 📋

- 📋 Never modify existing user content
- 📋 Always append to "AI Additions" section
- 📋 Clear source attribution
- 📋 Confidence scoring

---

## Day 10: Note Completeness Job 📋 **PLANNED**

**Status:** Job infrastructure ready, AI logic pending

### Server Components 📋

- 📋 **VerifyNoteCompleteness Job**
  - Compare note vs transcript/summary
  - Semantic similarity analysis
  - Generate atomic suggestions (≤120 words)
  - Source attribution (timestamp/section)
  - Confidence scoring

- 📋 **Coverage Score Endpoint**
  - `GET /v1/notes/{id}/coverage`
  - Response: score (0-100)
  - Breakdown by section

### Desktop Client 📋

- 📋 **Coverage Score Display**
  - Circular progress indicator
  - Color-coded (red/yellow/green)
  - Trend over time

- 📋 **Suggestion List**
  - Ordered by confidence
  - Preview content
  - Quick accept

### Tests 📋

- 📋 Idempotency (no duplicate suggestions)
- 📋 Never deletes existing content
- 📋 Score stability (±5%)
- 📋 Semantic accuracy

---

## Day 11: Design Projects Stub 📋 **PLANNED**

**Status:** Entirely new feature domain

### Server Components 📋

- 📋 **DesignProject Entity**
  - ID, title
  - Goals text
  - Vibes array
  - Requirements text
  - Brand keywords array
  - Assets array

- 📋 **DesignIdea Entity**
  - ID, project reference
  - Type (Thumbnail/Prompt/Moodboard/Reference)
  - Content
  - Asset reference
  - Score
  - Created timestamp

- 📋 **Endpoints**
  - `POST /v1/design/projects` - Create project
  - `GET /v1/design/projects/{id}` - Get project
  - `GET /v1/design/projects/{id}/ideas` - List ideas

- 📋 **Jobs (Stubs)**
  - GenerateThumbnails
  - BrainstormConcepts
  - ReferenceFinder

- 📋 **SignalR Events**
  - `Design:IdeasAdded`

### Desktop Client 📋

- 📋 **Design Tab**
  - Project wizard
  - Input form (goals, vibes, requirements)
  - Generate button

---

## Day 12: Design Ideation Logic 📋 **PLANNED**

### Server Components 📋

- 📋 **GenerateThumbnails Job**
  - 12 ASCII/wireframe layouts
  - Captions (card size, hierarchy)
  - Layout system suggestions

- 📋 **BrainstormConcepts Job**
  - 6-10 design directions
  - Palette ideas (hex codes)
  - Typography families
  - Layout systems
  - Structured JSON output

- 📋 **ReferenceFinder Job**
  - 8-12 reference leads
  - Artists/movements/years
  - Keywords
  - License hints (CC0, public domain)

### Desktop Client 📋

- 📋 **Ideas Card Grid**
  - Visual previews
  - Pin to board
  - Expand details

- 📋 **Board View**
  - Grouped by type
  - Drag and drop
  - Export PNG/PDF

---

## Day 13: Integration Hardening 📋 **PLANNED**

**Status:** Quality and UX improvements

### Tasks 📋

- 📋 **Empty States**
  - No lectures
  - No focus sessions
  - No suggestions
  - No design projects

- 📋 **Error States**
  - API failures
  - Upload errors
  - Job failures
  - Validation errors

- 📋 **Retry/Backoff**
  - Exponential backoff on API calls
  - Automatic job retries (3x)
  - Connection resilience

- 📋 **Optimistic UI**
  - Instant feedback
  - Rollback on error
  - Loading states

- 📋 **Telemetry Events**
  - lecture_created
  - transcribed
  - summarized
  - note_generated
  - focus_started
  - distraction
  - verify_started
  - suggestion_accepted
  - design_ideas_added

### E2E Smoke Tests 📋

- 📋 Record → Transcribe → Note
- 📋 Start focus strict → Phone active → Overlay
- 📋 Verify notes → Accept suggestion
- 📋 Design project → Ideas → Export board

---

## Day 14: UI Polish & Documentation 📋 **PLANNED**

**Status:** Final refinements

### UI Polish 📋

- 📋 **Design System**
  - 8-pt spacing grid
  - 12px border radius
  - Subtle shadows (elevation system)
  - Fluent System Icons
  - 150ms ease-out animations

- 📋 **Command Palette Actions**
  - Start Focus
  - Create Lecture
  - Verify Notes
  - New Design Project

### Documentation 📋

- ✅ **OPERATIONS.md** ✅ Created
- ✅ **ROADMAP.md** ✅ This document
- 📋 **API Reference**
  - Endpoint documentation
  - Authentication guide
  - Error codes
  - Rate limits

- 📋 **README Updates**
  - New features section
  - Updated screenshots
  - GIF demos
  - Architecture diagrams

---

## Optional Features (Use Anytime)

### Cloudflare Tunnel Check 📋

- 📋 `/v1/system/ingress` endpoint
- 📋 Returns server origin + port
- 📋 Bash script for local vs public URL test
- 📋 Document cloudflared setup

### Security & Auth Audit 📋

- 📋 JWT requirement tests for `/v1/*` endpoints
- 📋 Refresh token implementation
- 📋 Signing key rotation
- 📋 CORS policy validation

### Performance Optimization 📋

- 📋 EF Core compiled queries
- 📋 Response caching for lecture GET
- 📋 Transcript payload limit (`?full=true` query param)
- 📋 Asset CDN integration

---

## Timeline Estimate

Based on feature complexity and dependencies:

| Phase | Duration | Status |
|-------|----------|--------|
| Foundations | 1 week | ✅ Complete |
| Day 1-2 (Assets & Lectures) | 2 days | ✅ Complete |
| Day 3-4 (AI Processing) | 3-4 days | 🚧 In Progress |
| Day 5-6 (Assessment & Focus) | 3-4 days | 🚧 Partial |
| Day 7-8 (Mobile + Analytics) | 3 days | 📋 Planned |
| Day 9-10 (AI Verification) | 4-5 days | 📋 Planned |
| Day 11-12 (Design Tools) | 4-5 days | 📋 Planned |
| Day 13 (Hardening) | 2-3 days | 📋 Planned |
| Day 14 (Polish) | 2-3 days | 📋 Planned |
| **Total** | **5-6 weeks** | **~30% Complete** |

---

## Dependencies & Prerequisites

### External Services

1. **PostgreSQL** - Database & Hangfire storage
2. **Whisper.cpp** - Audio transcription
3. **LLM API** - Text generation (OpenAI/Anthropic/Ollama)
4. **Google Calendar API** - Assessment detection
5. **Canvas LMS API** - Course integration

### Infrastructure

1. **Reverse Proxy** - Nginx/Cloudflare Tunnel
2. **SSL Certificate** - Let's Encrypt
3. **Object Storage** - S3/Azure Blob (optional)
4. **Monitoring** - Grafana/Prometheus (optional)

### Development Tools

1. **Visual Studio 2022** - WPF development (Windows only)
2. **Android SDK** - MAUI mobile builds
3. **Docker** - Container deployment (optional)

---

## Contributing

This roadmap is a living document. To propose changes:

1. Open a GitHub issue with the `roadmap` label
2. Describe the feature/change
3. Explain rationale and benefits
4. Link to relevant discussions

---

Last Updated: January 2025
Version: 1.0.0
