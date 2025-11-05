using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FocusDeck.Persistence;
using System.Security.Claims;
using System.Text;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[Route("v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AutomationDbContext _db;

    public AnalyticsController(AutomationDbContext db)
    {
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] int days = 30)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var startDate = DateTime.UtcNow.AddDays(-days);

        // Focus sessions data
        var sessions = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.StartTime >= startDate)
            .ToListAsync();

        var totalFocusMinutes = sessions
            .Where(s => s.EndTime.HasValue)
            .Sum(s => (s.EndTime!.Value - s.StartTime).TotalMinutes);

        var totalDistractions = sessions.Sum(s => s.DistractionsCount);

        // Calculate streak
        var allSessions = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.EndTime.HasValue)
            .OrderByDescending(s => s.StartTime)
            .Select(s => s.StartTime.Date)
            .Distinct()
            .ToListAsync();

        var currentStreak = CalculateStreak(allSessions);

        // Lectures data
        var lectures = await _db.Lectures
            .Where(l => l.CreatedBy == userId && l.CreatedAt >= startDate)
            .ToListAsync();

        var lecturesProcessed = lectures.Count(l => l.Status == Domain.Entities.LectureStatus.Completed);

        // Notes data - simplified
        var notes = await _db.Notes
            .Where(n => n.CreatedDate >= startDate)
            .ToListAsync();

        var avgCoverage = notes.Any() 
            ? notes.Average(n => CalculateCoverageScore(n))
            : 0;

        var suggestionsAccepted = await _db.NoteSuggestions
            .Where(s => s.AcceptedAt.HasValue && s.AcceptedAt >= startDate)
            .CountAsync();

        return Ok(new
        {
            focusMinutes = (int)totalFocusMinutes,
            distractionsPerHour = totalFocusMinutes > 0 ? totalDistractions / (totalFocusMinutes / 60) : 0,
            currentStreak,
            lecturesProcessed,
            avgCoverage = (int)avgCoverage,
            suggestionsAccepted
        });
    }

    [HttpGet("focus-minutes")]
    public async Task<IActionResult> GetFocusMinutes([FromQuery] int days = 30, [FromQuery] string? courseId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var startDate = DateTime.UtcNow.AddDays(-days);

        var sessions = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.StartTime >= startDate && s.EndTime.HasValue)
            .ToListAsync();

        var groupedData = sessions
            .GroupBy(s => s.StartTime.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                minutes = (int)g.Sum(s => (s.EndTime!.Value - s.StartTime).TotalMinutes)
            })
            .ToList();

        return Ok(new { series = groupedData });
    }

    [HttpGet("lectures-timeline")]
    public async Task<IActionResult> GetLecturesTimeline([FromQuery] int days = 30, [FromQuery] string? courseId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var startDate = DateTime.UtcNow.AddDays(-days);

        var query = _db.Lectures
            .Where(l => l.CreatedBy == userId && l.CreatedAt >= startDate);

        if (!string.IsNullOrEmpty(courseId))
            query = query.Where(l => l.CourseId == courseId);

        var lectures = await query.ToListAsync();

        var groupedData = lectures
            .GroupBy(l => l.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                count = g.Count(),
                processed = g.Count(l => l.Status == Domain.Entities.LectureStatus.Completed)
            })
            .ToList();

        return Ok(new { series = groupedData });
    }

    [HttpGet("suggestions-accepted")]
    public async Task<IActionResult> GetSuggestionsAccepted([FromQuery] int days = 30, [FromQuery] string? courseId = null)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var suggestions = await _db.NoteSuggestions
            .Where(s => s.AcceptedAt.HasValue && s.AcceptedAt >= startDate)
            .ToListAsync();

        var groupedData = suggestions
            .GroupBy(s => s.AcceptedAt!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                count = g.Count(),
                byType = g.GroupBy(s => s.Type.ToString()).Select(tg => new
                {
                    type = tg.Key,
                    count = tg.Count()
                }).ToList()
            })
            .ToList();

        return Ok(new { series = groupedData });
    }

    [HttpGet("export/json")]
    public async Task<IActionResult> ExportJson([FromQuery] int days = 30, [FromQuery] string? courseId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var overview = await GetOverviewData(userId, days);
        var focusMinutes = await GetFocusMinutesData(userId, days, courseId);
        var lectures = await GetLecturesTimelineData(userId, days, courseId);
        var suggestions = await GetSuggestionsAcceptedData(days, courseId);

        var exportData = new
        {
            generatedAt = DateTime.UtcNow,
            range = $"{days} days",
            courseId,
            overview,
            focusMinutes,
            lectures,
            suggestions
        };

        return Ok(exportData);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] int days = 30, [FromQuery] string? courseId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var focusMinutes = await GetFocusMinutesData(userId, days, courseId);
        var lectures = await GetLecturesTimelineData(userId, days, courseId);

        var csv = new StringBuilder();
        csv.AppendLine("Date,Focus Minutes,Lectures Count,Lectures Processed");

        var allDates = focusMinutes.Select(f => f.date)
            .Union(lectures.Select(l => l.date))
            .Distinct()
            .OrderBy(d => d);

        foreach (var date in allDates)
        {
            var focus = focusMinutes.FirstOrDefault(f => f.date == date);
            var lecture = lectures.FirstOrDefault(l => l.date == date);

            csv.AppendLine($"{date},{focus?.minutes ?? 0},{lecture?.count ?? 0},{lecture?.processed ?? 0}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"analytics_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private int CalculateStreak(List<DateTime> sessionDates)
    {
        if (!sessionDates.Any())
            return 0;

        var streak = 0;
        var today = DateTime.UtcNow.Date;

        for (int i = 0; i < 365; i++)
        {
            var checkDate = today.AddDays(-i);
            if (sessionDates.Contains(checkDate))
                streak++;
            else if (i > 0) // Allow today to be missing
                break;
        }

        return streak;
    }

    private double CalculateCoverageScore(Domain.Entities.Note note)
    {
        // Stub calculation - in production would compare with lecture transcript
        if (string.IsNullOrEmpty(note.Content))
            return 0;

        var lines = note.Content.Split('\n').Length;
        var words = note.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (words < 50) return 30;
        if (words < 200) return 60;
        if (lines > 20 && words > 500) return 90;

        return 75;
    }

    private async Task<object> GetOverviewData(string userId, int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var sessions = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.StartTime >= startDate)
            .ToListAsync();

        var totalFocusMinutes = sessions.Where(s => s.EndTime.HasValue)
            .Sum(s => (s.EndTime!.Value - s.StartTime).TotalMinutes);

        var lectures = await _db.Lectures
            .Where(l => l.CreatedBy == userId && l.CreatedAt >= startDate)
            .CountAsync();

        return new
        {
            focusMinutes = (int)totalFocusMinutes,
            lecturesProcessed = lectures
        };
    }

    private async Task<List<dynamic>> GetFocusMinutesData(string userId, int days, string? courseId)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var sessions = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.StartTime >= startDate && s.EndTime.HasValue)
            .ToListAsync();

        return sessions
            .GroupBy(s => s.StartTime.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                minutes = (int)g.Sum(s => (s.EndTime!.Value - s.StartTime).TotalMinutes)
            } as dynamic)
            .ToList();
    }

    private async Task<List<dynamic>> GetLecturesTimelineData(string userId, int days, string? courseId)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var query = _db.Lectures
            .Where(l => l.CreatedBy == userId && l.CreatedAt >= startDate);

        if (!string.IsNullOrEmpty(courseId))
            query = query.Where(l => l.CourseId == courseId);

        var lectures = await query.ToListAsync();

        return lectures
            .GroupBy(l => l.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                count = g.Count(),
                processed = g.Count(l => l.Status == Domain.Entities.LectureStatus.Completed)
            } as dynamic)
            .ToList();
    }

    private async Task<List<dynamic>> GetSuggestionsAcceptedData(int days, string? courseId)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var suggestions = await _db.NoteSuggestions
            .Where(s => s.AcceptedAt.HasValue && s.AcceptedAt >= startDate)
            .ToListAsync();

        return suggestions
            .GroupBy(s => s.AcceptedAt!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                count = g.Count()
            } as dynamic)
            .ToList();
    }
}
