import { useState, useEffect } from 'react';
import { Button } from '../components/Button';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../components/Card';
import { Badge } from '../components/Badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '../components/Dialog';
import { EmptyState } from '../components/States';
import { NoteEditor, type AcademicSource } from '../components/NoteEditor';

interface Note {
  id: string;
  title: string;
  content: string;
  tags: string[];
  createdDate: string;
  lastModified?: string;
  coveragePercent?: number;
  type: 'quick' | 'paper';
  sources: AcademicSource[];
  citationStyle: string;
}

interface Suggestion {
  id: string;
  type: number;
  typeName: string;
  contentMarkdown: string;
  source: string;
  confidence: number;
  createdAt: string;
}

export function NotesPage() {
  const [notes, setNotes] = useState<Note[]>([]);
  const [selectedNote, setSelectedNote] = useState<Note | null>(null);
  const [suggestions, setSuggestions] = useState<Suggestion[]>([]);
  const [selectedSuggestion, setSelectedSuggestion] = useState<Suggestion | null>(null);
  const [isVerifying, setIsVerifying] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [editedContent, setEditedContent] = useState('');

  // Editor State
  const [editorMode, setEditorMode] = useState<'quick' | 'paper'>('quick');
  const [sources, setSources] = useState<AcademicSource[]>([]);
  const [citationStyle, setCitationStyle] = useState('APA');

  useEffect(() => {
    loadNotes();
  }, []);

  const loadNotes = async () => {
    try {
      const response = await fetch('/v1/notes/list');
      if (response.ok) {
        const data = await response.json();
        setNotes(data.notes || []);
      }
    } catch (error) {
      console.error('Failed to load notes:', error);
    }
  };

  const handleNoteClick = async (note: Note) => {
    setSelectedNote(note);
    setEditedContent(note.content);
    setEditMode(false);
    
    // Initialize editor state
    setEditorMode(note.type || 'quick');
    setSources(note.sources || []);
    setCitationStyle(note.citationStyle || 'APA');

    // Load suggestions for this note
    try {
      const response = await fetch(`/v1/notes/${note.id}/suggestions`);
      if (response.ok) {
        const data = await response.json();
        setSuggestions(data.suggestions || []);
      }
    } catch (error) {
      console.error('Failed to load suggestions:', error);
      setSuggestions([]);
    }
  };

  const handleSaveNote = async () => {
    if (!selectedNote) return;

    try {
        const payload = {
            content: editedContent,
            type: editorMode,
            sources: sources,
            citationStyle: citationStyle
        };

        const response = await fetch(`/v1/notes/${selectedNote.id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            // Update local state
            setSelectedNote({
                ...selectedNote,
                content: editedContent,
                type: editorMode,
                sources: sources,
                citationStyle: citationStyle,
                lastModified: new Date().toISOString()
            });
            // Refresh list to show updated timestamp
            loadNotes();
        }
    } catch (error) {
        console.error('Failed to save note:', error);
    }
  };

  const handleVerifyNote = async () => {
    if (!selectedNote) return;
    
    setIsVerifying(true);
    try {
      const response = await fetch(`/v1/notes/${selectedNote.id}/verify`, {
        method: 'POST'
      });
      if (response.ok) {
        // Reload suggestions after verification
        const suggestionsResponse = await fetch(`/v1/notes/${selectedNote.id}/suggestions`);
        if (suggestionsResponse.ok) {
          const data = await suggestionsResponse.json();
          setSuggestions(data.suggestions || []);
        }
      }
    } catch (error) {
      console.error('Failed to verify note:', error);
    } finally {
      setIsVerifying(false);
    }
  };

  const handleAcceptSuggestion = async (suggestionId: string) => {
    try {
      const response = await fetch(`/v1/notes/suggestions/${suggestionId}/accept`, {
        method: 'POST'
      });
      if (response.ok) {
        const data = await response.json();
        // Update the note content
        if (selectedNote) {
          setSelectedNote({ ...selectedNote, content: data.updatedContent });
          setEditedContent(data.updatedContent);
        }
        // Remove the accepted suggestion from the list
        setSuggestions(suggestions.filter(s => s.id !== suggestionId));
        setSelectedSuggestion(null);
      }
    } catch (error) {
      console.error('Failed to accept suggestion:', error);
    }
  };

  const getCoverageColor = (coverage?: number) => {
    if (!coverage) return 'default';
    if (coverage >= 80) return 'success';
    if (coverage >= 50) return 'warning';
    return 'danger';
  };

  const getSuggestionTypeColor = (typeName: string) => {
    switch (typeName) {
      case 'MissingPoint': return 'warning';
      case 'Definition': return 'info';
      case 'Reference': return 'default';
      case 'Clarification': return 'success';
      default: return 'default';
    }
  };

  if (notes.length === 0) {
    return (
      <div>
        <div className="mb-6">
          <h1 className="text-2xl font-semibold">Notes</h1>
          <p className="text-sm text-gray-400 mt-1">AI-verified notes from your lectures</p>
        </div>
        <EmptyState
          title="No notes yet"
          description="Create your first note from a lecture to get started with AI verification"
        />
      </div>
    );
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-semibold">Notes</h1>
        <p className="text-sm text-gray-400 mt-1">AI-verified notes from your lectures</p>
      </div>

      <div className="grid grid-cols-1 gap-4">
        {notes.map((note) => (
          <Card
            key={note.id}
            className="hover:border-primary/50 transition-colors cursor-pointer"
            onClick={() => handleNoteClick(note)}
          >
            <CardHeader>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <CardTitle>{note.title || 'Untitled Note'}</CardTitle>
                  <CardDescription>
                    Updated {note.lastModified ? new Date(note.lastModified).toLocaleDateString() : new Date(note.createdDate).toLocaleDateString()}
                  </CardDescription>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant={getCoverageColor(note.coveragePercent)}>
                    {note.coveragePercent ? `${Math.round(note.coveragePercent)}%` : 'N/A'}
                  </Badge>
                </div>
              </div>
            </CardHeader>
            {note.tags.length > 0 && (
              <CardContent>
                <div className="flex flex-wrap gap-2">
                  {note.tags.map((tag, idx) => (
                    <Badge key={idx} variant="default" className="text-xs">
                      {tag}
                    </Badge>
                  ))}
                </div>
              </CardContent>
            )}
          </Card>
        ))}
      </div>

      {/* Note Detail Dialog */}
      <Dialog open={selectedNote !== null} onOpenChange={() => setSelectedNote(null)}>
        <DialogContent className="max-w-5xl max-h-[90vh] overflow-hidden flex flex-col">
          <DialogHeader>
            <DialogTitle>{selectedNote?.title || 'Untitled Note'}</DialogTitle>
            <DialogDescription>
              Last updated {selectedNote?.lastModified ? new Date(selectedNote.lastModified).toLocaleDateString() : 'Never'}
              {selectedNote?.coveragePercent && (
                <span className="ml-2">
                  • Coverage: <Badge variant={getCoverageColor(selectedNote.coveragePercent)} className="ml-1">
                    {Math.round(selectedNote.coveragePercent)}%
                  </Badge>
                </span>
              )}
            </DialogDescription>
          </DialogHeader>

          <div className="flex-1 overflow-hidden flex gap-4">
            {/* Note Content (Left) */}
            <div className="flex-1 overflow-y-auto">
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="font-semibold">Content</h3>
                  <div className="flex gap-2">
                    {editMode && (
                        <Button
                            size="sm"
                            variant="ghost"
                            onClick={handleSaveNote}
                        >
                            Save
                        </Button>
                    )}
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => setEditMode(!editMode)}
                    >
                      {editMode ? 'View' : 'Edit'}
                    </Button>
                    <Button
                      size="sm"
                      variant="primary"
                      onClick={handleVerifyNote}
                      disabled={isVerifying}
                    >
                      {isVerifying ? 'Verifying...' : 'Verify with AI'}
                    </Button>
                  </div>
                </div>

                {editMode ? (
                  <div className="h-96 border border-gray-800 rounded-lg overflow-hidden">
                    <NoteEditor
                        content={editedContent}
                        onChange={setEditedContent}
                        mode={editorMode}
                        onModeChange={setEditorMode}
                        sources={sources}
                        onAddSource={(s) => setSources([...sources, s])}
                        onRemoveSource={(id) => setSources(sources.filter(s => s.id !== id))}
                        citationStyle={citationStyle}
                        onCitationStyleChange={setCitationStyle}
                    />
                  </div>
                ) : (
                  <div className="prose prose-invert prose-sm max-w-none p-4 bg-gray-800/50 rounded-lg">
                    <pre className="whitespace-pre-wrap font-sans text-sm leading-relaxed">
                      {selectedNote?.content || 'No content'}
                    </pre>
                  </div>
                )}
              </div>
            </div>

            {/* Suggestions Rail (Right) */}
            <div className="w-80 border-l border-gray-700 pl-4 overflow-y-auto">
              <h3 className="font-semibold mb-4">
                AI Suggestions ({suggestions.length})
              </h3>

              {suggestions.length === 0 ? (
                <EmptyState
                  title="No suggestions"
                  description="Click 'Verify with AI' to generate suggestions"
                />
              ) : (
                <div className="space-y-3">
                  {suggestions.map((suggestion) => (
                    <Card
                      key={suggestion.id}
                      className="cursor-pointer hover:border-primary/50 transition-colors"
                      onClick={() => setSelectedSuggestion(suggestion)}
                    >
                      <CardContent className="p-3">
                        <div className="flex items-start justify-between mb-2">
                          <Badge variant={getSuggestionTypeColor(suggestion.typeName)}>
                            {suggestion.typeName}
                          </Badge>
                          <span className="text-xs text-gray-400">
                            {Math.round(suggestion.confidence * 100)}%
                          </span>
                        </div>
                        <p className="text-xs text-gray-300 line-clamp-2">
                          {suggestion.contentMarkdown.substring(0, 80)}...
                        </p>
                        <p className="text-xs text-gray-500 mt-1">
                          {suggestion.source}
                        </p>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Suggestion Detail Dialog */}
      <Dialog open={selectedSuggestion !== null} onOpenChange={() => setSelectedSuggestion(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              <Badge variant={getSuggestionTypeColor(selectedSuggestion?.typeName || '')}>
                {selectedSuggestion?.typeName}
              </Badge>
              <span className="ml-2 text-sm font-normal text-gray-400">
                Confidence: {selectedSuggestion ? Math.round(selectedSuggestion.confidence * 100) : 0}%
              </span>
            </DialogTitle>
            <DialogDescription>
              Source: {selectedSuggestion?.source}
            </DialogDescription>
          </DialogHeader>

          <div className="py-4">
            <div className="prose prose-invert prose-sm max-w-none p-4 bg-gray-800/50 rounded-lg">
              <pre className="whitespace-pre-wrap font-sans text-sm leading-relaxed">
                {selectedSuggestion?.contentMarkdown}
              </pre>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="secondary"
              onClick={() => setSelectedSuggestion(null)}
            >
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={() => selectedSuggestion && handleAcceptSuggestion(selectedSuggestion.id)}
            >
              Accept & Add to Note
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
