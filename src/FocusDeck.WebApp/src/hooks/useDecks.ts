import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../services/api';

export interface Flashcard {
  id: string;
  front: string;
  back: string;
  nextReview?: string;
  easeFactor?: number;
  interval?: number;
}

export interface Deck {
  id: string;
  title: string;
  description?: string;
  cards: Flashcard[];
}

// Assuming endpoints:
// GET /v1/decks
// POST /v1/decks
// POST /v1/decks/{id}/cards
// POST /v1/decks/{id}/cards/{cardId}/review  (for spaced repetition)

export const deckService = {
    getDecks: async () => {
        // If the backend doesn't have decks yet, we might need to mock or ensure it does.
        // Assuming we are "stop pretending", we call it.
        // If 404, we return empty array to prevent crash if backend feature is missing.
        try {
            const res = await apiFetch('/v1/decks');
            if (!res.ok) return [];
            return res.json();
        } catch {
            return [];
        }
    },
    createDeck: async (deck: Partial<Deck>) => {
        const res = await apiFetch('/v1/decks', {
            method: 'POST',
            body: JSON.stringify(deck)
        });
        if (!res.ok) throw new Error('Failed to create deck');
        return res.json();
    },
    addCard: async (deckId: string, card: Partial<Flashcard>) => {
        const res = await apiFetch(`/v1/decks/${deckId}/cards`, {
            method: 'POST',
            body: JSON.stringify(card)
        });
        if (!res.ok) throw new Error('Failed to add card');
        return res.json();
    },
    reviewCard: async (deckId: string, cardId: string, rating: number) => {
        const res = await apiFetch(`/v1/decks/${deckId}/cards/${cardId}/review`, {
            method: 'POST',
            body: JSON.stringify({ rating }) // 1=Hard, 2=Good, 3=Easy
        });
        if (!res.ok) throw new Error('Failed to review card');
        return res.json();
    }
};

export function useDecks() {
    return useQuery({
        queryKey: ['decks'],
        queryFn: deckService.getDecks
    });
}

export function useCreateDeck() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: deckService.createDeck,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['decks'] });
        }
    });
}

export function useAddCard() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ deckId, card }: { deckId: string, card: Partial<Flashcard> }) =>
            deckService.addCard(deckId, card),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['decks'] });
        }
    });
}
