using FocusDeck.Server.Data;
using FocusDeck.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly AutomationDbContext _db;
        private readonly ILogger<NotesController> _logger;

        public NotesController(AutomationDbContext db, ILogger<NotesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: api/notes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes(
            [FromQuery] string? search,
            [FromQuery] string? tag,
            [FromQuery] bool? pinned)
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

            _logger.LogInformation("Returned {Count} notes (search={Search}, tag={Tag}, pinned={Pinned})",
                results.Count, search, tag, pinned);

            return Ok(results);
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
                tags = tagCounts,
                recent
            });
        }

        // GET: api/notes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(string id)
        {
            var note = await _db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                _logger.LogWarning("Note {NoteId} not found", id);
                return NotFound();
            }
            return Ok(note);
        }

        // POST: api/notes
        [HttpPost]
        public async Task<ActionResult<Note>> CreateNote([FromBody] Note? newNote)
        {
            if (newNote == null)
            {
                return BadRequest("Note object is null");
            }

            var note = NormalizeNote(newNote);
            if (string.IsNullOrWhiteSpace(note.Id))
            {
                note.Id = Guid.NewGuid().ToString();
            }

            var utcNow = DateTime.UtcNow;
            note.CreatedDate = utcNow;
            note.LastModified = utcNow;

            await _db.Notes.AddAsync(note);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created note {NoteId}", note.Id);

            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }

        // PUT: api/notes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(string id, [FromBody] Note? updatedNote)
        {
            if (updatedNote == null || id != updatedNote.Id)
            {
                return BadRequest("Invalid note data");
            }

            var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                _logger.LogWarning("Note {NoteId} not found for update", id);
                return NotFound();
            }

            note.Title = updatedNote.Title?.Trim() ?? string.Empty;
            note.Content = updatedNote.Content ?? string.Empty;
            note.Color = string.IsNullOrWhiteSpace(updatedNote.Color) ? "#7C5CFF" : updatedNote.Color;
            note.IsPinned = updatedNote.IsPinned;
            note.Tags = NormalizeTags(updatedNote.Tags);
            note.Bookmarks = NormalizeBookmarks(updatedNote.Bookmarks);
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
            return Ok(pinnedNotes);
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
}
