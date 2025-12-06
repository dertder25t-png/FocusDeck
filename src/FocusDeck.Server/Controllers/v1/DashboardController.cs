using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel.Tenancy;
using System.Security.Claims;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[Route("v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public DashboardController(AutomationDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var tenantId = _currentTenant.TenantId;

        // 1. Stats
        var lecturesCount = await _db.Lectures.Where(l => l.CreatedBy == userId).CountAsync();
        var focusSessions = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.EndTime != null)
            .Select(s => new { s.StartTime, s.EndTime })
            .ToListAsync();
        var totalFocusMinutes = focusSessions.Sum(s => (s.EndTime!.Value - s.StartTime).TotalMinutes);
        var notesCount = await _db.Notes.Where(n => n.TenantId == tenantId).CountAsync();
        var projectsCount = await _db.Projects.Where(p => p.TenantId == tenantId).CountAsync();

        // 2. Tasks
        var tasks = await _db.TodoItems
            .Where(t => t.TenantId == tenantId && !t.IsCompleted)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(t => new DashboardTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                IsCompleted = t.IsCompleted,
                DueDate = t.DueDate
            })
            .ToListAsync();

        // 3. Events
        var events = await _db.EventCache
            .Where(e => e.TenantId == tenantId && e.StartTime >= DateTime.UtcNow && e.StartTime <= DateTime.UtcNow.AddDays(7))
            .OrderBy(e => e.StartTime)
            .Take(5)
            .Select(e => new DashboardEventDto
            {
                Id = e.Id.ToString(),
                Title = e.Title,
                StartTime = e.StartTime,
                IsAllDay = e.IsAllDay
            })
            .ToListAsync();

        // 4. Recent Activity (Unified Stream)
        var recentLectures = await _db.Lectures
            .Where(l => l.CreatedBy == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new DashboardActivityDto { Id = l.Id, Type = "lecture", Title = l.Title, Timestamp = l.CreatedAt, Details = l.Status.ToString() })
            .ToListAsync();

        var recentNotesActivity = await _db.Notes
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedDate)
            .Take(5)
            .Select(n => new DashboardActivityDto { Id = n.Id, Type = "note", Title = n.Title, Timestamp = n.CreatedDate, Details = "Note Created" })
            .ToListAsync();
            
        var recentProjectsActivity = await _db.Projects
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new DashboardActivityDto { Id = p.Id.ToString(), Type = "project", Title = p.Title, Timestamp = p.CreatedAt, Details = "Project Created" })
            .ToListAsync();

        var activity = recentLectures.Concat(recentNotesActivity).Concat(recentProjectsActivity)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToList();

        // 5. Habits
        var habits = await _db.Habits
            .Where(h => h.UserId == userId && h.TenantId == tenantId)
            .ToListAsync();
            
        var today = DateTime.UtcNow.Date;
        var completions = await _db.HabitCompletions
            .Where(hc => habits.Select(h => h.Id).Contains(hc.HabitId) && hc.Date >= today && hc.Date < today.AddDays(1))
            .ToListAsync();

        var habitDtos = habits.Select(h => new DashboardHabitDto
        {
            Id = h.Id,
            Title = h.Title,
            Icon = h.Icon,
            IsCompleted = completions.Any(c => c.HabitId == h.Id)
        }).ToList();

        // 6. Course Progress
        var courses = await _db.Courses
            .Where(c => c.TenantId == tenantId)
            .Select(c => new 
            {
                c.Code,
                TotalLectures = c.Lectures.Count,
                CompletedLectures = c.Lectures.Count(l => l.Status == LectureStatus.Completed)
            })
            .Take(5)
            .ToListAsync();

        var courseProgress = courses.Select(c => new DashboardCourseProgressDto
        {
            Code = c.Code ?? "Unk",
            ProgressPercent = c.TotalLectures > 0 ? (int)((double)c.CompletedLectures / c.TotalLectures * 100) : 0
        }).ToList();

        // 7. Recent Files (Notes, Decks, Projects) - For the Recent Files Widget
        // Re-using recentNotesActivity and recentProjectsActivity but purely for file listing
        var recentDecks = await _db.Decks
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Take(5)
            .Select(d => new { Id = d.Id.ToString(), Title = d.Name, Type = "deck", Timestamp = d.UpdatedAt ?? d.CreatedAt })
            .ToListAsync();

        var recentFiles = recentNotesActivity
            .Select(n => new DashboardRecentFileDto { Title = n.Title, Type = "note", Timestamp = n.Timestamp })
            .Concat(recentProjectsActivity.Select(p => new DashboardRecentFileDto { Title = p.Title, Type = "project", Timestamp = p.Timestamp }))
            .Concat(recentDecks.Select(d => new DashboardRecentFileDto { Title = d.Title ?? "Untitled Deck", Type = "deck", Timestamp = d.Timestamp }))
            .OrderByDescending(f => f.Timestamp)
            .Take(5)
            .ToList();

        return Ok(new DashboardSummaryDto
        {
            Stats = new DashboardStatsDto
            {
                Lectures = lecturesCount,
                FocusTime = Math.Round(totalFocusMinutes / 60, 1), // Return hours
                Notes = notesCount,
                Projects = projectsCount
            },
            Activity = activity,
            Tasks = tasks,
            Events = events,
            Habits = habitDtos,
            CourseProgress = courseProgress,
            RecentFiles = recentFiles
        });
    }
    
    [HttpPost("habits/{id}/toggle")]
    public async Task<IActionResult> ToggleHabit(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
        if (habit == null) return NotFound();
        
        var today = DateTime.UtcNow.Date;
        var completion = await _db.HabitCompletions
            .FirstOrDefaultAsync(hc => hc.HabitId == id && hc.Date >= today && hc.Date < today.AddDays(1));
            
        if (completion != null)
        {
            _db.HabitCompletions.Remove(completion);
        }
        else
        {
            _db.HabitCompletions.Add(new HabitCompletion { HabitId = id, Date = DateTime.UtcNow });
        }
        
        await _db.SaveChangesAsync();
        return Ok();
    }

    public class DashboardSummaryDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<DashboardActivityDto> Activity { get; set; } = new();
        public List<DashboardTaskDto> Tasks { get; set; } = new();
        public List<DashboardEventDto> Events { get; set; } = new();
        public List<DashboardHabitDto> Habits { get; set; } = new();
        public List<DashboardCourseProgressDto> CourseProgress { get; set; } = new();
        public List<DashboardRecentFileDto> RecentFiles { get; set; } = new();
    }

    public class DashboardStatsDto
    {
        public int Lectures { get; set; }
        public double FocusTime { get; set; }
        public int Notes { get; set; }
        public int Projects { get; set; }
    }

    public class DashboardTaskDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class DashboardEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public bool IsAllDay { get; set; }
    }

    public class DashboardActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } = string.Empty;
    }
    
    public class DashboardHabitDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }
    
    public class DashboardCourseProgressDto
    {
        public string Code { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
    }
    
    public class DashboardRecentFileDto
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
