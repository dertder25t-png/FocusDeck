# FocusDeck AI Coding Instructions

**Last Updated:** November 4, 2025 | **Project Status:** Phase 6b Week 2 | **Features:** Remote Control, OAuth Integration, Multi-Service Support

## 🎯 What is FocusDeck?

Cross-platform productivity suite: **Windows desktop (WPF)** + **Android mobile (MAUI)** + **Linux server (.NET)** synced via encrypted cloud storage. Now featuring remote device control, multi-service integrations (Spotify, Google Calendar, Canvas, etc.), and JWT-based authentication.

## 🏗️ Critical Architecture

### Four Platform Layers (NOT Combined)

```
User Apps
├── FocusDeck.Desktop (Windows-only, .NET 9 WPF)
├── FocusDeck.Mobile  (Android, .NET 8 MAUI)
└── FocusDeck.Server  (Linux, .NET 9, cross-platform)

Shared Libraries
├── FocusDeck.Shared       (DTO/models for client-server)
├── FocusDeck.Services     (business logic)
├── FocusDeck.Domain       (domain entities)
├── FocusDeck.Contracts    (API contracts/validators)
├── FocusDeck.Persistence  (EF Core DbContext)
└── FocusDeck.SharedKernel (base interfaces)

Legacy Windows (Separate Namespace)
├── FocusDock.App     (Original WPF dock UI)
├── FocusDock.Core    (window management, automation)
├── FocusDock.Data    (JSON persistence)
└── FocusDock.System  (Win32 P/Invoke)
```

**Key Rule:** Shared libraries do NOT contain platform-specific code. Use `#if NET8_0_ANDROID` only inside `FocusDeck.Mobile` project.

### Data Flow Pattern

**Server** (source of truth) ↔ **SignalR/REST** ↔ **Client** (caches locally) ↔ **Local storage** (offline)

- Server stores in PostgreSQL/SQLite via Entity Framework (FocusDeck.Persistence)
- Clients encrypt-then-upload to OneDrive/Google Drive (end-to-end encryption via AES-256-GCM)
- Conflict resolution: last-write-wins with version history

## 🔧 Essential Build Commands

### Standard dotnet CLI

```bash
# Full solution (all platforms)
dotnet build

# Desktop only
dotnet build src/FocusDeck.Desktop/FocusDeck.Desktop.csproj

# Mobile (Android)
dotnet build src/FocusDeck.Mobile/FocusDeck.Mobile.csproj -f net8.0-android

# Server
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj

# Run Desktop locally
dotnet run --project src/FocusDeck.Desktop/FocusDeck.Desktop.csproj

# Tests
dotnet test
```

**Windows only:** Desktop/Mobile build requires MAUI workload (`dotnet workload restore`)  
**Linux/Mac:** Server builds everywhere; mobile/desktop cross-compile only for CI/CD

### BMAD-METHOD Build System

FocusDeck uses **BMAD-METHOD** (Build → Measure → Adapt → Deploy) for structured development:

```bash
# Build all modules (compiles & restores dependencies)
./tools/BMAD-METHOD/bmad build

# Measure performance & health
./tools/BMAD-METHOD/bmad measure

# Adapt: Format code & run analysis
./tools/BMAD-METHOD/bmad adapt

# Deploy to production
./tools/BMAD-METHOD/bmad deploy

# Run full cycle
./tools/BMAD-METHOD/bmad run
```

**Configuration:** See `.bmad-config.yml` for module definitions, targets, and health checks

## 📋 Code Patterns

### Server Services (FocusDeck.Server)

Registered in `Program.cs` with dependency injection:

```csharp
// Pattern: Scoped business logic service
public interface IStudyService
{
    Task<StudySessionDto> CreateSessionAsync(CreateSessionRequest req, CancellationToken ct);
}

public class StudyService : IStudyService
{
    private readonly FocusDeckDbContext _db;
    private readonly ILogger<StudyService> _logger;
    
    // RULE: Inject via constructor, resolve from DI
    public StudyService(FocusDeckDbContext db, ILogger<StudyService> logger)
    {
        _db = db;
        _logger = logger;
    }
}

// Registration in Program.cs
builder.Services.AddScoped<IStudyService, StudyService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISyncService, SyncService>();
```

**Locations:**
- Business logic: `src/FocusDeck.Server/Services/`
- Auth services: `src/FocusDeck.Server/Services/Auth/`
- Integrations: `src/FocusDeck.Server/Services/Integrations/`
- Storage: `src/FocusDeck.Server/Services/Storage/`
- Background jobs: `src/FocusDeck.Server/Jobs/`

