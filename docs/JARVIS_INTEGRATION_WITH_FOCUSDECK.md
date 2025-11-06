#  JARVIS Integration with Existing FocusDeck Architecture

**Version:** 1.0  
**Date:** November 5, 2025  
**Context:** How JARVIS layers on top of current FocusDeck systems  

---

## Current FocusDeck Architecture (Phase 6b)

### Existing Layers (What's Already Built)

`

                     User Applications                        

  Desktop (WPF)      Mobile (MAUI)    Linux Server (Web)  
  Windows-only      Android target    .NET 9 cross-plat  

                                                

              REST API + SignalR (FocusDeck.Server)           

  Controllers: /api/studies, /api/notes, /api/focus, etc.  
  Hubs: NotificationsHub (real-time)                       
  Jobs: Hangfire background tasks                          

         

              Services Layer (FocusDeck.Services)             

  IStudyService - Session tracking                         
  IFocusService - Focus mode management                    
  ICanvasService - Canvas API integration                  
  INoteService - Note management                           
  IEncryptionService - End-to-end encryption               
  ISyncService - Cloud sync (OneDrive/Google Drive)        

         

         Persistence Layer (FocusDeck.Persistence)            

  AutomationDbContext (EF Core)                            
  Entities: StudySession, Note, DeviceRegistration, etc.  
  Migrations: Managed via BMAD                             
  Supports: PostgreSQL (prod) + SQLite (dev)               

         

                 PostgreSQL / SQLite Database                 

`

---

## JARVIS Integration Points

### Layer 1: Activity Detection (NEW)

`
Win/Linux/Mobile Sensors
  
IActivityDetectionService (NEW)
   WindowsActivityDetectionService (Desktop)
   LinuxActivityDetectionService (Server)
   MobileActivityDetectionService (Mobile)
  
IContextAggregationService (NEW)
   Uses existing services:
   ICanvasService (to get assignments)
   INoteService (to get open notes)
   IStudyService (to get current session)
`

### Layer 2: Analysis Services (NEW)

`
IContextAggregationService
  
IBurnoutAnalysisService (NEW)
   Uses: StudentWellnessMetrics (DB)
   Publishes: BurnoutAlert  SignalR
  
INotificationFilterService (NEW)
   Uses: NotificationPreference (DB)
   Filters: Incoming notifications
  
IFlashcardGenerationService (NEW)
   Uses: INoteService (existing)
   Creates: GeneratedFlashcard (DB)
  
IEssayDraftService (NEW)
   Uses: ICanvasService (existing)
   Creates: EssayDraft (DB)
  
IWorkspacePreparationService (NEW)
   Uses: Platform-specific P/Invoke
   Returns: Preparation results
  
IProductivityMetricsService (NEW)
   Uses: FocusSession, StudentWellnessMetrics (DB)
   Creates: WeeklyProductivityReport (DB)
`

### Layer 3: Integration Points in Controllers

`
EXISTING Controllers:
 StudySessionsController (manages sessions)
 NotesController (manages notes)
 SettingsController (user preferences)
 RemoteController (device control)

NEW JARVIS Controllers (optional):
 ActivityController (expose activity state)
 WellnessController (wellness metrics)
 NotificationsController (filter preferences)
 ContentGenerationController (draft creation)
`

---

## Database Schema: New JARVIS Entities

### Already Exist (Don't Touch)
`
StudySession
  - SessionId: Guid (PK)
  - StartTime: DateTime
  - EndTime: DateTime?
  - DurationMinutes: int
  - Status: SessionStatus enum
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

Note
  - NoteId: Guid (PK)
  - Title: string
  - Content: string (encrypted on server, local on client)
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

DeviceRegistration
  - Id: Guid (PK)
  - DeviceId: string
  - UserId: Guid
  - RegisteredAt: DateTime
`

### NEW for JARVIS

