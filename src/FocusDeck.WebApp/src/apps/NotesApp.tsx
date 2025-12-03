
import { useState, useEffect } from 'react'
import { Plus, Search, Trash2, BookOpen, Mic, X } from 'lucide-react'
import { TiptapEditor } from '../components/TiptapEditor' // Correct path
import { useNotes, useNote } from '../hooks/useNotes'
import { formatDate } from '../lib/utils'
import type { Note } from '../types'

export function NotesApp() {
  const { notes, createNote, updateNote, deleteNote, isLoading } = useNotes()
  const [activeNoteId, setActiveNoteId] = useState<string | null>(null)

  const { data: activeNoteData, isLoading: isLoadingNote } = useNote(activeNoteId);

  const [searchQuery, setSearchQuery] = useState('')
  const [localTitle, setLocalTitle] = useState('')
  const [localContent, setLocalContent] = useState('')

  // Sync local state when active note changes
  useEffect(() => {
    if (activeNoteData) {
      setLocalTitle(activeNoteData.title || '')
      setLocalContent(activeNoteData.content || '')
    } else if (activeNoteId === null) {
      setLocalTitle('')
      setLocalContent('')
    }
  }, [activeNoteData, activeNoteId])

  // Simple debounce for auto-save
  useEffect(() => {
    if (!activeNoteId || !activeNoteData) return

    const timer = setTimeout(() => {
        const currentTitle = activeNoteData.title || '';
        const currentContent = activeNoteData.content || '';

      if (localTitle !== currentTitle || localContent !== currentContent) {
        updateNote({
          id: activeNoteId,
          data: { title: localTitle, content: localContent }
        })
      }
    }, 1000)

    return () => clearTimeout(timer)
  }, [localTitle, localContent, activeNoteId, activeNoteData, updateNote])

  const filteredNotes = (notes || []).filter((note: Note) =>
    (note.title || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
    (note.content || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
    (note.tags || []).some(tag => tag.toLowerCase().includes(searchQuery.toLowerCase()))
  )

  const handleCreateNote = () => {
    createNote({ title: 'New Note', content: '', tags: [] }, {
      onSuccess: (newNote: Note) => {
        if (newNote && newNote.id) {
            setActiveNoteId(newNote.id)
        }
      }
    })
  }

  const handleDeleteNote = (id: string, e: React.MouseEvent) => {
    e.stopPropagation()
    if (confirm('Are you sure you want to delete this note?')) {
      deleteNote(id, {
        onSuccess: () => {
          if (activeNoteId === id) setActiveNoteId(null)
        }
      })
    }
  }

  return (
    <div className="flex h-full bg-surface-100 text-ink">
      {/* Sidebar List */}
      <div className="w-80 border-r border-surface-200 flex flex-col bg-surface-50">
        <div className="p-4 border-b border-surface-200">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-semibold text-lg flex items-center gap-2">
              <BookOpen className="w-5 h-5 text-primary" />
              Notes
            </h2>
            <button
              onClick={handleCreateNote}
              className="p-2 bg-primary text-white rounded-lg hover:bg-primary/90 transition-colors"
            >
              <Plus className="w-4 h-4" />
            </button>
          </div>
          <div className="relative">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" />
            <input
              type="text"
              placeholder="Search notes..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-9 pr-4 py-2 bg-surface text-sm rounded-lg border border-surface-200 focus:border-primary focus:ring-1 focus:ring-primary outline-none"
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto">
          {isLoading ? (
             <div className="p-4 text-center text-ink-muted">Loading notes...</div>
          ) : filteredNotes.length === 0 ? (
            <div className="p-8 text-center text-ink-muted text-sm">
              {searchQuery ? 'No notes found' : 'Create your first note'}
            </div>
          ) : (
            <div className="divide-y divide-surface-200">
              {filteredNotes.map((note: Note) => (
                <div
                  key={note.id}
                  onClick={() => setActiveNoteId(note.id)}
                  className={`p-4 cursor-pointer hover:bg-surface-200/50 transition-colors group ${
                    activeNoteId === note.id ? 'bg-surface-200 border-l-4 border-primary pl-3' : 'pl-4'
                  }`}
                >
                  <div className="flex items-start justify-between mb-1">
                    <h3 className={`font-medium text-sm truncate pr-2 ${activeNoteId === note.id ? 'text-primary' : 'text-ink'}`}>
                      {note.title || 'Untitled Note'}
                    </h3>
                    <button
                      onClick={(e) => handleDeleteNote(note.id, e)}
                      className="opacity-0 group-hover:opacity-100 p-1 hover:bg-red-100 hover:text-red-600 rounded transition-all"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                  <p className="text-xs text-ink-muted line-clamp-2 h-8">
                    {(note.content || '').replace(/<[^>]*>/g, '') || 'No content'}
                  </p>
                  <div className="flex items-center gap-3 mt-2">
                    <span className="text-[10px] text-ink-muted bg-surface-200 px-1.5 py-0.5 rounded">
                      {formatDate(new Date(note.createdDate))}
                    </span>
                    {(note.tags || []).slice(0, 2).map(tag => (
                      <span key={tag} className="text-[10px] text-primary bg-primary/10 px-1.5 py-0.5 rounded">
                        #{tag}
                      </span>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Editor Area */}
      <div className="flex-1 flex flex-col h-full bg-surface overflow-hidden">
        {activeNoteId ? (
          <>
            <div className="px-8 py-6 border-b border-surface-200 flex items-center justify-between bg-surface-50/50">
              <input
                type="text"
                value={localTitle}
                onChange={(e) => setLocalTitle(e.target.value)}
                placeholder="Note Title"
                className="text-2xl font-bold bg-transparent border-none outline-none placeholder-surface-300 w-full text-ink"
              />
              <div className="flex items-center gap-2">
                <button className="p-2 text-ink-muted hover:text-primary hover:bg-primary/5 rounded-lg transition-colors" title="Record Audio">
                  <Mic className="w-5 h-5" />
                </button>
                <button
                  onClick={() => setActiveNoteId(null)}
                  className="p-2 text-ink-muted hover:text-ink hover:bg-surface-200 rounded-lg transition-colors md:hidden"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>
            </div>

            <div className="flex-1 overflow-y-auto">
               {isLoadingNote ? (
                   <div className="p-8 text-center">Loading content...</div>
               ) : (
                  <div className="max-w-3xl mx-auto py-8 px-8 h-full">
                    <TiptapEditor
                        key={activeNoteId}
                        content={localContent}
                        onChange={setLocalContent}
                        editable={true}
                    />
                  </div>
               )}
            </div>

            {/* Status Bar */}
            <div className="px-4 py-2 border-t border-surface-200 text-xs text-ink-muted flex items-center justify-between bg-surface-50">
              <div className="flex gap-4">
                <span>{localContent.split(/\s+/).filter(w => w.length > 0).length} words</span>
                <span>Last edited just now</span>
              </div>
            </div>
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center text-ink-muted bg-surface-50">
            <div className="w-16 h-16 bg-surface-200 rounded-2xl flex items-center justify-center mb-4">
              <BookOpen className="w-8 h-8 opacity-50" />
            </div>
            <p className="text-lg font-medium mb-1">Select a note to view</p>
            <p className="text-sm opacity-70">Or create a new one to get started</p>
          </div>
        )}
      </div>
    </div>
  )
}
