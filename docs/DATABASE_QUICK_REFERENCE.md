# Database Layer Quick Reference Guide

## Overview
FocusDeck uses Entity Framework Core with SQLite for local persistence on mobile devices. The database stores study sessions with full metadata for analytics and synchronization.

---

## Database Location
```
Windows: C:\Users\[Username]\AppData\Local\FocusDeck\focusdeck.db
```
Directory is created automatically on first app run.

---

## Key Classes

### StudySession (Entity)
Located: `FocusDeck.Shared/Models/StudySession.cs`

**Common Properties:**
```csharp
var session = new StudySession
{
    SessionId = Guid.NewGuid(),              // Auto-ID
    StartTime = DateTime.UtcNow,             // When started
    EndTime = DateTime.UtcNow.AddMinutes(25), // When ended
    DurationMinutes = 25,                    // Total time
    SessionNotes = "Studied chapter 5",      // User notes
    Status = SessionStatus.Completed,        // Current state
    FocusRate = 85,                         // 0-100 score
    BreaksCount = 2,                        // Num breaks
    BreakDurationMinutes = 5,               // Break time
    Category = "Math"                       // Subject
};
```

### StudySessionDbContext (ORM)
Located: `FocusDeck.Mobile/Data/StudySessionDbContext.cs`

Usage:
```csharp
var dbContext = new StudySessionDbContext();
await dbContext.InitializeDatabaseAsync(); // Call once on app startup
```

### ISessionRepository (Interface)
Located: `FocusDeck.Mobile/Data/Repositories/ISessionRepository.cs`

**Always inject this interface, never depend on concrete implementations.**

---

## Common Operations

### Create/Save Session
```csharp
// Inject via constructor
public class MyClass
{
    private readonly ISessionRepository _repository;
    
    public MyClass(ISessionRepository repository)
    {
        _repository = repository;
    }
}

// Save
var session = new StudySession { /* ... */ };
var saved = await _repository.CreateSessionAsync(session);
Debug.WriteLine($"Saved: {saved.SessionId}");
```

### Get Session by ID
```csharp
var sessionId = Guid.Parse("...");
var session = await _repository.GetSessionByIdAsync(sessionId);
if (session != null)
{
    Debug.WriteLine($"Duration: {session.DurationMinutes} minutes");
}
```

### Get All Sessions
```csharp
var allSessions = await _repository.GetAllSessionsAsync();
Debug.WriteLine($"Total sessions: {allSessions.Count}");
```

### Get Sessions in Date Range
```csharp
var today = DateTime.Today;
var tomorrow = DateTime.Today.AddDays(1);
var todaysSessions = await _repository.GetSessionsByDateRangeAsync(today, tomorrow);
```

### Get Recent Sessions
```csharp
var recent = await _repository.GetRecentSessionsAsync(10); // Last 10
foreach (var session in recent)
{
    Debug.WriteLine($"{session.StartTime}: {session.DurationMinutes}m");
}
```

### Update Session
```csharp
session.FocusRate = 90;
session.SessionNotes = "Updated notes";
var updated = await _repository.UpdateSessionAsync(session);
```

### Delete Session
```csharp
var deleted = await _repository.DeleteSessionAsync(sessionId);
if (deleted)
    Debug.WriteLine("Session deleted");
```

### Complete Session
```csharp
// Marks as Completed and sets EndTime if not set
var completed = await _repository.CompleteSessionAsync(sessionId);
```

### Get Statistics
```csharp
var stats = await _repository.GetSessionStatisticsAsync(
    DateTime.Today.AddDays(-7),  // Last 7 days
    DateTime.Today
);

Debug.WriteLine($"Sessions: {stats.SessionCount}");
Debug.WriteLine($"Total time: {stats.TotalMinutes}m");
Debug.WriteLine($"Avg session: {stats.AverageSessionMinutes:F1}m");
Debug.WriteLine($"Avg focus: {stats.AverageFocusRate}%");
```

### Get Total Study Time
```csharp
var totalMinutes = await _repository.GetTotalStudyTimeAsync();
Debug.WriteLine($"Lifetime study: {totalMinutes} minutes");
```

---

## Dependency Injection Setup

### In MauiProgram.cs (Already Done)
```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    // ... other setup ...
    
    builder.Services.AddMobileServices(); // Registers DbContext & Repository
    
    return builder.Build();
}
```

### In Pages/ViewModels
```csharp
// Constructor injection (PREFERRED)
public MyViewModel(ISessionRepository repository)
{
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
}

// In MAUI page with DI resolution
public partial class MyPage : ContentPage
{
    public MyPage()
    {
        InitializeComponent();
        
        var repository = Application.Current!.Handler.MauiContext!
            .Services.GetService<ISessionRepository>()
            ?? throw new InvalidOperationException("Repository not registered");
        
        BindingContext = new MyViewModel(repository);
    }
}
```

---

## Session Status Enum

```csharp
public enum SessionStatus
{
    Active = 0,      // Currently running
    Paused = 1,      // Paused but can resume
    Completed = 2,   // Finished successfully
    Canceled = 3     // Abandoned
}
```

