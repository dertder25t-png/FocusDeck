using Microsoft.AspNetCore.Mvc;
using FocusDeck.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private static readonly List<Note> _notes = new List<Note>();
        private readonly ILogger<NotesController> _logger;

        public NotesController(ILogger<NotesController> logger)
        {
            _logger = logger;
        }

        // GET: api/notes
        [HttpGet]
        public ActionResult<IEnumerable<Note>> GetNotes()
        {
            _logger.LogInformation("Getting all notes");
            return Ok(_notes);
        }

        // GET: api/notes/{id}
        [HttpGet("{id}")]
        public ActionResult<Note> GetNote(string id)
        {
            var note = _notes.FirstOrDefault(n => n.Id == id);
            if (note == null)
            {
                _logger.LogWarning("Note {NoteId} not found", id);
                return NotFound();
            }
            return Ok(note);
        }

        // POST: api/notes
        [HttpPost]
        public ActionResult<Note> CreateNote([FromBody] Note newNote)
        {
            if (newNote == null)
            {
                return BadRequest("Note object is null");
            }

            // Ensure ID is set
            if (string.IsNullOrEmpty(newNote.Id))
            {
                newNote.Id = Guid.NewGuid().ToString();
            }

            // Set timestamps
            newNote.CreatedDate = DateTime.UtcNow;
            newNote.LastModified = DateTime.UtcNow;

            _notes.Add(newNote);
            _logger.LogInformation("Created note {NoteId}", newNote.Id);

            return CreatedAtAction(nameof(GetNote), new { id = newNote.Id }, newNote);
        }

        // PUT: api/notes/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateNote(string id, [FromBody] Note updatedNote)
        {
            if (updatedNote == null || id != updatedNote.Id)
            {
                return BadRequest("Invalid note data");
            }

            var note = _notes.FirstOrDefault(n => n.Id == id);
            if (note == null)
            {
                _logger.LogWarning("Note {NoteId} not found for update", id);
                return NotFound();
            }

            // Update properties
            note.Title = updatedNote.Title;
            note.Content = updatedNote.Content;
            note.Tags = updatedNote.Tags;
            note.Color = updatedNote.Color;
            note.IsPinned = updatedNote.IsPinned;
            note.Bookmarks = updatedNote.Bookmarks;
            note.LastModified = DateTime.UtcNow;

            _logger.LogInformation("Updated note {NoteId}", id);
            return NoContent();
        }

        // DELETE: api/notes/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteNote(string id)
        {
            var note = _notes.FirstOrDefault(n => n.Id == id);
            if (note == null)
            {
                _logger.LogWarning("Note {NoteId} not found for deletion", id);
                return NotFound();
            }

            _notes.Remove(note);
            _logger.LogInformation("Deleted note {NoteId}", id);
            return NoContent();
        }

        // GET: api/notes/tagged/{tag}
        [HttpGet("tagged/{tag}")]
        public ActionResult<IEnumerable<Note>> GetNotesByTag(string tag)
        {
            var notes = _notes.Where(n => n.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
            return Ok(notes);
        }

        // GET: api/notes/pinned
        [HttpGet("pinned")]
        public ActionResult<IEnumerable<Note>> GetPinnedNotes()
        {
            var pinnedNotes = _notes.Where(n => n.IsPinned).ToList();
            return Ok(pinnedNotes);
        }
    }
}
