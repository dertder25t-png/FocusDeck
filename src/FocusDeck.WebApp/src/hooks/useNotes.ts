
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { noteService } from '../services/api';
import type { Note } from '../types';

export function useNotes(search?: string, tag?: string, pinned?: boolean) {
  return useQuery({
    queryKey: ['notes', { search, tag, pinned }],
    queryFn: () => noteService.getNotes(search, tag, pinned),
  });
}

export function useNote(id: string | null) {
  return useQuery({
    queryKey: ['note', id],
    queryFn: () => noteService.getNote(id!),
    enabled: !!id,
  });
}

export function useCreateNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (note: Partial<Note>) => noteService.createNote(note),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notes'] });
      queryClient.invalidateQueries({ queryKey: ['noteStats'] });
    },
  });
}

export function useUpdateNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, note }: { id: string; note: Partial<Note> }) =>
      noteService.updateNote(id, note),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['notes'] });
      queryClient.invalidateQueries({ queryKey: ['note', id] });
      queryClient.invalidateQueries({ queryKey: ['noteStats'] });
    },
  });
}

export function useDeleteNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => noteService.deleteNote(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notes'] });
      queryClient.invalidateQueries({ queryKey: ['noteStats'] });
    },
  });
}

export function useNoteStats() {
  return useQuery({
    queryKey: ['noteStats'],
    queryFn: () => noteService.getStats(),
  });
}