`csharp
// Phase 1: Activity Context
StudentContext
  - Id: Guid (PK)
  - StudentId: Guid (FK)  User
  - CurrentApp: string?
  - FocusLevel: int (0-100)
  - IsIdle: bool
  - ActivityIntensity: int (0-100)
  - Timestamp: DateTime
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

// Phase 2: Wellness Tracking
StudentWellnessMetrics
  - Id: Guid (PK)
  - StudentId: Guid (FK)  User
  - Date: DateTime
  - HoursWorked: int
  - BreakFrequency: int
  - QualityScore: double (0-10)
  - SleepHours: int
  - BurnoutScore: double (0-100)
  - CreatedAt: DateTime

// Phase 3: Notifications
NotificationPreference
  - Id: Guid (PK)
  - StudentId: Guid (FK)  User
  - ContactOrApp: string (contact name or app)
  - Priority: NotificationPriority enum
  - IsWhitelisted: bool
  - BatchSize: int
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

// Phase 4: Content Generation
GeneratedFlashcard
  - Id: Guid (PK)
  - CanvasAssignmentId: string
  - Question: string
  - Answer: string
  - MasteryScore: double (0-100)
  - StudentId: Guid (FK)  User
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

EssayDraft
  - Id: Guid (PK)
  - CanvasAssignmentId: string
  - Outline: string (JSON)
  - Thesis: string
  - Quotes: List<string> (JSON)
  - Completeness: double (0-100)
  - StudentId: Guid (FK)  User
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

// Phase 5: Metrics
FocusSessionMetrics
  - Id: Guid (PK)
  - StudySessionId: Guid (FK)  StudySession
  - DurationMinutes: int
  - DistractionsBlocked: int
  - QualityScore: double (0-10)
  - CreatedAt: DateTime

WeeklyProductivityReport
  - Id: Guid (PK)
  - StudentId: Guid (FK)  User
  - WeekStart: DateTime
  - TasksCompleted: int
  - HoursWorked: int
  - DeepFocusMinutes: int
  - DistractionsBlocked: int
  - BreaksTaken: int
  - SleepAverage: double
  - BurnoutScore: double
  - BalanceScore: double (0-100)
  - CreatedAt: DateTime

StudentPreferences
  - Id: Guid (PK)
  - StudentId: Guid (FK)  User
  - OptimalFocusLengthMinutes: int
  - PreferredBreakType: string
  - NotificationAggressiveness: int (0-10)
  - CreatedAt: DateTime
  - UpdatedAt: DateTime
`

---

## Service Registration: How JARVIS Integrates with DI

### In Program.cs (FocusDeck.Server)

`csharp
// EXISTING registrations (no changes needed!)
builder.Services.AddScoped<IStudyService, StudyService>();
builder.Services.AddScoped<IFocusService, FocusService>();
builder.Services.AddScoped<ICanvasService, CanvasService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ISyncService, SyncService>();

// NEW JARVIS registrations (add below existing)

// Phase 1: Activity Detection
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddScoped<IActivityDetectionService, WindowsActivityDetectionService>();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddScoped<IActivityDetectionService, LinuxActivityDetectionService>();
}

builder.Services.AddScoped<IContextAggregationService, ContextAggregationService>();

// Phase 2: Wellness
builder.Services.AddScoped<IBurnoutAnalysisService, BurnoutAnalysisService>();
builder.Services.AddScoped<IWellnessService, WellnessService>();

// Phase 3: Notifications
builder.Services.AddScoped<INotificationFilterService, NotificationFilterService>();

// Phase 4: Content Generation
builder.Services.AddScoped<IFlashcardGenerationService, FlashcardGenerationService>();
builder.Services.AddScoped<IEssayDraftService, EssayDraftService>();
builder.Services.AddScoped<IStudyGuideService, StudyGuideService>();

// Phase 5: Workspace
builder.Services.AddScoped<IWorkspacePreparationService, WorkspacePreparationService>();

// Phase 6: Metrics
builder.Services.AddScoped<IProductivityMetricsService, ProductivityMetricsService>();
builder.Services.AddScoped<IPersonalizationService, PersonalizationService>();

// Background Jobs (using existing Hangfire)
RecurringJob.AddOrUpdate<IContextAggregationService>(
    "aggregate-student-context",
    x => x.GetAggregatedContextAsync(Guid.Empty, CancellationToken.None),
    Cron.EveryMinute);

RecurringJob.AddOrUpdate<IBurnoutAnalysisService>(
    "check-burnout-patterns",
    x => x.AnalyzePatternsAsync(Guid.Empty, CancellationToken.None),
    Cron.Every(120));  // Every 2 hours

RecurringJob.AddOrUpdate<IProductivityMetricsService>(
    "calculate-weekly-metrics",
    x => x.GenerateReportAsync(Guid.Empty, CancellationToken.None),
    Cron.Daily("18:00"));  // 6pm daily
`

---

## Existing Services: How JARVIS Reuses Them

### ICanvasService (Existing)
**Used by:**
- ContextAggregationService (get upcoming assignments)
- FlashcardGenerationService (get assignment details)
- EssayDraftService (parse assignment prompts)