**Scope Rules:**
- **Scoped:** Default for services with DbContext (one per HTTP request)
- **Transient:** Stateless utilities, logging helpers
- **Singleton:** Configuration, caches, connection pools

### Controller Patterns

```csharp
// Modern API controller pattern (versioned)
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/remote")]
[ApiController]
public class RemoteController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<RemoteController> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;

    public RemoteController(
        AutomationDbContext db,
        ILogger<RemoteController> logger,
        IHubContext<NotificationsHub, INotificationClient> hubContext)
    {
        _db = db;
        _logger = logger;
        _hubContext = hubContext;
    }

    [HttpPost("actions")]
    public async Task<ActionResult<RemoteActionDto>> CreateAction([FromBody] CreateRemoteActionRequest req)
    {
        var action = new RemoteAction { ... };
        _db.RemoteActions.Add(action);
        await _db.SaveChangesAsync();
        
        // Broadcast via SignalR
        await _hubContext.Clients.All.SendAsync("RemoteActionCreated", action);
        
        return CreatedAtAction(nameof(GetAction), new { id = action.Id }, action.ToDto());
    }
}

// Legacy API controller (non-versioned)
[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private const string DefaultUserId = "default_user";
    private static readonly string[] SensitiveMetadataIndicators = { "token", "secret", "password" };
    
    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var services = await _context.ConnectedServices
            .AsNoTracking()
            .ToListAsync();
        return Ok(services.Select(s => new { ... }));
    }
}
```

**Key Patterns:**
- Always return `ActionResult<T>` for proper HTTP status codes
- Use `[FromBody]` for request bodies, `[FromQuery]` for query parameters
- Return `CreatedAtAction()` for POST (201 Created)
- Return `NoContent()` for successful DELETE (204)
- Use `AsNoTracking()` for read-only queries
- Always await `SaveChangesAsync()` for mutations

### Mobile/Desktop UI (MVVM)

**Mobile (MAUI):**
```csharp
// XAML data binding to ViewModel
// src/FocusDeck.Mobile/ViewModels/StudyTimerViewModel.cs
public class StudyTimerViewModel : BaseViewModel
{
    private int _secondsRemaining;
    public int SecondsRemaining
    {
        get => _secondsRemaining;
        set => SetProperty(ref _secondsRemaining, value);
    }
    
    public Command StartSessionCommand => new Command(async () => 
        await _studyService.StartSessionAsync());
}
```

**Desktop (WPF):** Similar pattern using `INotifyPropertyChanged` in `MainWindow.xaml.cs`

### Data Models Hierarchy

```csharp
// Database entities (FocusDeck.Domain/Entities/)
public class StudySession : IAggregateRoot
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    // EF tracked
}

// API DTOs (FocusDeck.Contracts/DTOs/)
public class StudySessionDto
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    // No navigation properties - serializable
}

// Validators (FocusDeck.Contracts/Validators/)
public class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(x => x.Duration).GreaterThan(0);
    }
}
```

**Rule:** Never serialize EF entities directly - map to DTOs via AutoMapper or manual mapping

### Cloud Sync Flow

```csharp
// Encryption is ALWAYS applied before cloud upload
var encryptedData = await _encryptionService.EncryptAsync(data);
await _cloudProvider.UploadAsync(encryptedData);

// Decryption after download
var decrypted = await _encryptionService.DecryptAsync(encrypted);

// Device registry prevents sync loops
await _deviceRegistry.RegisterDeviceAsync(deviceId);
```

## � Entity Framework Core Patterns

### DbContext Setup (FocusDeck.Persistence)

```csharp
// AutomationDbContext.cs - Single DbContext for entire application
public class AutomationDbContext : DbContext
{
    public AutomationDbContext(DbContextOptions<AutomationDbContext> options)
        : base(options) { }

    // All domain entities
    public DbSet<StudySession> StudySessions { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<Automation> Automations { get; set; }
    public DbSet<DeviceRegistration> DeviceRegistrations { get; set; }
    public DbSet<SyncTransaction> SyncTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Apply all IEntityTypeConfiguration<T> from Configurations/ folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutomationDbContext).Assembly);
    }
}
```

### Entity Configuration Pattern

