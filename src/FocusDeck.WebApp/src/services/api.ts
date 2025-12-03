
import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig } from 'axios';
import { getAuthToken, refreshAuthToken, logout } from '../lib/utils';
import type { Note, TodoItem } from '../types';

// Create Axios instance
const api: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

api.interceptors.request.use(
  async (config) => {
    try {
      const token = await getAuthToken().catch(() => null);
      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    } catch {
      // Ignore
    }
    return config;
  },
  (error) => Promise.reject(error)
);

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: any) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (originalRequest.url?.includes('/v1/auth/refresh')) {
        await logout();
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({
            resolve: (token: string) => {
              if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${token}`;
              }
              resolve(api(originalRequest));
            },
            reject: (err) => {
              reject(err);
            },
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const newToken = await refreshAuthToken();
        if (newToken) {
          processQueue(null, newToken);
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
          }
          return api(originalRequest);
        } else {
             processQueue(new Error('Session expired'), null);
             await logout();
             return Promise.reject(error);
        }
      } catch (refreshErr) {
        processQueue(refreshErr, null);
        await logout();
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

// Default export for import api from '...'
export default api;

export const noteService = {
  getNotes: async (search?: string, tag?: string, pinned?: boolean, type?: string) => {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (tag) params.append('tag', tag);
    if (pinned !== undefined) params.append('pinned', String(pinned));
    if (type) params.append('type', type);

    const response = await api.get(`/api/notes?${params.toString()}`);
    return response.data;
  },

  getNote: async (id: string) => {
    const response = await api.get(`/api/notes/${id}`);
    return response.data;
  },

  createNote: async (note: Partial<Note>) => {
    const response = await api.post('/api/notes', note);
    return response.data;
  },

  updateNote: async (id: string, note: Partial<Note>) => {
    await api.put(`/api/notes/${id}`, note);
  },

  deleteNote: async (id: string) => {
    await api.delete(`/api/notes/${id}`);
  },

  getStats: async () => {
    const response = await api.get('/api/notes/stats');
    return response.data;
  }
};

export const taskService = {
  getTasks: async (completed?: boolean) => {
    const params = new URLSearchParams();
    if (completed !== undefined) params.append('completed', String(completed));

    const response = await api.get(`/api/tasks?${params.toString()}`);
    return response.data;
  },

  createTask: async (task: Partial<TodoItem>) => {
    const response = await api.post('/api/tasks', task);
    return response.data;
  },

  updateTask: async (id: string, task: Partial<TodoItem>) => {
    await api.put(`/api/tasks/${id}`, task);
  },

  deleteTask: async (id: string) => {
    await api.delete(`/api/tasks/${id}`);
  },
};