**No changes needed** - JARVIS queries existing methods

### INoteService (Existing)
**Used by:**
- ContextAggregationService (get open notes)
- FlashcardGenerationService (extract key concepts)

**No changes needed** - JARVIS reads existing notes

### IStudyService (Existing)
**Used by:**
- ContextAggregationService (current session info)
- ProductivityMetricsService (compute study hours)

**Possible enhancement:**
`csharp
// Extend IStudyService with JARVIS-specific method?
public interface IStudyService
{
    // Existing methods...
    
    // NEW: Get focus quality score for session
    Task<double> GetFocusQualityScoreAsync(Guid sessionId, CancellationToken ct);
}
`

### IFocusService (Existing)
**Used by:**
- NotificationFilterService (honor focus mode)
- WorkspacePreparationService (enable focus during prep)

**Possible enhancement:**
`csharp
// NEW method: Prep workspace when focus starts?
Task PrepareWorkspaceAsync(Guid studentId, CancellationToken ct);
`

---

## SignalR Hub: Existing NotificationsHub

### Current Methods (No Changes)
`csharp
// From INotificationClient interface
Task SendAsync(string method, params object[] args);
Task SendNotification(Notification notification);
`

### NEW Hub Methods for JARVIS

`csharp
// In INotificationClient interface (add these)
Task SendBurnoutAlert(BurnoutAlert alert);
Task SendContextUpdated(StudentContext context);
Task SendWorkspaceReady(WorkspacePreparation prep);
Task SendFocusTimer(FocusTimerUpdate timer);
Task SendMetricsUpdate(WeeklyProductivityReport report);

// In NotificationsHub (add these methods)
public async Task BroadcastBurnoutAlert(Guid studentId, BurnoutAlert alert)
{
    await Clients.User(studentId.ToString())
        .SendBurnoutAlert(alert);
}

public async Task BroadcastContextUpdate(Guid studentId, StudentContext context)
{
    await Clients.User(studentId.ToString())
        .SendContextUpdated(context);
}
`

---

## Migrations: How to Add JARVIS Entities

### Using BMAD

`ash
# After Phase 1 design is complete:
dotnet ef migrations add AddActivityDetectionTables \
  -p src/FocusDeck.Persistence \
  -s src/FocusDeck.Server

# After Phase 2 design:
dotnet ef migrations add AddWellnessMetricsTable \
  -p src/FocusDeck.Persistence \
  -s src/FocusDeck.Server

# And so on for each phase...
`

### Or Using BMAD Scripts
`ash
./bmad build  # Compiles with new DbContext
./bmad adapt  # Runs migrations (if needed)
`

---

## Folder Structure: Where JARVIS Code Lives

`
src/FocusDeck.Services/  (EXISTING - add JARVIS here)
 Existing/
    StudyService.cs
    FocusService.cs
    CanvasService.cs
    NoteService.cs

 Activity/  (NEW - Phase 1)
    IActivityDetectionService.cs
    ActivityDetectionService.cs (abstract)
    WindowsActivityDetectionService.cs
    LinuxActivityDetectionService.cs
    IContextAggregationService.cs

 Wellness/  (NEW - Phase 2)
    IBurnoutAnalysisService.cs
    BurnoutAnalysisService.cs
    IWellnessService.cs

 Notifications/  (NEW - Phase 3)
    INotificationFilterService.cs
    NotificationFilterService.cs

 ContentGeneration/  (NEW - Phase 4)
    IFlashcardGenerationService.cs
    FlashcardGenerationService.cs
    IEssayDraftService.cs
    EssayDraftService.cs

 Workspace/  (NEW - Phase 5)
    IWorkspacePreparationService.cs
    WorkspacePreparationService.cs

 Metrics/  (NEW - Phase 6)
     IProductivityMetricsService.cs
     ProductivityMetricsService.cs
     IPersonalizationService.cs

src/FocusDeck.Desktop/  (EXISTING - add JARVIS impl)
 Services/
    WindowsActivityDetectionService.cs  (NEW - Phase 1)
 Views/  (existing)

src/FocusDeck.Mobile/  (EXISTING - add JARVIS impl)
 Services/
    MobileActivityDetectionService.cs  (NEW - Phase 1)
 ViewModels/  (existing)

src/FocusDeck.Server/  (EXISTING - add JARVIS endpoints)
 Controllers/
    ActivityController.cs  (NEW - Phase 1, optional)
    WellnessController.cs  (NEW - Phase 2, optional)
    MetricsController.cs  (NEW - Phase 6, optional)
 Hubs/
    NotificationsHub.cs  (MODIFY - add new methods)
 Jobs/  (EXISTING - add JARVIS jobs)
     ActivityPollingJob.cs  (NEW)
     BurnoutCheckJob.cs  (NEW)
     MetricsCalculationJob.cs  (NEW)

src/FocusDeck.Domain/Entities/  (EXISTING - add JARVIS entities)
 Existing/
    StudySession.cs
    Note.cs
    DeviceRegistration.cs

 JARVIS/  (NEW folder)
    StudentContext.cs
    StudentWellnessMetrics.cs
    NotificationPreference.cs
    GeneratedFlashcard.cs
    EssayDraft.cs
    FocusSessionMetrics.cs
    WeeklyProductivityReport.cs
    StudentPreferences.cs

src/FocusDeck.Persistence/Configurations/  (EXISTING - add JARVIS configs)
 Existing/
    StudySessionConfiguration.cs
    NoteConfiguration.cs

 JARVIS/  (NEW folder)
     StudentContextConfiguration.cs
     StudentWellnessMetricsConfiguration.cs
     NotificationPreferenceConfiguration.cs
     GeneratedFlashcardConfiguration.cs
     EssayDraftConfiguration.cs
     FocusSessionMetricsConfiguration.cs
     WeeklyProductivityReportConfiguration.cs
     StudentPreferencesConfiguration.cs
`

