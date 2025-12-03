
import api from '../services/api';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

export interface Deck {
  id: string;
  name: string;
  cards: any[]; // Define proper structure if needed
}

export const useDecks = () => {
  const queryClient = useQueryClient();

  const decksQuery = useQuery({
    queryKey: ['decks'],
    queryFn: async () => {
      const response = await api.get('/api/decks');
      return response.data as Deck[];
    }
  });

  const createDeckMutation = useMutation({
    mutationFn: async (deck: Partial<Deck>) => {
      const response = await api.post('/api/decks', deck);
      return response.data as Deck;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['decks'] });
    }
  });

  return {
    decks: decksQuery.data || [],
    isLoading: decksQuery.isLoading,
    createDeck: createDeckMutation.mutate
  };
};
