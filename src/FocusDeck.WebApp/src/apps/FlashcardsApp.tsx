import React, { useState } from 'react';

interface Flashcard {
  id: string;
  front: string;
  back: string;
  nextReview: Date;
}

interface Deck {
  id: string;
  title: string;
  cards: Flashcard[];
}

const MOCK_DECKS: Deck[] = [
  {
    id: 'd1',
    title: 'Computer Science 101',
    cards: [
      { id: 'c1', front: 'What is a bit?', back: 'The basic unit of information in computing (0 or 1).', nextReview: new Date() },
      { id: 'c2', front: 'What is RAM?', back: 'Random Access Memory - volatile memory used for currently running programs.', nextReview: new Date() }
    ]
  },
  {
    id: 'd2',
    title: 'Spanish Vocabulary',
    cards: [
      { id: 'c3', front: 'Hola', back: 'Hello', nextReview: new Date() },
      { id: 'c4', front: 'Gracias', back: 'Thank you', nextReview: new Date() }
    ]
  }
];

export const FlashcardsApp: React.FC = () => {
  const [decks, setDecks] = useState<Deck[]>(MOCK_DECKS);
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
    // Mock RNG / Spaced Repetition Logic
    // In a real app, we would update nextReview based on rating
    setIsFlipped(false);
    if (activeDeck && currentCardIndex < activeDeck.cards.length - 1) {
      setCurrentCardIndex(currentCardIndex + 1);
    } else {
      alert("Session Complete!");
      setStudyMode(false);
      setActiveDeck(null);
    }
  };

  const handleNewDeck = () => {
    const newDeck: Deck = {
      id: Date.now().toString(),
      title: 'New Deck',
      cards: []
    };
    setDecks([...decks, newDeck]);
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
        <h1 className="text-2xl font-bold text-ink">Flashcards</h1>
        <button onClick={handleNewDeck} className="px-4 py-2 bg-accent-blue text-white rounded-lg hover:bg-blue-600 transition-colors font-medium"><i className="fa-solid fa-plus mr-2"></i> New Deck</button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {decks.map(deck => (
          <div key={deck.id} className="bg-surface rounded-xl p-6 border border-border hover:shadow-lg transition-all group">
            <div className="flex justify-between items-start mb-4">
              <div className="w-12 h-12 rounded-lg bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center text-blue-600 dark:text-blue-400 text-xl">
                <i className="fa-solid fa-layer-group"></i>
              </div>
              <button className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"><i className="fa-solid fa-ellipsis"></i></button>
            </div>
            <h3 className="text-lg font-bold text-ink mb-2">{deck.title}</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">{deck.cards.length} cards • Due today</p>
            <button 
              onClick={() => handleStudy(deck)}
              className="w-full py-2 bg-subtle border border-border rounded-lg text-ink font-medium hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
            >
              Study Now
            </button>
          </div>
        ))}
      </div>
    </div>
  );
};