---

## Project File Updates: No Breaking Changes

All existing project files remain unchanged:
- FocusDeck.Server.csproj - No modifications
- FocusDeck.Services.csproj - No modifications
- FocusDeck.Domain.csproj - No modifications
- FocusDeck.Persistence.csproj - No modifications
- FocusDeck.Desktop.csproj - No modifications
- FocusDeck.Mobile.csproj - No modifications

JARVIS code uses existing NuGet packages. No new dependencies needed.

---

## Risk Mitigation: How JARVIS Doesn't Break Existing Code

 **Backward Compatible**
- All new services are additional, not replacing existing ones
- Existing controllers unchanged
- Existing database entities untouched
- Can deploy incrementally (each phase independent)

 **Graceful Degradation**
- If activity detection fails, app still works (without JARVIS features)
- If context aggregation times out, system doesn't crash
- If burnout analysis encounters error, continues normal operation

 **Feature Flags (Optional)**
- Could wrap JARVIS features behind feature flags
- Allow disabling JARVIS entirely for performance
- Roll out to 10% of users first, then 100%

---

## Testing Strategy: Leverages Existing Test Suite

### Existing Tests (Don't Touch)
- StudyService tests (existing)
- FocusService tests (existing)
- CanvasService tests (existing)
- Note encryption tests (existing)

### NEW Tests (Add Alongside)
- ActivityDetectionService tests (mock platform APIs)
- ContextAggregationService tests (mock Canvas/Note services)
- BurnoutAnalysisService tests (fake student metrics)
- FlashcardGenerationService tests (mock note content)
- And so on...

**Test structure:**
`
tests/
 FocusDeck.Server.Tests/  (existing)
    Services/Existing/  (existing tests)
    Services/JARVIS/  (NEW tests)
        ActivityDetectionServiceTests.cs
        BurnoutAnalysisServiceTests.cs
        FlashcardGenerationServiceTests.cs
        ... (one per JARVIS service)
`

---

## Deployment: No Changes to DevOps

Existing BMAD deployment process still works:

`ash
./bmad build    # Compiles JARVIS code too
./bmad measure  # Tests JARVIS features
./bmad adapt    # Code analysis on JARVIS code
./bmad deploy   # Deploys everything together
`

No changes to:
- CI/CD pipeline
- GitHub Actions workflows
- Linux server deployment
- Docker Compose setup
- Database migration process

---

## Summary: JARVIS Fits Seamlessly Into FocusDeck

| Aspect | Impact | Status |
|--------|--------|--------|
| Existing services | Reused, not modified |  Safe |
| Database | New tables only |  Backward compat |
| API endpoints | New only |  No breaking changes |
| SignalR hubs | Extended, not modified |  Additive |
| Deployment | Unchanged process |  No new tooling |
| Dependencies | Uses existing packages |  No conflicts |
| Testing | New tests alongside |  Parallel suites |

**Bottom line:** JARVIS is a pure *addition* to FocusDeck, not a refactor. You can build it incrementally, phase by phase, without touching existing code.

---

**Next:** Start Phase 1 with the task breakdown in JARVIS_PHASE1_DETAILED.md
