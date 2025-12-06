using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel.Tenancy;
using System.Security.Claims;

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

        // Sequential execution to avoid DbContext concurrency issues
        var lecturesCount = await _db.Lectures
            .Where(l => l.CreatedBy == userId)
            .CountAsync();

        var focusSessions = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.EndTime != null)
            .Select(s => new { s.StartTime, s.EndTime })
            .ToListAsync();

        // Explicitly filter by tenant for safety, though global filters might apply
        var notesCount = await _db.Notes.Where(n => n.TenantId == tenantId).CountAsync();

        var projectsCount = await _db.Projects.Where(p => p.TenantId == tenantId).CountAsync();

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

        var totalFocusMinutes = focusSessions
            .Sum(s => (s.EndTime!.Value - s.StartTime).TotalMinutes);

        // Fetch recent activity
        // We fetch top 5 of each and combine in memory to sort and take top 10
        // This is efficient enough for a dashboard without complex UNION query
        var recentLectures = await _db.Lectures
            .Where(l => l.CreatedBy == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new DashboardActivityDto
            {
                Id = l.Id,
                Type = "lecture",
                Title = l.Title,
                Timestamp = l.CreatedAt,
                Details = l.Status.ToString()
            })
            .ToListAsync();

        var recentNotes = await _db.Notes
            .OrderByDescending(n => n.CreatedDate)
            .Take(5)
            .Select(n => new DashboardActivityDto
            {
                Id = n.Id,
                Type = "note",
                Title = n.Title,
                Timestamp = n.CreatedDate,
                Details = "Note Created"
            })
            .ToListAsync();

        var recentProjects = await _db.Projects
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new DashboardActivityDto
            {
                Id = p.Id.ToString(),
                Type = "project",
                Title = p.Title,
                Timestamp = p.CreatedAt,
                Details = "Project Created"
            })
            .ToListAsync();

        var allActivity = recentLectures
            .Concat(recentNotes)
            .Concat(recentProjects)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToList();

        return Ok(new DashboardSummaryDto
        {
            Stats = new DashboardStatsDto
            {
                Lectures = lecturesCount,
                FocusTime = totalFocusMinutes,
                Notes = notesCount,
                Projects = projectsCount
            },
            Activity = allActivity,
            Tasks = tasks,
            Events = events
        });
    }

    public class DashboardSummaryDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<DashboardActivityDto> Activity { get; set; } = new();
        public List<DashboardTaskDto> Tasks { get; set; } = new();
        public List<DashboardEventDto> Events { get; set; } = new();
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
}