```csharp
// StudySessionConfiguration.cs - Fluent API config per entity
public class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        // Key
        builder.HasKey(e => e.SessionId);
        builder.Property(e => e.SessionId).ValueGeneratedNever(); // Use Guid.NewGuid()

        // Required properties
        builder.Property(e => e.StartTime).IsRequired();
        builder.Property(e => e.DurationMinutes).IsRequired();
        builder.Property(e => e.Status).IsRequired();

        // Optional with defaults
        builder.Property(e => e.BreaksCount).HasDefaultValue(0);
        builder.Property(e => e.Category).HasMaxLength(120);

        // Indexes for query performance
        builder.HasIndex(e => e.StartTime);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.StartTime }); // Composite index
    }
}
```

### Database Registration (Program.cs)

```csharp
// Auto-detects PostgreSQL vs SQLite from connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=focusdeck.db";

if (connectionString.Contains("Host=") || connectionString.Contains("Server="))
{
    // PostgreSQL for production
    builder.Services.AddDbContext<AutomationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // SQLite for development
    builder.Services.AddDbContext<AutomationDbContext>(options =>
        options.UseSqlite(connectionString));
}
```

**Location:** `src/FocusDeck.Persistence/`  
**Key Pattern:** One DbContext, configuration per entity in `Configurations/` folder  
**Scope:** Always register DbContext as Scoped (one per HTTP request)

### Entity Design Pattern

```csharp
// StudySession.cs - Domain entity with metadata
public class StudySession
{
    [Key]
    public Guid SessionId { get; set; } = Guid.NewGuid();
    
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }  // Nullable = optional
    public int DurationMinutes { get; set; }
    
    // System fields (managed by middleware or service)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Status enum
    public SessionStatus Status { get; set; } = SessionStatus.Active;
}

public enum SessionStatus { Active, Paused, Completed, Canceled }
```

**Rule:** Every entity has `CreatedAt` + `UpdatedAt` for auditing and sync versioning.

### Query Pattern (Services)

```csharp
// StudyService.cs - Scoped service pattern
public class StudyService
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<StudyService> _logger;
    
    public StudyService(AutomationDbContext db, ILogger<StudyService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    // Query pattern: async, tracked if modifying
    public async Task<StudySessionDto> GetSessionAsync(Guid id, CancellationToken ct)
    {
        var session = await _db.StudySessions
            .AsNoTracking()  // Read-only queries
            .FirstOrDefaultAsync(x => x.SessionId == id, ct);
        
        return session?.ToDto();
    }
    
    // Mutation pattern: use UnitOfWork or SaveChangesAsync
    public async Task<StudySessionDto> CreateSessionAsync(CreateSessionRequest req, CancellationToken ct)
    {
        var session = new StudySession 
        { 
            StartTime = DateTime.UtcNow,
            DurationMinutes = req.DurationMinutes,
            Status = SessionStatus.Active
        };
        
        _db.StudySessions.Add(session);
        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("Session created: {SessionId}", session.SessionId);
        return session.ToDto();
    }
}
```

**Key Rules:**
- Use `AsNoTracking()` for read-only queries (better performance)
- Always `await SaveChangesAsync()`, never `.SaveChanges()`
- Pass `CancellationToken` to async methods
- Log state changes for auditing

## �📁 File Organization Rules

**By Feature (Server):**
```
src/FocusDeck.Server/
├── Controllers/StudySessions/     # API endpoints
├── Services/StudySessions/        # Business logic
├── Jobs/StudySessionJobs.cs       # Background tasks (Hangfire)
└── Hubs/StudySessionHub.cs        # Real-time SignalR
```

**By Platform (Mobile):**
```
src/FocusDeck.Mobile/
├── Pages/StudyTimer/              # XAML UI
├── ViewModels/StudyTimerViewModel.cs
├── Services/StudySessionService.cs
└── Platforms/Android/             # Android-specific P/Invoke
```

**Data Access (Persistence):**
```
src/FocusDeck.Persistence/
├── AutomationDbContext.cs         # Main DbContext
├── Configurations/                # Entity fluent API
│   ├── StudySessionConfiguration.cs
│   ├── NoteConfiguration.cs
│   └── AutomationConfiguration.cs
├── Migrations/                    # EF Core migration scripts
└── AutomationDbContextFactory.cs  # Design-time factory
```

## 🔌 Integration Points

### API Versioning

Controllers use **API versioning** with namespace organization:

```csharp
// Versioned controllers (v1 pattern)
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/remote")]
public class RemoteController : ControllerBase { }

// Non-versioned controllers (backward compatibility)
[Route("api/[controller]")]
public class ServicesController : ControllerBase { }
```

