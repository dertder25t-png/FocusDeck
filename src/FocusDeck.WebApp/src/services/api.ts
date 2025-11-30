
import { apiFetch } from '../lib/utils';
import type { Note, TodoItem } from '../types';

export const noteService = {
  getNotes: async (search?: string, tag?: string, pinned?: boolean, type?: string) => {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (tag) params.append('tag', tag);
    if (pinned !== undefined) params.append('pinned', String(pinned));
    if (type) params.append('type', type);

    const response = await apiFetch(`/api/notes?${params.toString()}`);
    if (!response.ok) {
      const msg = await response.text().catch(() => 'Failed to fetch notes');
      throw new Error(msg || 'Failed to fetch notes');
    }
    // Success payload is JSON array
    return response.json();
  },

  getNote: async (id: string) => {
    const response = await apiFetch(`/api/notes/${id}`);
    if (!response.ok) {
      const msg = await response.text().catch(() => 'Failed to fetch note');
      throw new Error(msg || 'Failed to fetch note');
    }
    // Success payload is JSON object
    return response.json();
  },

  createNote: async (note: Partial<Note>) => {
    const response = await apiFetch('/api/notes', {
      method: 'POST',
      body: JSON.stringify(note),
    });
    if (!response.ok) {
      const msg = await response.text().catch(() => 'Failed to create note');
      throw new Error(msg || 'Failed to create note');
    }
    // Success payload is JSON object
    return response.json();
  },

  updateNote: async (id: string, note: Partial<Note>) => {
    const response = await apiFetch(`/api/notes/${id}`, {
      method: 'PUT',
      body: JSON.stringify(note),
    });
    if (!response.ok) {
      const msg = await response.text().catch(() => 'Failed to update note');
      throw new Error(msg || 'Failed to update note');
    }
    // Note: The backend returns NoContent (204) for update
    return;
  },

  deleteNote: async (id: string) => {
    const response = await apiFetch(`/api/notes/${id}`, {
      method: 'DELETE',
    });
    if (!response.ok) {
      const msg = await response.text().catch(() => 'Failed to delete note');
      throw new Error(msg || 'Failed to delete note');
    }
  },

  getStats: async () => {
    const response = await apiFetch('/api/notes/stats');
    if (!response.ok) throw new Error('Failed to fetch stats');
    return response.json();
  }
};

export const taskService = {
  getTasks: async (completed?: boolean) => {
    const params = new URLSearchParams();
    if (completed !== undefined) params.append('completed', String(completed));

    const response = await apiFetch(`/api/tasks?${params.toString()}`);
    if (!response.ok) throw new Error('Failed to fetch tasks');
    return response.json();
  },

  createTask: async (task: Partial<TodoItem>) => {
    const response = await apiFetch('/api/tasks', {
      method: 'POST',
      body: JSON.stringify(task),
    });
    if (!response.ok) throw new Error('Failed to create task');
    return response.json();
  },

  updateTask: async (id: string, task: Partial<TodoItem>) => {
    const response = await apiFetch(`/api/tasks/${id}`, {
      method: 'PUT',
      body: JSON.stringify(task),
    });
    if (!response.ok) throw new Error('Failed to update task');
    return;
  },

  deleteTask: async (id: string) => {
    const response = await apiFetch(`/api/tasks/${id}`, {
      method: 'DELETE',
    });
    if (!response.ok) throw new Error('Failed to delete task');
  },
};
