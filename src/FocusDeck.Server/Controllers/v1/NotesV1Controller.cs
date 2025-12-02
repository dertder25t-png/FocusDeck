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
    private readonly FocusDeck.Server.Services.Jarvis.ISuggestionService _jarvisService;
    private readonly FocusDeck.SharedKernel.Tenancy.ICurrentTenant _currentTenant;

    public NotesV1Controller(
        AutomationDbContext db,
        ILogger<NotesV1Controller> logger,
        CalendarResolver calendarResolver,
        FocusDeck.Server.Services.Jarvis.ISuggestionService jarvisService,
        FocusDeck.SharedKernel.Tenancy.ICurrentTenant currentTenant)
    {
        _db = db;
        _logger = logger;
        _calendarResolver = calendarResolver;
        _jarvisService = jarvisService;
        _currentTenant = currentTenant;
    }

    // POST: v1/notes/start
    [HttpPost("start")]
    public async Task<ActionResult> StartNote([FromBody] CreateNoteDto? request)
    {
        // Enforce strict tenancy via ICurrentTenant
        if (!_currentTenant.HasTenant || !_currentTenant.TenantId.HasValue)
        {
            _logger.LogWarning("StartNote blocked: Missing tenant context");
            return Unauthorized();
        }

        var tenantId = _currentTenant.TenantId.Value;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
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

        // Hook up Jarvis AI for real analysis
        var existingSuggestions = await _db.NoteSuggestions
            .Where(ns => ns.NoteId == id)
            .ToListAsync();

        // Generate new suggestions if less than 5 exist to avoid spamming
        if (existingSuggestions.Count < 5)
        {
            var newSuggestions = await _jarvisService.AnalyzeNoteAsync(note);
            if (newSuggestions.Any())
            {
                await _db.NoteSuggestions.AddRangeAsync(newSuggestions);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Generated {Count} suggestions for note {NoteId} via Jarvis", newSuggestions.Count, id);
            }
            else
            {
                _logger.LogInformation("Jarvis generated no suggestions for note {NoteId}", id);
            }
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
        // Calculate coverage based on content density relative to an expected baseline (e.g. 500 words for a lecture)
        // TODO: Future enhancement - retrieve Linked Transcript via note.EventId and perform semantic similarity check.

        if (string.IsNullOrWhiteSpace(note.Content)) return 0.0;

        // Base metrics
        var wordCount = note.Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

        // Assume a standard lecture note should have at least 300 words for "good" coverage without transcript comparison
        double densityScore = Math.Min(100.0, (wordCount / 300.0) * 100.0);

        // Quality multipliers
        bool hasStructure = note.Content.Contains("##") || note.Content.Contains("- ") || note.Content.Contains("* ");
        bool hasTags = note.Tags != null && note.Tags.Any();
        bool hasCitations = note.Sources != null && note.Sources.Any();

        double multiplier = 1.0;
        if (hasStructure) multiplier += 0.1;
        if (hasTags) multiplier += 0.05;
        if (hasCitations) multiplier += 0.1;

        return Math.Min(100.0, densityScore * multiplier);
    }

}