**Location:** `src/FocusDeck.Server/Controllers/` and `src/FocusDeck.Server/Controllers/v1/`

### Server → Client (SignalR Real-Time)

```csharp
// Server notifies clients of new study sessions
await _hubContext.Clients.All.SendAsync("SessionStarted", session);

// Remote control notifications
await _hubContext.Clients.All.SendAsync("RemoteActionCreated", action);
await _hubContext.Clients.Group(deviceId).SendAsync("RemoteTelemetry", telemetry);

// Client listens
HubConnection.On<StudySessionDto>("SessionStarted", session => {...});
HubConnection.On<RemoteActionDto>("RemoteActionCreated", action => {...});
```

**Location:** `src/FocusDeck.Server/Hubs/NotificationsHub.cs`  
**Interface:** `INotificationClient` defines all hub method signatures

### External APIs (Canvas, Google Calendar, Spotify, etc.)

```csharp
// Adapter pattern for external services
public interface ICanvasService
{
    Task<List<AssignmentDto>> GetAssignmentsAsync();
}

// OAuth flow
// 1. Get OAuth URL: GET /api/services/oauth/{service}/url
// 2. User authenticates with provider
// 3. Provider redirects to: GET /api/services/oauth/{service}/callback?code={code}
// 4. Service fetches and stores token in ConnectedService entity
// 5. Future requests use stored token

// Implementation locations:
// - Google Calendar: `src/FocusDeck.Server/Services/Integrations/GoogleCalendarService.cs`
// - Canvas: `src/FocusDeck.Server/Services/Integrations/CanvasService.cs`
// - Setup guides: `src/FocusDeck.Server/Controllers/Support/ServiceSetupGuideFactory.cs`
```

**API Keys & Credentials:**
- Stored in database (`ServiceConfiguration` entity) or `appsettings.Production.json`
- User secrets for development: `dotnet user-secrets set "Jwt:Key" "..."`
- Sensitive metadata masked in responses (security)

### Background Jobs (Hangfire)

```csharp
// Schedule recurring job
RecurringJob.AddOrUpdate<ISyncService>(
    "sync-study-sessions",
    x => x.SyncAsync(CancellationToken.None),
    Cron.Hourly);

// Server update jobs
RecurringJob.AddOrUpdate<IServerUpdateService>(
    "check-updates",
    x => x.CheckForUpdatesAsync(CancellationToken.None),
    Cron.Daily);
```

**Location:** `src/FocusDeck.Server/Jobs/` and `Program.cs` registration

## 🆕 Recent Features (November 2025)

### Remote Device Control (`/v1/remote`)

Cross-device action execution with real-time SignalR updates:

```csharp
// RemoteController - New endpoints
[HttpPost("actions")]  // Phone → Desktop: submit action (click window, pause, etc.)
[HttpGet("actions?pending=true")]  // Desktop: fetch pending remote actions
[HttpPost("actions/{id}/complete")]  // Desktop: mark action as done
[HttpGet("telemetry/summary")]  // Phone: get desktop telemetry

// Domain entities (FocusDeck.Domain/Entities/Remote/)
public class RemoteAction { Guid Id, string Action, DateTime CreatedAt, etc. }
public class DeviceLink { Guid Id, string SourceDeviceId, string TargetDeviceId }
```

**SignalR Integration:**
- Server broadcasts `RemoteActionCreated` when phone creates action
- Desktop sends `SendTelemetry` with current session, progress, notes
- Clients listen on `RemoteTelemetry` event
- **Location:** `src/FocusDeck.Server/Hubs/NotificationsHub.cs`

### OAuth + Multi-Service Integration

Support for **Spotify, Google Calendar, Google Drive, Canvas, Apple Music, Home Assistant, etc.**

```csharp
// ServicesController - Service management
[HttpPost("connect/{service}")]  // Connect service via credentials/OAuth
[HttpGet("oauth/{service}/url")]  // Get OAuth consent URL
[HttpGet("oauth/{service}/callback")]  // OAuth redirect handler
[HttpDelete("{id}")]  // Disconnect service

// Domain entities
public class ConnectedService  // OAuth tokens, metadata
public class ServiceConfiguration  // API credentials (ClientId, ClientSecret, ApiKey)

// Enums (FocusDeck.Domain/Entities/)
public enum ServiceType { GoogleCalendar, GoogleDrive, Spotify, Canvas, AppleMusic, HomeAssistant... }
```

