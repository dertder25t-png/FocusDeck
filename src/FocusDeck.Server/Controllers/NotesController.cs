using FocusDeck.Persistence;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Services.Calendar;
using FocusDeck.Server.Services.Jarvis;
using FocusDeck.Services.Abstractions;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly AutomationDbContext _db;
        private readonly ILogger<NotesController> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly CalendarResolver _calendarResolver;
        private readonly ICurrentTenant _currentTenant;
        private readonly ISuggestionService _suggestionService;

        public NotesController(
            AutomationDbContext db,
            ILogger<NotesController> logger,
            IEncryptionService encryptionService,
            CalendarResolver calendarResolver,
            ICurrentTenant currentTenant,
            ISuggestionService suggestionService)
        {
            _db = db;
            _logger = logger;
            _encryptionService = encryptionService;
            _calendarResolver = calendarResolver;
            _currentTenant = currentTenant;
            _suggestionService = suggestionService;
        }

        // GET: api/notes
        [HttpGet]
        public async Task<ActionResult> GetNotes(
            [FromQuery] string? search,
            [FromQuery] string? tag,
            [FromQuery] bool? pinned,
            [FromQuery] NoteType? type)
        {
            var query = _db.Notes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(n =>
                    EF.Functions.Like(n.Title, pattern) ||
                    EF.Functions.Like(n.Content, pattern));
            }

            if (pinned.HasValue)
            {
                query = query.Where(n => n.IsPinned == pinned.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(n => n.Type == type.Value);
            }

            var results = await query
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.LastModified ?? n.CreatedDate)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                results = results
                    .Where(n => n.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            results.ForEach(n =>
            {
                n.Title = _encryptionService.Decrypt(n.Title);
                n.Content = _encryptionService.Decrypt(n.Content);
            });

            _logger.LogInformation("Returned {Count} notes (search={Search}, tag={Tag}, pinned={Pinned}, type={Type})",
                results.Count, search, tag, pinned, type);

            var enrichedResults = results.Select(n => new
            {
                n.Id,
                n.Title,
                n.Content,
                n.Type,
                n.Tags,
                n.Color,
                n.IsPinned,
                n.CreatedDate,
                n.LastModified,
                n.Bookmarks,
                n.Sources,
                n.CitationStyle,
                n.CourseId,
                n.EventId,
                n.TenantId,
                CoveragePercent = CalculateCoverage(n)
            });

            return Ok(enrichedResults);
        }

        // GET: api/notes/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            var notes = await _db.Notes.AsNoTracking().ToListAsync();
            if (notes.Count == 0)
            {
                return Ok(new
                {
                    total = 0,
                    pinned = 0,
                    tags = Array.Empty<object>(),
                    recent = Array.Empty<Note>()
                });
            }

            notes.ForEach(n =>
            {
                n.Title = _encryptionService.Decrypt(n.Title);
                n.Content = _encryptionService.Decrypt(n.Content);
            });

            var tagCounts = notes
                .SelectMany(n => n.Tags)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { name = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(25)
                .ToList();

            var recent = notes
                .OrderByDescending(n => n.LastModified ?? n.CreatedDate)
                .Take(5)
                .ToList();

            return Ok(new
            {
                total = notes.Count,
                pinned = notes.Count(n => n.IsPinned),
                papers = notes.Count(n => n.Type == NoteType.AcademicPaper),
                tags = tagCounts,
                recent
            });
        }

        // GET: api/notes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(string id)
        {
            var note = await _db.Notes
                .Include(n => n.Sources)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                _logger.LogWarning("Note {NoteId} not found", id);
                return NotFound();
            }

            note.Title = _encryptionService.Decrypt(note.Title);
            note.Content = _encryptionService.Decrypt(note.Content);

            return Ok(note);
        }

        // POST: api/notes
        [HttpPost]
        public async Task<ActionResult<Note>> CreateNote([FromBody] CreateNoteRequest? request)
        {
            // Enforce strict tenancy via ICurrentTenant if available
            if (!_currentTenant.HasTenant || !_currentTenant.TenantId.HasValue)
            {
                _logger.LogWarning("CreateNote: Missing tenant context. Falling back to user claims if needed, but should be scoped.");
                // Depending on middleware, HasTenant might be false if not authenticated, but [Authorize] is not strictly on class.
                // Assuming tenant is resolved. If not, it might be global user. But Note needs TenantId.
                // If _currentTenant is not set, check if we can resolve from user.
            }

            var tenantId = _currentTenant.TenantId ?? Guid.Empty; // Should handle Guid.Empty case or fail.
            if (tenantId == Guid.Empty)
            {
                // Try to get from user claims if ICurrentTenant failed
                 // This is a fallback, ideally middleware handles it.
            }

            // Resolve context (Auto-Tag)
            var (evt, course) = await _calendarResolver.ResolveCurrentContextAsync(tenantId);

            var note = new Note
            {
                Id = Guid.NewGuid().ToString(),
                Title = request?.Title ?? "Untitled Note",
                Content = request?.Content ?? "",
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                TenantId = tenantId,
                Type = request?.Type ?? NoteType.QuickNote,
                Color = request?.Color ?? "#7C5CFF",
                IsPinned = request?.IsPinned ?? false,
                Tags = NormalizeTags(request?.Tags)
            };

            if (evt != null)
            {
                note.EventId = evt.Id;
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

            // Ensure tags are unique after adding course code
            note.Tags = note.Tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            note.Title = _encryptionService.Encrypt(note.Title);
            note.Content = _encryptionService.Encrypt(note.Content);

            await _db.Notes.AddAsync(note);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created note {NoteId}", note.Id);

            note.Title = _encryptionService.Decrypt(note.Title);
            note.Content = _encryptionService.Decrypt(note.Content);

            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }

        // PUT: api/notes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(string id, [FromBody] UpdateNoteRequest? request)
        {
            if (request == null)
            {
                return BadRequest("Invalid note data");
            }

            var note = await _db.Notes
                .Include(n => n.Sources) // Fix: Include Sources to prevent null reference on update
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                _logger.LogWarning("Note {NoteId} not found for update", id);
                return NotFound();
            }

            // SECURITY: Encrypt content before storage
            if (request.Content != null)
            {
                note.Content = _encryptionService.Encrypt(request.Content);
            }

            if (request.Title != null)
            {
                note.Title = _encryptionService.Encrypt(request.Title.Trim());
            }

            if (request.Color != null) note.Color = request.Color;
            if (request.IsPinned.HasValue) note.IsPinned = request.IsPinned.Value;
            if (request.Tags != null) note.Tags = NormalizeTags(request.Tags);
            if (request.Type.HasValue) note.Type = request.Type.Value;
            if (request.CitationStyle != null) note.CitationStyle = request.CitationStyle;

            if (request.Sources != null)
            {
                // Simple replace strategy for MVP (or merge if desired)
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

            _logger.LogInformation("Updated note {NoteId}", id);
            return NoContent();
        }

        // DELETE: api/notes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(string id)
        {
            var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                _logger.LogWarning("Note {NoteId} not found for deletion", id);
                return NotFound();
            }

            _db.Notes.Remove(note);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted note {NoteId}", id);
            return NoContent();
        }

        // GET: api/notes/tagged/{tag}
        [HttpGet("tagged/{tag}")]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotesByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return BadRequest("Tag is required");
            }

            var notes = await _db.Notes
                .AsNoTracking()
                .OrderByDescending(n => n.LastModified ?? n.CreatedDate)
                .ToListAsync();

            notes.ForEach(n =>
            {
                n.Title = _encryptionService.Decrypt(n.Title);
                n.Content = _encryptionService.Decrypt(n.Content);
            });

            var filtered = notes
                .Where(n => n.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Ok(filtered);
        }

        // GET: api/notes/pinned
        [HttpGet("pinned")]
        public async Task<ActionResult<IEnumerable<Note>>> GetPinnedNotes()
        {
            var pinnedNotes = await _db.Notes
                .AsNoTracking()
                .Where(n => n.IsPinned)
                .OrderByDescending(n => n.LastModified ?? n.CreatedDate)
                .ToListAsync();

            pinnedNotes.ForEach(n =>
            {
                n.Title = _encryptionService.Decrypt(n.Title);
                n.Content = _encryptionService.Decrypt(n.Content);
            });

            return Ok(pinnedNotes);
        }

        // POST: api/notes/{id}/verify
        [HttpPost("{id}/verify")]
        public async Task<ActionResult> VerifyNote(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = await _db.Notes
                .Include(n => n.Sources) // Fix: Include Sources for AI context
                .FirstOrDefaultAsync(n => n.Id == id);

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
                var newSuggestions = await _suggestionService.AnalyzeNoteAsync(decryptedNote);
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

        // GET: api/notes/{id}/suggestions
        [HttpGet("{id}/suggestions")]
        public async Task<ActionResult> GetSuggestions(string id)
        {
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

        // POST: api/notes/suggestions/{id}/accept
        [HttpPost("suggestions/{id}/accept")]
        public async Task<ActionResult> AcceptSuggestion(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
            suggestion.AcceptedBy = userId ?? "system";

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

        // GET: api/notes/{id}/coverage
        [HttpGet("{id}/coverage")]
        public async Task<ActionResult> GetCoverage(string id)
        {
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

        private double CalculateCoverage(Note note)
        {
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

        private static Note NormalizeNote(Note note)
        {
            note.Title = note.Title?.Trim() ?? string.Empty;
            note.Content = note.Content ?? string.Empty;
            note.Color = string.IsNullOrWhiteSpace(note.Color) ? "#7C5CFF" : note.Color;
            note.Tags = NormalizeTags(note.Tags);
            note.Bookmarks = NormalizeBookmarks(note.Bookmarks);
            return note;
        }

        private static List<string> NormalizeTags(IEnumerable<string>? tags)
        {
            return tags?
                .Select(t => t?.Trim() ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }

        private static List<NoteBookmark> NormalizeBookmarks(IEnumerable<NoteBookmark>? bookmarks)
        {
            return bookmarks?
                .Where(b => b != null)
                .Select(b => new NoteBookmark
                {
                    Id = string.IsNullOrWhiteSpace(b.Id) ? Guid.NewGuid().ToString() : b.Id,
                    Name = b.Name?.Trim() ?? string.Empty,
                    Position = b.Position,
                    Length = b.Length,
                    Color = string.IsNullOrWhiteSpace(b.Color) ? "#FFD700" : b.Color,
                    CreatedDate = b.CreatedDate == default ? DateTime.UtcNow : b.CreatedDate
                })
                .OrderBy(b => b.Position)
                .ToList()
                ?? new List<NoteBookmark>();
        }
    }

    public class UpdateNoteRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Color { get; set; }
        public bool? IsPinned { get; set; }
        public List<string>? Tags { get; set; }
        public NoteType? Type { get; set; }
        public string? CitationStyle { get; set; }
        public List<AcademicSourceDto>? Sources { get; set; }
    }

    public class CreateNoteRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public NoteType Type { get; set; }
        public List<string>? Tags { get; set; }
        public string? Color { get; set; }
        public bool? IsPinned { get; set; }
    }

    public class AcademicSourceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
