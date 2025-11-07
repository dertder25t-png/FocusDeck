using FocusDeck.Mobile.Models;
using FocusDeck.Mobile.Services.Auth;

namespace FocusDeck.Mobile.Data.Repositories;

public class NoteRepository
{
    private readonly StudySessionDbContext _context;
    private readonly MobileVaultService _vaultService;

    public NoteRepository(StudySessionDbContext context, MobileVaultService vaultService)
    {
        _context = context;
        _vaultService = vaultService;
    }

    public async Task<List<Note>> GetNotesAsync()
    {
        var notes = await _context.Notes.ToListAsync();
        foreach (var note in notes)
        {
            note.Title = await _vaultService.DecryptAsync(note.Title);
            note.Content = await _vaultService.DecryptAsync(note.Content);
        }
        return notes;
    }

    public async Task<Note> GetNoteAsync(string id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note != null)
        {
            note.Title = await _vaultService.DecryptAsync(note.Title);
            note.Content = await _vaultService.DecryptAsync(note.Content);
        }
        return note;
    }

    public async Task<int> SaveNoteAsync(Note note)
    {
        note.Title = await _vaultService.EncryptAsync(note.Title);
        note.Content = await _vaultService.EncryptAsync(note.Content);

        if (_context.Notes.Any(n => n.Id == note.Id))
        {
            _context.Update(note);
        }
        else
        {
            _context.Notes.Add(note);
        }
        return await _context.SaveChangesAsync();
    }

    public async Task<int> DeleteNoteAsync(Note note)
    {
        _context.Notes.Remove(note);
        return await _context.SaveChangesAsync();
    }
}