**Key Patterns:**
- Database stores OAuth tokens with expiry
- Sensitive metadata masked in responses (security)
- Setup guides per service (OAuth vs Simple API key)
- Health check endpoint to verify service connectivity

### JWT Authentication

**Token Service** (`src/FocusDeck.Server/Services/Auth/TokenService.cs`):

```csharp
public interface ITokenService
{
    string GenerateAccessToken(string userId, string[] roles);  // 60-min expiry
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    string ComputeTokenHash(string token);
    string ComputeClientFingerprint(string? clientId, string? userAgent);
}
```

**Configuration** (appsettings.Production.json):
```json
{
  "Jwt": {
    "Key": "256-bit-base64-key",
    "Issuer": "https://focusdeck.909436.xyz",
    "Audience": "focusdeck-clients",
    "AccessTokenExpirationMinutes": 60
  }
}
```

**Controllers:** Use `[Authorize]` attribute on endpoints requiring JWT

### Focus Sessions & FocusSignal

Real-time focus mode tracking:

```csharp
public class FocusSession  // Device focus state (active, paused, complete)
public class FocusPolicy   // Rules for focus (app whitelist, break intervals)
public class FocusSignal    // Signal emission from mobile to server
```

**Server Endpoint:** `/v1/focus/signal` - Mobile sends focus telemetry

### Design System & Decks

**DesignController** (`/v1/design`) - UI theme management:
- Save/retrieve user design preferences (colors, typography)

**DecksController** - Study deck management

## 🏗️ BMAD-METHOD Build Lifecycle

**BMAD** is FocusDeck's structured development methodology: **B**uild → **M**easure → **A**dapt → **D**eploy

This creates a **looped pipeline** that runs locally during development and automatically on GitHub Actions for CI/CD.

### Phase 1: BUILD

**Goal:** Compile code, restore dependencies, validate all modules compile

```bash
# Build all modules (Server, Shared, Domain, Persistence, Contracts, Desktop, Mobile)
./tools/BMAD-METHOD/bmad build

# Or specific module
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release
```

**What happens:**
- ✅ Restores NuGet packages
- ✅ Compiles .NET code
- ✅ Validates project references
- ✅ Outputs to `./bin` and `./publish`

**Modules in order (dependency chain):**
1. **Domain** - Base entities (StudySession, RemoteAction, FocusSession)
2. **Persistence** - EF Core DbContext & configurations
3. **Contracts** - DTOs & validators
4. **Shared** - Cross-platform DTOs
5. **Server** - REST API, SignalR, business logic
6. **Desktop** - Windows WPF client
7. **Mobile** - Android MAUI client

### Phase 2: MEASURE

**Goal:** Verify health, run tests, collect performance metrics

```bash
# Full measurement cycle
./tools/BMAD-METHOD/bmad measure

# Includes:
# - Health checks (http://localhost:5000/healthz)
# - API validation (http://localhost:5000/api/services)
# - Unit tests (dotnet test)
# - Integration tests
# - Performance metrics
# - Logging analysis
```

**What happens:**
- ✅ Runs health checks on running server
- ✅ Executes unit & integration tests
- ✅ Collects Serilog telemetry
- ✅ Measures API response times (target: P95 < 500ms)
- ✅ Generates test coverage report (minimum: 70%)

**Output:** Test results in `./test-results/`, metrics in `./logs/`

### Phase 3: ADAPT

**Goal:** Improve code quality, fix issues, refactor

```bash
# Full adaptation cycle
./tools/BMAD-METHOD/bmad adapt

# Includes:
# - Format code (dotnet format)
# - Static analysis (Roslyn analyzers)
# - Security scanning (vulnerable packages)
# - Dependency update checks
```

**What happens:**
- ✅ Auto-formats C# code
- ✅ Runs linting/analysis
- ✅ Checks for security vulnerabilities
- ✅ Reports outdated packages
- ✅ Suggests refactoring opportunities

**Output:** Analysis reports in `./.bmad/adapt-results/`

### Phase 4: DEPLOY

**Goal:** Publish build artifacts, deploy to production

```bash
# Deploy to Linux server
./tools/BMAD-METHOD/bmad deploy

# Manual deploy (if needed)
dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj \
  -c Release \
  -o ./publish/server \
  --self-contained
```

**What happens:**
- ✅ Publishes FocusDeck.Server in Release mode
- ✅ Creates self-contained bundle
- ✅ SSH deploys to Linux server (192.168.1.110)
- ✅ Stops systemd service, updates binaries, restarts
- ✅ Verifies health check endpoint
- ✅ Rolls back on failure

