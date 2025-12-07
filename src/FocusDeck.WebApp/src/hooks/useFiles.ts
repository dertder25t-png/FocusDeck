import { useNotes } from './useNotes';
import type { WindowId } from '../contexts/WindowManagerContext';

export interface FileItem {
  id: string;
  name: string;
  type: 'note' | 'board' | 'canvas' | 'flashcard';
  targetContent: WindowId;
  lastModified?: string;
}

export function useFiles(workspace: string) {
  // Aggregate files from Notes and Tasks (as boards)
  // In a real scenario, this might come from a dedicated /v1/files or /v1/workspaces endpoint
  // For now, we synthesize it to eliminate mock data while keeping the "OS" feel.

  const { data: notes = [] } = useNotes();

  const files: FileItem[] = [];

  // Map Notes to Files
  if (Array.isArray(notes)) {
    notes.forEach(note => {
      files.push({
        id: note.id,
        name: note.title || 'Untitled Note',
        type: 'note',
        targetContent: 'win-notes',
        lastModified: note.lastModified
      });
    });
  }

  // Map Tasks to Boards (Just one main board for now)
  // Let's create a virtual file for the main Kanban board
  files.push({
    id: 'kanban-main',
    name: 'Main Board',
    type: 'board',
    targetContent: 'win-kanban',
    lastModified: new Date().toISOString()
  });

  // Filter by workspace if we had workspace logic in entities
  // For now, we just return all
  return { files: { [workspace]: files }, isLoading: false };
}
