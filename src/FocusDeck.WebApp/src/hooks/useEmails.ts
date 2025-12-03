
import api from '../services/api';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

export interface Email {
  id: string;
  sender: string; // e.g., "John Doe <john@example.com>"
  subject: string;
  snippet: string;
  date: string;
  isRead: boolean;
  bodyHtml?: string;
}

export const useEmails = () => {
  const queryClient = useQueryClient();

  const emailsQuery = useQuery({
    queryKey: ['emails'],
    queryFn: async () => {
        try {
            const response = await api.get('/api/v1/integrations/google/messages');
            return response.data as Email[];
        } catch (err) {
             console.warn("Failed to fetch emails, likely endpoint missing", err);
             return [];
        }
    }
  });

  const sendEmailMutation = useMutation({
    mutationFn: async (payload: { to: string; subject: string; body: string }) => {
        await api.post('/api/v1/integrations/google/send', payload);
    }
  });

  return {
    emails: emailsQuery.data || [],
    isLoading: emailsQuery.isLoading,
    isError: emailsQuery.isError,
    sendEmail: sendEmailMutation.mutateAsync
  };
};
