import React, { useState } from 'react';
import { useDecks, useCreateDeck, useAddCard, deckService, type Deck, type Flashcard } from '../hooks/useDecks';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../components/Dialog';
import { useToast } from '../hooks/useToast';

export const FlashcardsApp: React.FC = () => {
  const { data: decks = [], isLoading } = useDecks();
  const createDeckMutation = useCreateDeck();
  const addCardMutation = useAddCard();
  const { addToast } = useToast();

  const [activeDeck, setActiveDeck] = useState<Deck | null>(null);
  const [studyMode, setStudyMode] = useState(false);
  const [currentCardIndex, setCurrentCardIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);

  // Modals
  const [isNewDeckOpen, setIsNewDeckOpen] = useState(false);
  const [newDeckTitle, setNewDeckTitle] = useState('');
  const [isAddCardOpen, setIsAddCardOpen] = useState(false);
  const [newCardFront, setNewCardFront] = useState('');
  const [newCardBack, setNewCardBack] = useState('');
  const [targetDeckId, setTargetDeckId] = useState<string | null>(null);

  const handleStudy = (deck: Deck) => {
    if (deck.cards.length === 0) {
        addToast({ title: 'Empty Deck', description: 'Add some cards first!', variant: 'error' });
        return;
    }
    setActiveDeck(deck);
    setStudyMode(true);
    setCurrentCardIndex(0);
    setIsFlipped(false);
  };

  const handleRate = async (rating: number) => { // 1=Hard, 3=Easy
      // In a real app, send rating to backend
      if (activeDeck && activeDeck.cards[currentCardIndex]) {
          try {
              // Optimistic update or just fire and forget for UI responsiveness
              await deckService.reviewCard(activeDeck.id, activeDeck.cards[currentCardIndex].id, rating);
          } catch (e) {
              console.error("Failed to submit review", e);
          }
      }

      setIsFlipped(false);
      if (activeDeck && currentCardIndex < activeDeck.cards.length - 1) {
          setCurrentCardIndex(currentCardIndex + 1);
      } else {
          addToast({ title: 'Session Complete', description: 'Great job!', variant: 'success' });
          setStudyMode(false);
          setActiveDeck(null);
      }
  };

  const handleCreateDeck = async () => {
    if (!newDeckTitle) return;
    try {
        await createDeckMutation.mutateAsync({ title: newDeckTitle, cards: [] });
        setIsNewDeckOpen(false);
        setNewDeckTitle('');
        addToast({ title: 'Success', description: 'Deck created.', variant: 'success' });
    } catch (e) {
        addToast({ title: 'Error', description: 'Failed to create deck.', variant: 'error' });
    }
  };

  const handleAddCard = async () => {
      if (!targetDeckId || !newCardFront || !newCardBack) return;
      try {
          await addCardMutation.mutateAsync({ deckId: targetDeckId, card: { front: newCardFront, back: newCardBack } });
          setIsAddCardOpen(false);
          setNewCardFront('');
          setNewCardBack('');
          addToast({ title: 'Success', description: 'Card added.', variant: 'success' });
      } catch (e) {
          addToast({ title: 'Error', description: 'Failed to add card.', variant: 'error' });
      }
  };

  const openAddCard = (deckId: string) => {
      setTargetDeckId(deckId);
      setIsAddCardOpen(true);
  };

  if (studyMode && activeDeck) {
    const card = activeDeck.cards[currentCardIndex];
    return (
      <div className="h-full flex flex-col items-center justify-center bg-gray-100 dark:bg-gray-900 p-8">
        <div className="w-full max-w-2xl flex justify-between items-center mb-8">
          <button onClick={() => setStudyMode(false)} className="text-gray-500 hover:text-gray-700 dark:text-gray-400"><i className="fa-solid fa-arrow-left"></i> Back to Decks</button>
          <span className="font-mono text-sm text-gray-400">{currentCardIndex + 1} / {activeDeck.cards.length}</span>
        </div>

        <div 
          className="w-full max-w-2xl aspect-[3/2] perspective-1000 cursor-pointer group"
          onClick={() => setIsFlipped(!isFlipped)}
        >
          <div className={`relative w-full h-full transition-transform duration-500 transform-style-3d ${isFlipped ? 'rotate-y-180' : ''}`} style={{ transformStyle: 'preserve-3d' }}>
            {/* Front */}
            <div className="absolute w-full h-full bg-surface border-2 border-border rounded-2xl shadow-xl flex items-center justify-center p-12 text-center" style={{ backfaceVisibility: 'hidden', WebkitBackfaceVisibility: 'hidden' }}>
              <h2 className="text-3xl font-bold text-ink">{card.front}</h2>
              <div className="absolute bottom-6 text-xs text-gray-400 uppercase tracking-widest">Click to Flip</div>
            </div>
            {/* Back */}
            <div className="absolute w-full h-full bg-subtle border-2 border-accent-blue rounded-2xl shadow-xl flex items-center justify-center p-12 text-center rotate-y-180" style={{ backfaceVisibility: 'hidden', WebkitBackfaceVisibility: 'hidden', transform: 'rotateY(180deg)' }}>
              <h2 className="text-2xl font-medium text-ink">{card.back}</h2>
            </div>
          </div>
        </div>

        {isFlipped && (
          <div className="flex gap-4 mt-8">
            <button onClick={() => handleRate(1)} className="px-8 py-3 bg-red-100 text-red-700 rounded-lg font-bold hover:bg-red-200 transition-colors">Hard</button>
            <button onClick={() => handleRate(2)} className="px-8 py-3 bg-blue-100 text-blue-700 rounded-lg font-bold hover:bg-blue-200 transition-colors">Good</button>
            <button onClick={() => handleRate(3)} className="px-8 py-3 bg-green-100 text-green-700 rounded-lg font-bold hover:bg-green-200 transition-colors">Easy</button>
          </div>
        )}
      </div>
    );
  }

  if (isLoading) return <div className="h-full flex items-center justify-center">Loading decks...</div>;

  return (
    <div className="h-full bg-white dark:bg-gray-900 p-8 overflow-y-auto">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-2xl font-bold text-ink">Flashcards</h1>
        <button onClick={() => setIsNewDeckOpen(true)} className="px-4 py-2 bg-accent-blue text-white rounded-lg hover:bg-blue-600 transition-colors font-medium"><i className="fa-solid fa-plus mr-2"></i> New Deck</button>
      </div>

      {decks.length === 0 && (
          <div className="text-center py-20 text-gray-400">
              <i className="fa-solid fa-layer-group text-4xl mb-4"></i>
              <p>No decks found. Create one to get started!</p>
          </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {decks.map(deck => (
          <div key={deck.id} className="bg-surface rounded-xl p-6 border border-border hover:shadow-lg transition-all group relative">
            <div className="flex justify-between items-start mb-4">
              <div className="w-12 h-12 rounded-lg bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center text-blue-600 dark:text-blue-400 text-xl">
                <i className="fa-solid fa-layer-group"></i>
              </div>
              <div className="flex gap-2">
                 <button onClick={() => openAddCard(deck.id)} className="text-gray-400 hover:text-blue-600" title="Add Card"><i className="fa-solid fa-plus"></i></button>
              </div>
            </div>
            <h3 className="text-lg font-bold text-ink mb-2">{deck.title}</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">{deck.cards?.length || 0} cards</p>
            <button 
              onClick={() => handleStudy(deck)}
              className="w-full py-2 bg-subtle border border-border rounded-lg text-ink font-medium hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
            >
              Study Now
            </button>
          </div>
        ))}
      </div>

      {/* New Deck Modal */}
      <Dialog open={isNewDeckOpen} onOpenChange={setIsNewDeckOpen}>
          <DialogContent>
              <DialogHeader><DialogTitle>Create New Deck</DialogTitle></DialogHeader>
              <div className="py-4">
                  <input className="w-full px-3 py-2 border rounded dark:bg-gray-800 dark:border-gray-700" placeholder="Deck Title" value={newDeckTitle} onChange={e => setNewDeckTitle(e.target.value)} />
              </div>
              <DialogFooter>
                  <button onClick={() => setIsNewDeckOpen(false)} className="px-4 py-2 mr-2">Cancel</button>
                  <button onClick={handleCreateDeck} className="px-4 py-2 bg-blue-600 text-white rounded">Create</button>
              </DialogFooter>
          </DialogContent>
      </Dialog>

      {/* Add Card Modal */}
      <Dialog open={isAddCardOpen} onOpenChange={setIsAddCardOpen}>
          <DialogContent>
              <DialogHeader><DialogTitle>Add New Card</DialogTitle></DialogHeader>
              <div className="py-4 space-y-4">
                  <input className="w-full px-3 py-2 border rounded dark:bg-gray-800 dark:border-gray-700" placeholder="Front (Question)" value={newCardFront} onChange={e => setNewCardFront(e.target.value)} />
                  <textarea className="w-full px-3 py-2 border rounded dark:bg-gray-800 dark:border-gray-700 h-24" placeholder="Back (Answer)" value={newCardBack} onChange={e => setNewCardBack(e.target.value)} />
              </div>
              <DialogFooter>
                  <button onClick={() => setIsAddCardOpen(false)} className="px-4 py-2 mr-2">Cancel</button>
                  <button onClick={handleAddCard} className="px-4 py-2 bg-blue-600 text-white rounded">Add Card</button>
              </DialogFooter>
          </DialogContent>
      </Dialog>

    </div>
  );
};
