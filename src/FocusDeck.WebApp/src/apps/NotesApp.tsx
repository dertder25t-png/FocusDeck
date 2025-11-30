import React, { useState, useEffect } from 'react';
import { useNotes, useNote, useCreateNote, useUpdateNote } from '../hooks/useNotes';
import type { Note } from '../types';
import { TiptapEditor } from '../components/TiptapEditor';
import { formatDistanceToNow } from 'date-fns';
import { Folder, Plus } from 'lucide-react';

// Helper to group notes by tag (simulating folders)
const groupNotesByTag = (notes: Note[]) => {
  const groups: Record<string, Note[]> = {};
  const uncategorized: Note[] = [];

  notes.forEach(note => {
    if (note.tags && note.tags.length > 0) {
      note.tags.forEach(tag => {
        if (!groups[tag]) groups[tag] = [];
        // Prevent duplicates if multiple tags, just add to first for simplicity in this tree view
        // Or we can list it under multiple tags. Let's list under all tags.
        if (!groups[tag].some(n => n.id === note.id)) {
            groups[tag].push(note);
        }
      });
    } else {
      uncategorized.push(note);
    }
  });

  return { groups, uncategorized };
};

export const NotesApp: React.FC = () => {
  const [activeNoteId, setActiveNoteId] = useState<string | null>(null);
  const [showCitations, setShowCitations] = useState(false);
  // We'll use local state for immediate feedback while typing,
  // but sync with React Query mutations debounced.
  const [localTitle, setLocalTitle] = useState('');
  const [localContent, setLocalContent] = useState('');

  const { data: notes = [], isLoading } = useNotes();
  const { data: activeNote } = useNote(activeNoteId);
  const createNoteMutation = useCreateNote();
  const updateNoteMutation = useUpdateNote();

  const [expandedFolders, setExpandedFolders] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (activeNote) {
      setLocalTitle(activeNote.title);
      setLocalContent(activeNote.content);
    }
  }, [activeNote]);

  const { groups, uncategorized } = groupNotesByTag(notes);
  const sortedFolders = Object.keys(groups).sort();

  const handleCreateNote = async (folderTag?: string) => {
    const newNote = {
      title: 'Untitled Note',
      content: '',
      tags: folderTag ? [folderTag] : [],
      type: 0 // QuickNote
    };
    const result = await createNoteMutation.mutateAsync(newNote);
    if (result) {
        setActiveNoteId(result.id);
    }
  };

  const handleCreateFolder = () => {
    const folderName = prompt("Enter folder name:");
    if (folderName) {
        // Just create a placeholder note or handle folder creation logic.
        // Since we map folders to tags, we can't create an empty folder without a note.
        // Let's create a welcome note in that folder.
        handleCreateNote(folderName);
        setExpandedFolders(prev => ({ ...prev, [folderName]: true }));
    }
  };

  // Debounced save
  useEffect(() => {
    if (!activeNoteId) return;

    // Only update if changed
    if (activeNote && (localTitle !== activeNote.title || localContent !== activeNote.content)) {
        const timeoutId = setTimeout(() => {
            updateNoteMutation.mutate({
                id: activeNoteId,
                note: { title: localTitle, content: localContent }
            });
        }, 1000); // 1s debounce
        return () => clearTimeout(timeoutId);
    }
  }, [localTitle, localContent, activeNoteId]);

  const toggleFolder = (folder: string) => {
    setExpandedFolders(prev => ({ ...prev, [folder]: !prev[folder] }));
  };

  const getSaveStatus = () => {
      if (updateNoteMutation.isPending) return "Saving...";
      if (updateNoteMutation.isError) return "Error saving";
      if (activeNote?.lastModified) {
          return `Saved ${formatDistanceToNow(new Date(activeNote.lastModified), { addSuffix: true })}`;
      }
      return "Saved";
  };

  if (isLoading) return <div className="p-4">Loading notes...</div>;

  return (
    <div className="flex h-full bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 transition-colors duration-300 overflow-hidden">
      {/* Sidebar - Tree View */}
      <div className="w-64 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-gray-50 dark:bg-gray-950 shrink-0 transition-all duration-300">
        <div className="p-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
          <h2 className="font-bold text-xs uppercase tracking-wider text-gray-500 dark:text-gray-400">Folders</h2>
          <div className="flex gap-1">
             <button onClick={() => handleCreateFolder()} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200" title="New Folder">
               <Folder size={14} />
             </button>
             <button onClick={() => handleCreateNote()} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200" title="New Note">
               <Plus size={14} />
             </button>
          </div>
        </div>
        <div className="flex-1 overflow-y-auto p-2">
            {/* Folders (Tags) */}
            {sortedFolders.map(folder => (
                <div key={folder}>
                    <div
                        className="flex items-center gap-2 py-1.5 px-2 rounded-md cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300"
                        onClick={() => toggleFolder(folder)}
                    >
                        <i className={`fa-solid fa-chevron-right text-[10px] w-4 transition-transform ${expandedFolders[folder] ? 'rotate-90' : ''}`}></i>
                        <span className="truncate text-sm font-medium">{folder}</span>
                        <span className="ml-auto text-xs text-gray-400">{groups[folder].length}</span>
                    </div>
                    {expandedFolders[folder] && (
                        <div className="ml-4 border-l border-gray-200 dark:border-gray-800 pl-1">
                            {groups[folder].map(note => (
                                <div
                                    key={note.id}
                                    className={`flex items-center gap-2 py-1.5 px-2 rounded-md cursor-pointer transition-colors ${activeNoteId === note.id ? 'bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300 font-medium' : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300'}`}
                                    onClick={() => setActiveNoteId(note.id)}
                                >
                                    <span className="truncate text-sm">{note.title || 'Untitled'}</span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            ))}

            {/* Uncategorized Notes */}
             {uncategorized.length > 0 && (
                <div className="mt-2">
                    <div className="px-2 py-1 text-xs uppercase text-gray-400 font-bold tracking-wider">Uncategorized</div>
                    {uncategorized.map(note => (
                        <div
                            key={note.id}
                            className={`flex items-center gap-2 py-1.5 px-2 rounded-md cursor-pointer transition-colors ${activeNoteId === note.id ? 'bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300 font-medium' : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300'}`}
                            onClick={() => setActiveNoteId(note.id)}
                        >
                             <span className="truncate text-sm">{note.title || 'Untitled'}</span>
                        </div>
                    ))}
                </div>
             )}
        </div>
      </div>

      {/* Main Editor Area */}
      <div className="flex-1 flex flex-col relative h-full">
        {activeNoteId ? (
            <>
                {/* Toolbar */}
                <div className="h-12 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between px-4 bg-white dark:bg-gray-900 shrink-0">
                  <div className="flex items-center gap-2 text-gray-400">
                    <span className="text-xs flex items-center gap-1">
                        {updateNoteMutation.isPending ? <i className="fa-solid fa-circle-notch fa-spin"></i> : <i className="fa-solid fa-check"></i>}
                        {getSaveStatus()}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => setShowCitations(!showCitations)}
                      className={`p-2 rounded hover:bg-gray-100 dark:hover:bg-gray-800 ${showCitations ? 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20' : 'text-gray-600 dark:text-gray-400'}`}
                      title="Citations"
                    >
                      <i className="fa-solid fa-quote-right"></i>
                    </button>
                  </div>
                </div>

                {/* Editor Content */}
                <div className="flex-1 flex flex-col overflow-hidden">
                    <input
                        className="text-3xl font-bold p-8 pb-4 outline-none bg-transparent text-gray-900 dark:text-white border-none w-full"
                        value={localTitle}
                        onChange={(e) => setLocalTitle(e.target.value)}
                        placeholder="Note Title"
                    />
                    <div className="flex-1 overflow-hidden relative">
                         {/* We pass a key to force remount when switching notes, otherwise content might not update correctly in some editors */}
                         <TiptapEditor
                             key={activeNoteId}
                             content={localContent}
                             onChange={setLocalContent}
                         />
                    </div>
                </div>
            </>
        ) : (
            <div className="flex-1 flex items-center justify-center text-gray-400 flex-col gap-4">
                <i className="fa-regular fa-note-sticky text-4xl"></i>
                <p>Select a note or create a new one</p>
            </div>
        )}
      </div>

      {/* Citation Sidebar (Placeholder for now as it's not fully spec'd in requirements but was in original code) */}
      {showCitations && activeNote && (
        <div className="w-80 border-l border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-950 shrink-0 flex flex-col transition-all duration-300 h-full">
          <div className="p-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
            <h3 className="font-bold text-sm text-gray-700 dark:text-gray-200">Citations & Sources</h3>
            <button onClick={() => setShowCitations(false)} className="text-gray-400 hover:text-gray-600"><i className="fa-solid fa-xmark"></i></button>
          </div>
          <div className="p-4 flex-1 overflow-y-auto">
             <div className="text-sm text-gray-500 italic">Citation management coming soon...</div>
          </div>
        </div>
      )}
    </div>
  );
};
