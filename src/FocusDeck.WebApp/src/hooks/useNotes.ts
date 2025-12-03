
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { noteService } from '../services/api';
import type { Note } from '../types';

export const useNotes = () => {
  const queryClient = useQueryClient();

  const notesQuery = useQuery({
    queryKey: ['notes'],
    queryFn: async () => {
      // noteService.getNotes returns "response.data" if using Axios or "response.json()" if fetch
      // Assuming api.ts uses axios and returns data directly
      const data = await noteService.getNotes();
      // Ensure it's an array for safety or assume it is
      return (data.notes || data) as Note[];
    }
  });

  const createNoteMutation = useMutation({
    mutationFn: (newNote: Partial<Note>) => noteService.createNote(newNote),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notes'] });
    }
  });

  const updateNoteMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<Note> }) => noteService.updateNote(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notes'] });
    }
  });

  const deleteNoteMutation = useMutation({
    mutationFn: (id: string) => noteService.deleteNote(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notes'] });
    }
  });

  return {
    notes: notesQuery.data || [],
    isLoading: notesQuery.isLoading,
    isError: notesQuery.isError,
    createNote: createNoteMutation.mutate,
    updateNote: updateNoteMutation.mutate,
    deleteNote: deleteNoteMutation.mutate
  };
};

export const useNote = (id: string | null) => {
    return useQuery({
        queryKey: ['note', id],
        queryFn: async () => {
            if (!id) return null;
            const data = await noteService.getNote(id);
            return data as Note;
        },
        enabled: !!id
    });
};
