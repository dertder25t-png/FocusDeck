import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../services/api';

export interface Email {
  id: string;
  from: string;
  subject: string;
  preview: string;
  date: string;
  isUnread: boolean;
  body: string;
}

export function useEmails() {
  return useQuery({
    queryKey: ['emails'],
    queryFn: async () => {
      // Fetch from backend
      // Using /v1/integrations/google/messages as indicated in memory
      // If this endpoint doesn't exist yet, we might need to fallback or ensure backend has it.
      // But requirement is "stop pretending", so we assume we must try to fetch.
      const res = await apiFetch('/v1/integrations/google/messages');
      if (!res.ok) {
        throw new Error('Failed to fetch emails');
      }
      return res.json() as Promise<Email[]>;
    },
    // Don't retry too much if integration is missing
    retry: 1,
    staleTime: 60000
  });
}