**Configuration:** SSH secrets required in GitHub (DEPLOY_HOST, DEPLOY_USER, DEPLOY_KEY)

### Developer Workflow (Local)

When working on a feature:

```bash
# 1. Create feature branch
git checkout -b feature/my-awesome-feature

# 2. Make code changes
# ... edit src/FocusDeck.Server/Controllers/MyController.cs ...

# 3. Build locally
./tools/BMAD-METHOD/bmad build

# 4. Run tests
dotnet test

# 5. Measure performance
./tools/BMAD-METHOD/bmad measure

# 6. Adapt (format & analyze)
./tools/BMAD-METHOD/bmad adapt

# 7. Commit & push
git add -A
git commit -m "Add my awesome feature"
git push origin feature/my-awesome-feature
```

### CI/CD Workflow (GitHub Actions)

When you push to `master` or `develop`:

```yaml
# .github/workflows/focusdeck-bmad.yml runs automatically:

1. Build (Windows + Linux)
   ├─ Compile all modules
   ├─ Run unit tests
   └─ Check code quality

2. Measure
   ├─ Health checks
   ├─ Performance metrics
   └─ Test coverage

3. Adapt
   ├─ Code analysis
   ├─ Security scan
   └─ Generate reports

4. Deploy (master only)
   ├─ Publish server build
   ├─ SSH to Linux server
   ├─ Deploy systemd service
   └─ Verify health check
```

### BMAD Configuration Reference

**File:** `.bmad-config.yml`

Key sections:

```yaml
# Build modules
build:
  modules:
    - name: Server
      path: src/FocusDeck.Server
      build: dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Debug

# Health checks
measure:
  health_checks:
    - name: server_health
      url: http://localhost:5000/healthz
      expected_status: 200

# Deployment targets
deploy:
  targets:
    - name: linux_server
      host: 192.168.1.110
      systemd_service: focusdeck
```

### Troubleshooting BMAD

**Problem: Build fails**
```bash
# Clear build cache
dotnet clean
rm -r src/*/bin src/*/obj

# Restore & rebuild
dotnet restore
./tools/BMAD-METHOD/bmad build
```

**Problem: Health check timeout**
```bash
# Ensure server is running
dotnet run --project src/FocusDeck.Server/FocusDeck.Server.csproj &

# Check port 5000 is listening
netstat -an | grep 5000

# Test manually
curl http://localhost:5000/healthz
```

**Problem: Deploy fails**
```bash
# Verify SSH credentials
ssh -i ~/.ssh/deploy_key focusdeck@192.168.1.110 "echo OK"

# Check GitHub secrets are set
# Settings → Secrets → DEPLOY_HOST, DEPLOY_USER, DEPLOY_KEY
```

## 🧪 Testing Strategy

- **Unit tests:** Mock services, test business logic (services layer)
- **Integration tests:** Database + EF (use in-memory or SQLite test DB)
- **Manual testing:** UI behaviors (XAML data binding validation)

Run: `dotnet test` (includes FocusDeck.Mobile.Tests, Server tests, etc.)

## 🚨 Common Mistakes to Avoid

1. **Cross-platform code in platform projects:** Don't reference `System.Windows` in `.Mobile` (MAUI).
2. **Blocking async:** Never use `.Result` or `.Wait()` - always `await`.
3. **Unencrypted cloud uploads:** All user data must pass through encryption service.
4. **Missing DI registration:** New services must be added in `Program.cs`.
5. **Circular dependencies:** Check project references—data never imports Core or Services.
6. **Hardcoded API keys:** Use configuration/user secrets, never in code.

## 📚 Key Documentation

- **Architecture:** `PLATFORM_ARCHITECTURE.md`
- **MAUI structure:** `docs/MAUI_ARCHITECTURE.md`
- **Cloud sync:** `docs/CLOUD_SYNC_ARCHITECTURE.md`
- **API contracts:** `docs/DATABASE_API_REFERENCE.md`
- **Build config:** `docs/BUILD_CONFIGURATION.md`
- **Quick start:** `QUICK_START.md`

## 🔄 Workflow Summary

1. **Feature branch:** `git checkout -b feature/your-feature`
2. **Build locally:** `dotnet build` (all projects)
3. **Test:** `dotnet test`
4. **Commit:** `git commit -m "Add your feature"`
5. **Push:** `git push origin feature/your-feature` → GitHub PR
6. **CI/CD:** GitHub Actions tests on Windows + Linux
