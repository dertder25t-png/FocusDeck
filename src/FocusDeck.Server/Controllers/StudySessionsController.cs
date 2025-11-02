using FocusDeck.Persistence;
using FocusDeck.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FocusDeck.Server.Controllers;

[ApiController]
[Route("api/study-sessions")]
public class StudySessionsController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<StudySessionsController> _logger;

    public StudySessionsController(AutomationDbContext db, ILogger<StudySessionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudySession>>> GetSessions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? category,
        [FromQuery] SessionStatus? status)
    {
        var query = _db.StudySessions.AsNoTracking();

        if (from.HasValue)
        {
            query = query.Where(s => s.StartTime >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.StartTime <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var pattern = $"%{category.Trim()}%";
            query = query.Where(s => s.Category != null && EF.Functions.Like(s.Category, pattern));
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var sessions = await query
            .OrderByDescending(s => s.StartTime)
            .Take(500)
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudySession>> GetSession(Guid id)
    {
        var session = await _db.StudySessions.AsNoTracking().FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session);
    }

    [HttpPost]
    public async Task<ActionResult<StudySession>> CreateSession([FromBody] StudySession? request)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        var session = NormalizeSession(request);
        if (session.SessionId == Guid.Empty)
        {
            session.SessionId = Guid.NewGuid();
        }

        var now = DateTime.UtcNow;
        session.CreatedAt = now;
        session.UpdatedAt = now;

        await _db.StudySessions.AddAsync(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created study session {SessionId}", session.SessionId);
        return CreatedAtAction(nameof(GetSession), new { id = session.SessionId }, session);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] StudySession? request)
    {
        if (request == null || id != request.SessionId)
        {
            return BadRequest("Invalid session payload");
        }

        var session = await _db.StudySessions.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
        {
            return NotFound();
        }

        session.StartTime = request.StartTime;
        session.EndTime = request.EndTime;
        session.DurationMinutes = request.DurationMinutes > 0
            ? request.DurationMinutes
            : CalculateDurationMinutes(request.StartTime, request.EndTime);
        session.SessionNotes = request.SessionNotes;
        session.Status = request.Status;
        session.FocusRate = request.FocusRate;
        session.BreaksCount = Math.Max(0, request.BreaksCount);
        session.BreakDurationMinutes = Math.Max(0, request.BreakDurationMinutes);
        session.Category = NormalizeCategory(request.Category);
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated study session {SessionId}", id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        var session = await _db.StudySessions.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
        {
            return NotFound();
        }

        _db.StudySessions.Remove(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted study session {SessionId}", id);
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<StudySummaryResponse>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _db.StudySessions.AsNoTracking();

        var rangeStart = from ?? DateTime.UtcNow.AddDays(-14);
        var rangeEnd = to ?? DateTime.UtcNow;

        query = query.Where(s => s.StartTime >= rangeStart && s.StartTime <= rangeEnd);

        var sessions = await query.ToListAsync();
        if (sessions.Count == 0)
        {
            return Ok(StudySummaryResponse.Empty(rangeStart, rangeEnd));
        }

        var summary = StudySummaryResponse.FromSessions(sessions, rangeStart, rangeEnd);
        return Ok(summary);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<StudySession>>> GetRecent()
    {
        var sessions = await _db.StudySessions.AsNoTracking()
            .OrderByDescending(s => s.StartTime)
            .Take(10)
            .ToListAsync();
        return Ok(sessions);
    }

    private static StudySession NormalizeSession(StudySession session)
    {
        session.Category = NormalizeCategory(session.Category);
        session.SessionNotes = session.SessionNotes?.Trim();
        session.DurationMinutes = session.DurationMinutes > 0
            ? session.DurationMinutes
            : CalculateDurationMinutes(session.StartTime, session.EndTime);
        session.BreaksCount = Math.Max(0, session.BreaksCount);
        session.BreakDurationMinutes = Math.Max(0, session.BreakDurationMinutes);
        return session;
    }

    private static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return category.Trim();
    }

    private static int CalculateDurationMinutes(DateTime start, DateTime? end)
    {
        if (!end.HasValue || end.Value <= start)
        {
            return 0;
        }

        return (int)Math.Ceiling((end.Value - start).TotalMinutes);
    }

    public record StudySummaryResponse(
        int TotalSessions,
        int CompletedSessions,
        int ActiveSessions,
        int TotalMinutes,
        int ProductiveMinutes,
        double AverageSessionMinutes,
        double AverageFocusRate,
        DateTime RangeStart,
        DateTime RangeEnd,
        IReadOnlyList<CategorySummary> Categories,
        IReadOnlyList<DailySummary> Daily)
    {
        public static StudySummaryResponse Empty(DateTime from, DateTime to) =>
            new(
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                from,
                to,
                Array.Empty<CategorySummary>(),
                Array.Empty<DailySummary>());

        public static StudySummaryResponse FromSessions(
            IReadOnlyCollection<StudySession> sessions,
            DateTime from,
            DateTime to)
        {
            var completed = sessions.Where(s => s.Status == SessionStatus.Completed).ToList();
            var active = sessions.Where(s => s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused).ToList();
            var totalMinutes = sessions.Sum(s => s.DurationMinutes);
            var productiveMinutes = sessions.Sum(s => Math.Max(0, s.DurationMinutes - s.BreakDurationMinutes));
            var avgDuration = sessions.Count > 0 ? sessions.Average(s => s.DurationMinutes) : 0;
            var focusValues = sessions.Where(s => s.FocusRate.HasValue).Select(s => (double)s.FocusRate!.Value).ToList();
            var avgFocus = focusValues.Count > 0 ? Math.Round(focusValues.Average(), 1) : 0;

            var categories = sessions
                .GroupBy(s => string.IsNullOrWhiteSpace(s.Category) ? "Uncategorized" : s.Category!.Trim())
                .Select(g =>
                {
                    var focusList = g.Where(s => s.FocusRate.HasValue).Select(s => (double)s.FocusRate!.Value).ToList();
                    var averageFocus = focusList.Count > 0 ? Math.Round(focusList.Average(), 1) : 0;

                    return new CategorySummary(
                        g.Key,
                        g.Count(),
                        g.Sum(s => s.DurationMinutes),
                        g.Sum(s => Math.Max(0, s.DurationMinutes - s.BreakDurationMinutes)),
                        averageFocus);
                })
                .OrderByDescending(c => c.TotalMinutes)
                .ThenBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var daily = sessions
                .GroupBy(s => s.StartTime.Date)
                .Select(g =>
                {
                    var focusList = g.Where(s => s.FocusRate.HasValue).Select(s => (double)s.FocusRate!.Value).ToList();
                    var averageFocus = focusList.Count > 0 ? Math.Round(focusList.Average(), 1) : 0;

                    return new DailySummary(
                        g.Key,
                        g.Count(),
                        g.Sum(s => s.DurationMinutes),
                        g.Sum(s => Math.Max(0, s.DurationMinutes - s.BreakDurationMinutes)),
                        averageFocus);
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new StudySummaryResponse(
                sessions.Count,
                completed.Count,
                active.Count,
                totalMinutes,
                productiveMinutes,
                Math.Round(avgDuration, 1),
                avgFocus,
                from,
                to,
                categories,
                daily);
        }
    }

    public record CategorySummary(
        string Category,
        int Sessions,
        int TotalMinutes,
        int ProductiveMinutes,
        double AverageFocusRate);

    public record DailySummary(
        DateTime Date,
        int Sessions,
        int TotalMinutes,
        int ProductiveMinutes,
        double AverageFocusRate);
}
