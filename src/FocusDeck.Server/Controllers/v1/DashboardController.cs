using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FocusDeck.Persistence;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[Route("v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AutomationDbContext _db;

    public DashboardController(AutomationDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Parallel execution for stats to ensure speed
        var lecturesTask = _db.Lectures
            .Where(l => l.CreatedBy == userId)
            .CountAsync();

        var focusSessionsTask = _db.FocusSessions
            .Where(s => s.UserId == userId && s.EndTime != null)
            .Select(s => new { s.StartTime, s.EndTime })
            .ToListAsync();

        var notesTask = _db.Notes.CountAsync(); // Tenant filtered automatically

        var projectsTask = _db.Projects.CountAsync(); // Tenant filtered automatically

        // Wait for stats queries
        await Task.WhenAll(lecturesTask, focusSessionsTask, notesTask, projectsTask);

        var totalFocusMinutes = focusSessionsTask.Result
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
                Lectures = lecturesTask.Result,
                FocusTime = totalFocusMinutes,
                Notes = notesTask.Result,
                Projects = projectsTask.Result
            },
            Activity = allActivity
        });
    }

    public class DashboardSummaryDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<DashboardActivityDto> Activity { get; set; } = new();
    }

    public class DashboardStatsDto
    {
        public int Lectures { get; set; }
        public double FocusTime { get; set; }
        public int Notes { get; set; }
        public int Projects { get; set; }
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
