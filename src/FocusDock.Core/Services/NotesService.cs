using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDock.Data.Models;

namespace FocusDock.Core.Services;

public class NotesService
{
    private List<Note> _notes = new();
    private readonly string _notesFile;
    
    public event EventHandler? NotesChanged;
    
    public NotesService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "FocusDeck");
        Directory.CreateDirectory(appFolder);
        _notesFile = Path.Combine(appFolder, "notes.json");
        LoadNotes();
    }
    
    public IReadOnlyList<Note> GetAllNotes() => _notes.AsReadOnly();
    
    public void AddNote(Note note)
    {
        _notes.Add(note);
        SaveNotes();
        NotesChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Async version of AddNote for non-blocking file I/O
    /// </summary>
    public async Task AddNoteAsync(Note note)
    {
        _notes.Add(note);
        await SaveNotesAsync();
        NotesChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void UpdateNote(Note note)
    {
        note.LastModified = DateTime.Now;
        SaveNotes();
        NotesChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Async version of UpdateNote for non-blocking file I/O
    /// </summary>
    public async Task UpdateNoteAsync(Note note)
    {
        note.LastModified = DateTime.Now;
        await SaveNotesAsync();
        NotesChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void DeleteNote(string noteId)
    {
        _notes.RemoveAll(n => n.Id == noteId);
        SaveNotes();
        NotesChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Async version of DeleteNote for non-blocking file I/O
    /// </summary>
    public async Task DeleteNoteAsync(string noteId)
    {
        _notes.RemoveAll(n => n.Id == noteId);
        await SaveNotesAsync();
        NotesChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void TogglePin(string noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note != null)
        {
            note.IsPinned = !note.IsPinned;
            note.LastModified = DateTime.Now;
            SaveNotes();
            NotesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// Async version of TogglePin for non-blocking file I/O
    /// </summary>
    public async Task TogglePinAsync(string noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note != null)
        {
            note.IsPinned = !note.IsPinned;
            note.LastModified = DateTime.Now;
            await SaveNotesAsync();
            NotesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    private void LoadNotes()
    {
        try
        {
            if (File.Exists(_notesFile))
            {
                var json = File.ReadAllText(_notesFile);
                _notes = JsonSerializer.Deserialize<List<Note>>(json) ?? new List<Note>();
            }
        }
        catch
        {
            _notes = new List<Note>();
        }
    }
    
    /// <summary>
    /// Async version of LoadNotes for non-blocking file I/O
    /// </summary>
    private async Task LoadNotesAsync()
    {
        try
        {
            if (File.Exists(_notesFile))
            {
                var json = await File.ReadAllTextAsync(_notesFile);
                _notes = JsonSerializer.Deserialize<List<Note>>(json) ?? new List<Note>();
            }
        }
        catch
        {
            _notes = new List<Note>();
        }
    }
    
    private void SaveNotes()
    {
        try
        {
            var json = JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_notesFile, json);
        }
        catch
        {
            // Handle error silently
        }
    }
    
    /// <summary>
    /// Async version of SaveNotes for non-blocking file I/O
    /// </summary>
    private async Task SaveNotesAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_notesFile, json);
        }
        catch
        {
            // Handle error silently
        }
    }
}