**Example:**
```csharp
if (session.Status == SessionStatus.Completed)
{
    // Include in statistics
}

session.Status = SessionStatus.Paused;
await _repository.UpdateSessionAsync(session);
```

---

## Query Examples

### Find All Math Sessions
```csharp
var allSessions = await _repository.GetAllSessionsAsync();
var mathSessions = allSessions
    .Where(s => s.Category == "Math" && s.Status == SessionStatus.Completed)
    .OrderByDescending(s => s.StartTime)
    .ToList();
```

### Average Focus Rate for Week
```csharp
var stats = await _repository.GetSessionStatisticsAsync(
    DateTime.Today.AddDays(-7), 
    DateTime.Today
);
Debug.WriteLine($"Weekly focus rate: {stats.AverageFocusRate:F1}%");
```

### Sessions Over 1 Hour
```csharp
var allSessions = await _repository.GetAllSessionsAsync();
var longSessions = allSessions
    .Where(s => s.DurationMinutes > 60)
    .OrderByDescending(s => s.DurationMinutes)
    .ToList();
```

### Most Productive Category
```csharp
var stats = await _repository.GetSessionStatisticsAsync(
    DateTime.Today.AddDays(-30),
    DateTime.Today
);
Debug.WriteLine($"Most studied: {stats.MostCommonCategory}");
```

---

## Error Handling

### Safe Session Retrieval
```csharp
try
{
    var session = await _repository.GetSessionByIdAsync(sessionId);
    if (session == null)
    {
        Debug.WriteLine("Session not found");
        return;
    }
    // Use session
}
catch (ArgumentException ex)
{
    Debug.WriteLine($"Invalid session ID: {ex.Message}");
}
catch (Exception ex)
{
    Debug.WriteLine($"Database error: {ex.Message}");
}
```

### Safe Update
```csharp
try
{
    var updated = await _repository.UpdateSessionAsync(session);
    Debug.WriteLine("Session updated");
}
catch (InvalidOperationException ex)
{
    Debug.WriteLine($"Session not found: {ex.Message}");
}
catch (Exception ex)
{
    Debug.WriteLine($"Update failed: {ex.Message}");
}
```

---

## Performance Tips

### 1. Use Indexes for Common Queries
Indexes are already set up for:
- `StartTime` (for date range queries)
- `Status` (for filtering)
- `CreatedAt` (for chronological sorting)

### 2. Load Only Recent Data
```csharp
// Better: Load last 30 days only
var recent = await _repository.GetSessionsByDateRangeAsync(
    DateTime.Today.AddDays(-30),
    DateTime.Today
);

// vs.
// Avoid: Loading all sessions ever
var all = await _repository.GetAllSessionsAsync();
```

### 3. Use Aggregate Functions
```csharp
// Better: Single DB query
var stats = await _repository.GetSessionStatisticsAsync(start, end);

// vs.
// Avoid: Multiple queries
var sessions = await _repository.GetAllSessionsAsync();
var count = sessions.Count;
var total = sessions.Sum(s => s.DurationMinutes);
```

---

## Troubleshooting

### Database File Not Created
**Solution:** Call `await _dbContext.InitializeDatabaseAsync();` on app startup

### "Repository is null"
**Solution:** Verify `AddMobileServices()` is called in `MauiProgram.cs`

### Sessions Not Saving
**Solution:** Ensure `SaveSessionAsync()` in ViewModel is awaited properly

### Slow Queries
**Solution:** Check if date range is too large; consider using recent sessions

---

## Useful Extension Methods

Create these in a utility file for convenience:

```csharp
public static class SessionExtensions
{
    public static bool IsProductiveSession(this StudySession session) 
        => session.DurationMinutes >= 25 && session.Status == SessionStatus.Completed;
    
    public static double GetProductivityScore(this StudySession session)
        => session.IsActive ? 0 : (session.FocusRate ?? 0) / 100.0;
    
    public static int GetEffectiveMinutes(this StudySession session)
        => session.GetProductiveMinutes();
}

// Usage
var productive = session.IsProductiveSession();
var score = session.GetProductivityScore();
var effective = session.GetEffectiveMinutes();
```

---

## Database Backup/Export

```csharp
// Get database path for backup
var dbPath = StudySessionDbContext.GetDatabasePath();
Debug.WriteLine($"Database at: {dbPath}");

// Copy to backup location
var backupPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    "focusdeck_backup.db"
);
File.Copy(dbPath, backupPath, overwrite: true);
```

---

## Next: Cloud Sync (Week 4)

The database layer is now ready for OneDrive/Google Drive synchronization:

1. **Export:** Serialize sessions to JSON for cloud storage
2. **Import:** Deserialize sessions from cloud backup
3. **Merge:** Reconcile local and cloud versions
4. **Monitor:** Track sync status and conflicts

See `PHASE6b_WEEK4_PLAN.md` for cloud sync implementation details.

---

**Quick Reference Version:** 1.0  
**Last Updated:** October 28, 2025  
**Database Version:** 1.0 (SQLite 3.x)
