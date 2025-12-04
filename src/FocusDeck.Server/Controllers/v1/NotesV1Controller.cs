using FocusDeck.Persistence;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Services.Calendar;
using FocusDeck.Services.Abstractions;
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
    private readonly IEncryptionService _encryptionService;

    public NotesV1Controller(
        AutomationDbContext db,
        ILogger<NotesV1Controller> logger,
        CalendarResolver calendarResolver,
        FocusDeck.Server.Services.Jarvis.ISuggestionService jarvisService,
        FocusDeck.SharedKernel.Tenancy.ICurrentTenant currentTenant,
        IEncryptionService encryptionService)
    {
        _db = db;
        _logger = logger;
        _calendarResolver = calendarResolver;
        _jarvisService = jarvisService;
        _currentTenant = currentTenant;
        _encryptionService = encryptionService;
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

        // Store plaintext title for response before encrypting
        var plaintextTitle = note.Title;

        // SECURITY: Encrypt sensitive content before storage
        note.Title = _encryptionService.Encrypt(note.Title);
        note.Content = _encryptionService.Encrypt(note.Content);

        _db.Notes.Add(note);
        await _db.SaveChangesAsync();

        return Ok(new { noteId = note.Id, title = plaintextTitle, context = new { eventId = evt?.Id, courseId = course?.Id } });
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

        // SECURITY NOTE: Content is encrypted at rest. Database-level search on encrypted
        // content is not possible without implementing encrypted search (e.g., searchable encryption
        // or a separate search index with encrypted tokens). Current implementation only supports
        // filtering by Tags which are stored in plaintext for searchability.
        if (!string.IsNullOrEmpty(lectureId))
        {
            query = query.Where(n => n.Tags.Contains(lectureId));
        }

        var total = await query.CountAsync();

        var notes = await query
            .OrderByDescending(n => n.LastModified ?? n.CreatedDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        // SECURITY: Decrypt content before returning to client and calculating coverage
        foreach (var note in notes)
        {
            note.Title = _encryptionService.Decrypt(note.Title);
            note.Content = _encryptionService.Decrypt(note.Content);
        }

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

        // SECURITY: Decrypt note content before AI analysis
        // Create a copy to avoid modifying tracked entity
        var decryptedNote = new Note
        {
            Id = note.Id,
            Title = _encryptionService.Decrypt(note.Title),
            Content = _encryptionService.Decrypt(note.Content),
            Type = note.Type,
            Tags = note.Tags,
            Color = note.Color,
            IsPinned = note.IsPinned,
            CreatedDate = note.CreatedDate,
            LastModified = note.LastModified,
            Bookmarks = note.Bookmarks,
            Sources = note.Sources,
            CitationStyle = note.CitationStyle,
            CourseId = note.CourseId,
            EventId = note.EventId,
            TenantId = note.TenantId
        };

        // Hook up Jarvis AI for real analysis
        var existingSuggestions = await _db.NoteSuggestions
            .Where(ns => ns.NoteId == id)
            .ToListAsync();

        // Generate new suggestions if less than 5 exist to avoid spamming
        if (existingSuggestions.Count < 5)
        {
            var newSuggestions = await _jarvisService.AnalyzeNoteAsync(decryptedNote);
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

        // SECURITY: Decrypt content, modify, then re-encrypt
        var decryptedContent = _encryptionService.Decrypt(note.Content);

        // Append to note content under "AI Additions" section
        var aiAdditionsSection = "\n\n## AI Additions\n\n";
        if (!decryptedContent.Contains("## AI Additions"))
        {
            decryptedContent += aiAdditionsSection;
        }

        decryptedContent += $"\n### {suggestion.Type}\n\n{suggestion.ContentMarkdown}\n\n*Source: {suggestion.Source}*\n";
        
        // Re-encrypt before saving
        note.Content = _encryptionService.Encrypt(decryptedContent);
        note.LastModified = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Accepted suggestion {SuggestionId} for note {NoteId} by user {UserId}",
            id, suggestion.NoteId, userId);

        return Ok(new
        {
            message = "Suggestion accepted and added to note",
            noteId = note.Id,
            updatedContent = decryptedContent
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

        // SECURITY: Decrypt content for coverage calculation
        note.Title = _encryptionService.Decrypt(note.Title);
        note.Content = _encryptionService.Decrypt(note.Content);

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

        // SECURITY: Encrypt content before storage
        if (request.Content != null) 
        {
            note.Content = _encryptionService.Encrypt(request.Content);
        }
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
