
import React, { useState } from 'react';
import { useDecks, Deck } from '../hooks/useDecks'; // Ensure these are exported from useDecks.ts

export const FlashcardsApp: React.FC = () => {
  const { decks, isLoading, createDeck } = useDecks();
  const [activeDeck, setActiveDeck] = useState<Deck | null>(null);
  const [studyMode, setStudyMode] = useState(false);
  const [currentCardIndex, setCurrentCardIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);

  const handleStudy = (deck: Deck) => {
    setActiveDeck(deck);
    setStudyMode(true);
    setCurrentCardIndex(0);
    setIsFlipped(false);
  };

  const handleNextCard = () => {
    // In a real app, this would call a mutation to save progress
    setIsFlipped(false);
    if (activeDeck && activeDeck.cards && currentCardIndex < activeDeck.cards.length - 1) {
      setCurrentCardIndex(currentCardIndex + 1);
    } else {
      alert("Session Complete!");
      setStudyMode(false);
      setActiveDeck(null);
    }
  };

  const handleNewDeck = () => {
    createDeck({ name: 'New Deck', cards: [] });
  };

  if (isLoading) {
      return <div className="h-full flex items-center justify-center">Loading decks...</div>;
  }

  if (studyMode && activeDeck && activeDeck.cards?.length > 0) {
    const card = activeDeck.cards[currentCardIndex];
    return (
      <div className="h-full flex flex-col items-center justify-center bg-gray-100 dark:bg-gray-900 p-8">
        <div className="w-full max-w-2xl flex justify-between items-center mb-8">
          <button onClick={() => setStudyMode(false)} className="text-gray-500 hover:text-gray-700 dark:text-gray-400">Back to Decks</button>
          <span className="font-mono text-sm text-gray-400">{currentCardIndex + 1} / {activeDeck.cards.length}</span>
        </div>

        <div 
          className="w-full max-w-2xl aspect-[3/2] perspective-1000 cursor-pointer group"
          onClick={() => setIsFlipped(!isFlipped)}
        >
          <div className={`relative w-full h-full transition-transform duration-500 transform-style-3d ${isFlipped ? 'rotate-y-180' : ''}`} style={{ transformStyle: 'preserve-3d' }}>
            {/* Front */}
            <div className="absolute w-full h-full bg-white dark:bg-gray-800 border-2 border-gray-200 dark:border-gray-700 rounded-2xl shadow-xl flex items-center justify-center p-12 text-center" style={{ backfaceVisibility: 'hidden', WebkitBackfaceVisibility: 'hidden' }}>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-gray-100">{card.front || 'Front'}</h2>
              <div className="absolute bottom-6 text-xs text-gray-400 uppercase tracking-widest">Click to Flip</div>
            </div>
            {/* Back */}
            <div className="absolute w-full h-full bg-gray-50 dark:bg-gray-900 border-2 border-blue-500 rounded-2xl shadow-xl flex items-center justify-center p-12 text-center rotate-y-180" style={{ backfaceVisibility: 'hidden', WebkitBackfaceVisibility: 'hidden', transform: 'rotateY(180deg)' }}>
              <h2 className="text-2xl font-medium text-gray-900 dark:text-gray-100">{card.back || 'Back'}</h2>
            </div>
          </div>
        </div>

        {isFlipped && (
          <div className="flex gap-4 mt-8">
            <button onClick={() => handleNextCard()} className="px-8 py-3 bg-red-100 text-red-700 rounded-lg font-bold hover:bg-red-200 transition-colors">Hard</button>
            <button onClick={() => handleNextCard()} className="px-8 py-3 bg-green-100 text-green-700 rounded-lg font-bold hover:bg-green-200 transition-colors">Easy</button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="h-full bg-white dark:bg-gray-900 p-8 overflow-y-auto">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Flashcards</h1>
        <button onClick={handleNewDeck} className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium">New Deck</button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {decks.map((deck: Deck) => (
          <div key={deck.id} className="bg-white dark:bg-gray-800 rounded-xl p-6 border border-gray-200 dark:border-gray-700 hover:shadow-lg transition-all group">
            <div className="flex justify-between items-start mb-4">
              <div className="w-12 h-12 rounded-lg bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center text-blue-600 dark:text-blue-400 text-xl">
                 <span className="text-xl">🗂️</span>
              </div>
              <button className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">...</button>
            </div>
            <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100 mb-2">{deck.name || 'Untitled Deck'}</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">{deck.cards?.length || 0} cards</p>
            <button 
              onClick={() => handleStudy(deck)}
              disabled={!deck.cards || deck.cards.length === 0}
              className="w-full py-2 bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 font-medium hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Study Now
            </button>
          </div>
        ))}
      </div>
    </div>
  );
};
