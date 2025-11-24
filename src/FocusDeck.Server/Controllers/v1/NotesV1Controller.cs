using FocusDeck.Persistence;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Services.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[Route("v1/notes")]
[Authorize]
public class NotesV1Controller : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<NotesV1Controller> _logger;
    private readonly CalendarResolver _calendarResolver;

    public NotesV1Controller(AutomationDbContext db, ILogger<NotesV1Controller> logger, CalendarResolver calendarResolver)
    {
        _db = db;
        _logger = logger;
        _calendarResolver = calendarResolver;
    }

    // POST: v1/notes/start
    [HttpPost("start")]
    public async Task<ActionResult> StartNote([FromBody] CreateNoteDto? request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdStr = User.FindFirst("app_tenant_id")?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Unauthorized();
        }

        // Resolve context (Auto-Tag)
        var (evt, course) = await _calendarResolver.ResolveCurrentContextAsync(tenantId);

        var note = new Note
        {
            Id = Guid.NewGuid().ToString(),
            Title = request?.Title ?? "Untitled Note",
            Content = request?.Content ?? "",
            CreatedDate = DateTime.UtcNow,
            TenantId = tenantId
        };

        if (evt != null)
        {
            note.EventId = evt.Id; // Note: EventCache ID, not external ID
            // Auto-title if no specific title provided
            if (string.IsNullOrEmpty(request?.Title))
            {
                var topic = !string.IsNullOrEmpty(evt.Title) ? evt.Title : "Session";
                var courseCode = course?.Code ?? "General";
                note.Title = $"{courseCode} - {topic} - {DateTime.Now:MM/dd HH:mm}";
            }
        }
        else if (string.IsNullOrEmpty(request?.Title))
        {
             note.Title = $"Note - {DateTime.Now:MM/dd HH:mm}";
        }

        if (course != null)
        {
            note.CourseId = course.Id;
            note.Tags.Add(course.Code);
        }

        _db.Notes.Add(note);
        await _db.SaveChangesAsync();

        return Ok(new { noteId = note.Id, title = note.Title, context = new { eventId = evt?.Id, courseId = course?.Id } });
    }

    // GET: v1/notes/list
    [HttpGet("list")]
    public async Task<ActionResult> ListNotes(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20,
        [FromQuery] string? lectureId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var query = _db.Notes.AsNoTracking();

        // Filter by lecture if specified (using the note's tags or title)
        if (!string.IsNullOrEmpty(lectureId))
        {
            query = query.Where(n => n.Tags.Contains(lectureId) || n.Content.Contains(lectureId));
        }

        var total = await query.CountAsync();

        var notes = await query
            .OrderByDescending(n => n.LastModified ?? n.CreatedDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        // Calculate coverage for each note (stub - would compare against lecture transcript)
        var notesWithCoverage = notes.Select(n => new
        {
            n.Id,
            n.Title,
            n.Content,
            n.Tags,
            n.CreatedDate,
            n.LastModified,
            n.Type,
            n.Sources,
            n.CitationStyle,
            CoveragePercent = CalculateCoverage(n) // Stub calculation
        });

        return Ok(new
        {
            notes = notesWithCoverage,
            total,
            page,
            perPage,
            totalPages = (int)Math.Ceiling((double)total / perPage)
        });
    }

    // POST: v1/notes/{id}/verify
    [HttpPost("{id}/verify")]
    public async Task<ActionResult> VerifyNote(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id);
        if (note == null)
        {
            return NotFound(new { code = "NOTE_NOT_FOUND", message = "Note not found" });
        }

        // TODO: Enqueue VerifyNoteCompleteness job
        // For now, generate mock suggestions
        var existingSuggestions = await _db.NoteSuggestions
            .Where(ns => ns.NoteId == id)
            .ToListAsync();

        // Generate 2-3 new suggestions if less than 5 exist
        if (existingSuggestions.Count < 5)
        {
            var newSuggestions = GenerateMockSuggestions(note, existingSuggestions.Count);
            await _db.NoteSuggestions.AddRangeAsync(newSuggestions);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Generated {Count} suggestions for note {NoteId}", newSuggestions.Count, id);
        }

        return Ok(new { message = "Note verification started" });
    }

    // GET: v1/notes/{id}/suggestions
    [HttpGet("{id}/suggestions")]
    public async Task<ActionResult> GetSuggestions(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var note = await _db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (note == null)
        {
            return NotFound(new { code = "NOTE_NOT_FOUND", message = "Note not found" });
        }

        var suggestions = await _db.NoteSuggestions
            .AsNoTracking()
            .Where(ns => ns.NoteId == id && ns.AcceptedAt == null)
            .OrderByDescending(ns => ns.Confidence)
            .ThenByDescending(ns => ns.CreatedAt)
            .ToListAsync();

        return Ok(new
        {
            noteId = id,
            suggestions = suggestions.Select(s => new
            {
                s.Id,
                s.Type,
                TypeName = s.Type.ToString(),
                s.ContentMarkdown,
                s.Source,
                s.Confidence,
                s.CreatedAt
            })
        });
    }

    // POST: v1/notes/suggestions/{id}/accept
    [HttpPost("suggestions/{id}/accept")]
    public async Task<ActionResult> AcceptSuggestion(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var suggestion = await _db.NoteSuggestions.FirstOrDefaultAsync(s => s.Id == id);
        if (suggestion == null)
        {
            return NotFound(new { code = "SUGGESTION_NOT_FOUND", message = "Suggestion not found" });
        }

        if (suggestion.AcceptedAt != null)
        {
            return BadRequest(new { code = "ALREADY_ACCEPTED", message = "Suggestion already accepted" });
        }

        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == suggestion.NoteId);
        if (note == null)
        {
            return NotFound(new { code = "NOTE_NOT_FOUND", message = "Note not found" });
        }

        // Mark suggestion as accepted
        suggestion.AcceptedAt = DateTime.UtcNow;
        suggestion.AcceptedBy = userId;

        // Append to note content under "AI Additions" section
        var aiAdditionsSection = "\n\n## AI Additions\n\n";
        if (!note.Content.Contains("## AI Additions"))
        {
            note.Content += aiAdditionsSection;
        }

        note.Content += $"\n### {suggestion.Type}\n\n{suggestion.ContentMarkdown}\n\n*Source: {suggestion.Source}*\n";
        note.LastModified = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Accepted suggestion {SuggestionId} for note {NoteId} by user {UserId}",
            id, suggestion.NoteId, userId);

        return Ok(new
        {
            message = "Suggestion accepted and added to note",
            noteId = note.Id,
            updatedContent = note.Content
        });
    }

    // GET: v1/notes/{id}/coverage
    [HttpGet("{id}/coverage")]
    public async Task<ActionResult> GetCoverage(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var note = await _db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (note == null)
        {
            return NotFound(new { code = "NOTE_NOT_FOUND", message = "Note not found" });
        }

        var coverage = CalculateCoverage(note);

        return Ok(new
        {
            noteId = id,
            score = coverage,
            timestamp = DateTime.UtcNow
        });
    }

    // PUT: v1/notes/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateNote(string id, [FromBody] UpdateNoteDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id);
        if (note == null)
        {
            return NotFound(new { code = "NOTE_NOT_FOUND", message = "Note not found" });
        }

        if (request.Content != null) note.Content = request.Content;
        if (request.Type.HasValue) note.Type = request.Type.Value;
        if (request.CitationStyle != null) note.CitationStyle = request.CitationStyle;

        if (request.Sources != null)
        {
            // Simple replace strategy for MVP (or merge if desired)
            // Assuming request sends full list
            _db.AcademicSources.RemoveRange(note.Sources);
            note.Sources = request.Sources.Select(s => new AcademicSource
            {
                Id = Guid.TryParse(s.Id, out var sid) ? sid : Guid.NewGuid(),
                Title = s.Title,
                Author = s.Author,
                Publisher = s.Publisher,
                Year = s.Year,
                Url = s.Url,
                NoteId = note.Id,
                TenantId = note.TenantId
            }).ToList();
        }

        note.LastModified = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Note updated" });
    }

    private double CalculateCoverage(Note note)
    {
        // Stub calculation based on content length and structure
        // In production, this would compare against lecture transcripts
        var hasContent = !string.IsNullOrWhiteSpace(note.Content);
        var hasTags = note.Tags.Count > 0;
        var hasTitle = !string.IsNullOrWhiteSpace(note.Title);
        var contentLength = note.Content?.Length ?? 0;

        var baseScore = 0.0;
        if (hasTitle) baseScore += 20;
        if (hasTags) baseScore += 10;
        if (hasContent)
        {
            if (contentLength > 100) baseScore += 20;
            if (contentLength > 500) baseScore += 20;
            if (contentLength > 1000) baseScore += 20;
            if (note.Content?.Contains("##") == true) baseScore += 10; // Has sections
        }

        return Math.Min(100, baseScore);
    }

    private List<NoteSuggestion> GenerateMockSuggestions(Note note, int existingCount)
    {
        var suggestions = new List<NoteSuggestion>();
        var types = new[] {
            (NoteSuggestionType.MissingPoint, "Consider adding: The fundamental theorem states that..."),
            (NoteSuggestionType.Definition, "**Definition**: Algorithm complexity refers to..."),
            (NoteSuggestionType.Reference, "See also: Chapter 5, Section 2.3 for related concepts")
        };

        var count = Math.Min(3, 5 - existingCount);
        for (int i = 0; i < count; i++)
        {
            var (type, content) = types[i % types.Length];
            suggestions.Add(new NoteSuggestion
            {
                Id = Guid.NewGuid().ToString(),
                NoteId = note.Id,
                Type = type,
                ContentMarkdown = $"{content} (suggestion #{existingCount + i + 1})",
                Source = $"Lecture transcript timestamp {12 + i * 5}:{30 + i * 10}",
                Confidence = 0.85 - (i * 0.1),
                CreatedAt = DateTime.UtcNow
            });
        }

        return suggestions;
    }
}
